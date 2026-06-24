using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using WeCms.Shared;

namespace WeCms.Modules.FileCenter.Files;

public sealed class FileService : IFileService
{
    private const int MaxPageSize = 100;
    private readonly IFileRepository _repository;
    private readonly IFileStorage _storage;
    private readonly IFileScanService _fileScanService;
    private readonly IFileObjectKeyGenerator _objectKeyGenerator;
    private readonly IFileUploadPolicyResolver _policyResolver;
    private readonly IFileUploadConcurrencyGate _uploadConcurrencyGate;
    private readonly FileUploadOptions _fileUploadOptions;
    private readonly ILogger<FileService> _logger;

    public FileService(
        IFileRepository repository,
        IFileStorage storage,
        IFileScanService fileScanService,
        IFileObjectKeyGenerator objectKeyGenerator,
        IFileUploadPolicyResolver policyResolver,
        IFileUploadConcurrencyGate uploadConcurrencyGate,
        IOptions<FileUploadOptions> fileUploadOptions,
        ILogger<FileService> logger)
    {
        _repository = repository;
        _storage = storage;
        _fileScanService = fileScanService;
        _objectKeyGenerator = objectKeyGenerator;
        _policyResolver = policyResolver;
        _uploadConcurrencyGate = uploadConcurrencyGate;
        _fileUploadOptions = fileUploadOptions.Value;
        _logger = logger;
    }

    public Task<PagedResult<FileSummaryDto>> ListAsync(FileListQuery query, CancellationToken cancellationToken)
    {
        var page = query.Page <= 0 ? throw Validation("page must be greater than or equal to 1.") : query.Page;
        var pageSize = query.PageSize is <= 0 or > MaxPageSize ? throw Validation($"pageSize must be between 1 and {MaxPageSize}.") : query.PageSize;
        return _repository.ListAsync(new FileListCriteria(page, pageSize, NormalizeOptional(query.Keyword, 120), NormalizeOptional(query.MimeType, 120), NormalizeOptional(query.Status, 32)), cancellationToken);
    }

    public async Task<FileDetailDto> GetAsync(long id, CancellationToken cancellationToken)
    {
        return await _repository.GetAsync(id, cancellationToken) ?? throw new DomainException(ApiCodes.NotFound, "File was not found.");
    }

    public async Task<FileMutationResponse> CreateAsync(CreateFileRequest request, IFormFile file, FileRequestContext context, CancellationToken cancellationToken)
    {
        try
        {
            var originalName = FileNameSafety.NormalizeFileName(request.OriginalName, "originalName", 255);
            var requestMimeType = NormalizeRequired(request.MimeType, "mimeType", 120);
            var requestSha256 = NormalizeSha256(request.Sha256);
            var policy = _policyResolver.Resolve(request.Policy);
            var fileExt = FileNameSafety.NormalizeFileExtension(originalName);
            if (string.Equals(policy.Code, "avatar", StringComparison.OrdinalIgnoreCase))
            {
                throw Validation("avatar uploads must use the account avatar endpoint.");
            }

            if (request.SizeBytes <= 0 || request.SizeBytes > policy.MaxSizeBytes)
            {
                throw Validation($"sizeBytes must be between 1 and {policy.MaxSizeBytes}.");
            }

            if (file is null || file.Length <= 0)
            {
                throw Validation("file is required and must not be empty.");
            }

            if (!_uploadConcurrencyGate.TryAcquire(request.SizeBytes, out var uploadConcurrencyLease))
            {
                throw new DomainException(ApiCodes.TooManyRequests, "Too many large file uploads are in progress.");
            }

            await using (uploadConcurrencyLease)
            {
                await using var uploadContent = await FileUploadContent.ReadAsync(
                    file,
                    policy.MaxSizeBytes,
                    _fileUploadOptions,
                    cancellationToken);
                if (uploadContent.SizeBytes != request.SizeBytes)
                {
                    throw Validation("sizeBytes does not match uploaded content.");
                }

                await policy.ValidateContentAsync(uploadContent.Content, uploadContent.SizeBytes, requestMimeType, fileExt, cancellationToken);
                await ScanAsync(uploadContent, originalName, requestMimeType, request.SizeBytes, policy.Code, cancellationToken);

                var objectKey = $"{policy.StorageScope}/{_objectKeyGenerator.GenerateObjectKey(context.Now, fileExt)}";
                FileStorageResult stored;
                try
                {
                    uploadContent.Rewind();
                    stored = await _storage.StoreAsync(uploadContent.Content, objectKey, fileExt, policy.MaxSizeBytes, cancellationToken);
                }
                catch (DomainException)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    await DeleteFileAsync(objectKey, cancellationToken);
                    throw new InvalidOperationException("Upload content cannot be stored.", exception);
                }

                if (stored.SizeBytes != request.SizeBytes)
                {
                    await DeleteFileAsync(objectKey, cancellationToken);
                    throw Validation("sizeBytes does not match uploaded content.");
                }

                if (!string.Equals(stored.Sha256, requestSha256, StringComparison.OrdinalIgnoreCase))
                {
                    await DeleteFileAsync(objectKey, cancellationToken);
                    throw Validation("sha256 does not match uploaded content.");
                }

                if (!string.Equals(stored.MimeType, requestMimeType, StringComparison.OrdinalIgnoreCase))
                {
                    await DeleteFileAsync(objectKey, cancellationToken);
                    throw Validation("mimeType does not match uploaded content.");
                }

                var id = await _repository.CreateAsync(new FileCreateRecord("local", "system", objectKey, originalName, fileExt, stored.MimeType, stored.SizeBytes, stored.Sha256, "active", context.ActorUserId, context.Now), cancellationToken);
                await AuditAsync(context, "upload", id, "File uploaded and metadata created.", cancellationToken);
                return new FileMutationResponse(id);
            }
        }
        catch (DomainException exception)
        {
            await SecurityEventAsync(context, "file_upload_rejected", "warning", $"File upload rejected: {exception.Message}", cancellationToken);
            throw;
        }
    }

    private async Task ScanAsync(FileUploadContent uploadContent, string originalName, string mimeType, long sizeBytes, string policyCode, CancellationToken cancellationToken)
    {
        uploadContent.Rewind();
        var scanResult = await _fileScanService.ScanAsync(
            uploadContent.Content,
            new FileScanRequest(originalName, mimeType, sizeBytes, policyCode),
            cancellationToken);
        if (!scanResult.Clean)
        {
            throw Validation("file scan rejected uploaded content.");
        }
    }

    public async Task DeleteAsync(long id, FileRequestContext context, CancellationToken cancellationToken)
    {
        _ = await GetAsync(id, cancellationToken);
        await _repository.SoftDeleteAsync(id, context.Now, cancellationToken);
        await AuditAsync(context, "delete", id, "File metadata deleted.", cancellationToken);
    }

    public async Task<FileDownloadPayload> GetDownloadPayloadAsync(long id, bool inline, FileRequestContext context, CancellationToken cancellationToken)
    {
        var file = await _repository.GetDownloadAsync(id, cancellationToken) ?? throw new DomainException(ApiCodes.NotFound, "File was not found.");
        if (!string.Equals(file.Status, "active", StringComparison.OrdinalIgnoreCase))
        {
            throw new DomainException(ApiCodes.NotFound, "File was not found.");
        }

        var policy = ResolveDownloadPolicy(file.FileExt, file.MimeType);
        var canInline = inline && policy.AllowPreview && policy.AllowedMimeTypes.Contains(file.MimeType);
        var stream = await _storage.OpenReadAsync(file.ObjectKey, cancellationToken);
        var action = canInline ? "preview" : "download";
        await AuditAsync(context, action, id, $"File {action} requested.", cancellationToken);

        var fileName = FileNameSafety.NormalizeFileName(file.OriginalName, "fileName", 255);
        return new FileDownloadPayload(stream, file.MimeType, fileName, file.SizeBytes, canInline);
    }

    private IFileUploadPolicy ResolveDownloadPolicy(string fileExt, string mimeType)
    {
        foreach (var policyCode in new[] { "image", "document" })
        {
            var policy = _policyResolver.Resolve(policyCode);
            if (policy.AllowedExtensions.Contains(fileExt) && policy.AllowedMimeTypes.Contains(mimeType))
            {
                return policy;
            }
        }

        return _policyResolver.Resolve("document");
    }

    private async Task DeleteFileAsync(string objectKey, CancellationToken cancellationToken)
    {
        try
        {
            await _storage.DeleteAsync(objectKey, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Failed to cleanup file after upload failure. objectKey={ObjectKey}", objectKey);
        }
    }

    private Task AuditAsync(FileRequestContext context, string action, long targetFileId, string detail, CancellationToken cancellationToken)
    {
        return _repository.RecordAuditAsync(new FileAuditRecord(context.ActorUserId, context.ActorUsername, action, targetFileId, context.Ip, context.UserAgent, context.TraceId, "success", detail, context.Now), cancellationToken);
    }

    private Task SecurityEventAsync(FileRequestContext context, string eventType, string severity, string message, CancellationToken cancellationToken)
    {
        return _repository.RecordSecurityEventAsync(new FileSecurityEventRecord(eventType, context.ActorUserId, context.ActorUsername, context.Ip, severity, message, context.Now, context.TraceId), cancellationToken);
    }

    private static string NormalizeSha256(string? value)
    {
        var normalized = NormalizeRequired(value, "sha256", 64).ToLowerInvariant();
        return normalized.Length == 64 && normalized.All(Uri.IsHexDigit) ? normalized : throw Validation("sha256 must be 64 hex characters.");
    }

    private static string NormalizeRequired(string? value, string name, int maxLength) => NormalizeOptional(value, maxLength) ?? throw Validation($"{name} is required.");

    private static string? NormalizeOptional(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        return normalized.Length <= maxLength ? normalized : throw Validation($"value must be {maxLength} characters or fewer.");
    }

    private static DomainException Validation(string message) => new(ApiCodes.ValidationError, message);
}
