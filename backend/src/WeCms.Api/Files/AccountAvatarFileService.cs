using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using WeCms.Modules.FileCenter.Files;
using WeCms.Modules.Identity.Contracts;
using WeCms.Modules.Identity.Services;
using WeCms.Shared;

namespace WeCms.Api.Files;

public sealed class AccountAvatarFileService : IAccountAvatarFileService
{
    private readonly IFileStorage _storage;
    private readonly IFileScanService _fileScanService;
    private readonly IFileObjectKeyGenerator _objectKeyGenerator;
    private readonly IFileUploadPolicyResolver _policyResolver;
    private readonly IFileUploadConcurrencyGate _uploadConcurrencyGate;
    private readonly FileUploadOptions _fileUploadOptions;

    public AccountAvatarFileService(
        IFileStorage storage,
        IFileScanService fileScanService,
        IFileObjectKeyGenerator objectKeyGenerator,
        IFileUploadPolicyResolver policyResolver,
        IFileUploadConcurrencyGate uploadConcurrencyGate,
        IOptions<FileUploadOptions> fileUploadOptions)
    {
        _storage = storage;
        _fileScanService = fileScanService;
        _objectKeyGenerator = objectKeyGenerator;
        _policyResolver = policyResolver;
        _uploadConcurrencyGate = uploadConcurrencyGate;
        _fileUploadOptions = fileUploadOptions.Value;
    }

    public async Task<AccountAvatarStoredFile> StoreAsync(
        AccountAvatarUploadRequest request,
        IFormFile file,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var originalName = FileNameSafety.NormalizeFileName(request.OriginalName, "originalName", 255);
        var mimeType = NormalizeRequired(request.MimeType, "mimeType", 120);
        var sha256 = NormalizeSha256(request.Sha256);
        var ext = FileNameSafety.NormalizeFileExtension(originalName);
        var policy = _policyResolver.Resolve("avatar");
        if (request.SizeBytes <= 0 || request.SizeBytes > policy.MaxSizeBytes)
        {
            throw Validation($"Avatar size must be between 1 and {policy.MaxSizeBytes} bytes.");
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
                throw Validation("Avatar content does not match declared metadata.");
            }

            await policy.ValidateContentAsync(uploadContent.Content, uploadContent.SizeBytes, mimeType, ext, cancellationToken);
            await ScanAvatarAsync(uploadContent, originalName, mimeType, request.SizeBytes, policy.Code, cancellationToken);

            var objectKey = $"{policy.StorageScope}/{_objectKeyGenerator.GenerateObjectKey(now, ext)}";
            uploadContent.Rewind();
            var stored = await _storage.StoreAsync(uploadContent.Content, objectKey, ext, policy.MaxSizeBytes, cancellationToken);
            if (stored.SizeBytes != request.SizeBytes
                || !string.Equals(stored.Sha256, sha256, StringComparison.OrdinalIgnoreCase)
                || !string.Equals(stored.MimeType, mimeType, StringComparison.OrdinalIgnoreCase))
            {
                await _storage.DeleteAsync(objectKey, cancellationToken);
                throw Validation("Avatar content does not match declared metadata.");
            }

            return new AccountAvatarStoredFile(objectKey, stored.MimeType, ext);
        }
    }

    public async Task<AccountAvatarDownload> OpenAsync(
        string objectKey,
        string mimeType,
        string fileExtension,
        CancellationToken cancellationToken)
    {
        var normalizedExtension = FileNameSafety.NormalizeStoredFileExtension(fileExtension);
        return new AccountAvatarDownload(
            await _storage.OpenReadAsync(objectKey, cancellationToken),
            mimeType,
            $"avatar{normalizedExtension}",
            0,
            OwnsStream: true);
    }

    public Task DeleteAsync(string objectKey, CancellationToken cancellationToken)
    {
        return _storage.DeleteAsync(objectKey, cancellationToken);
    }

    private async Task ScanAvatarAsync(FileUploadContent uploadContent, string originalName, string mimeType, long sizeBytes, string policyCode, CancellationToken cancellationToken)
    {
        uploadContent.Rewind();
        var scanResult = await _fileScanService.ScanAsync(
            uploadContent.Content,
            new FileScanRequest(originalName, mimeType, sizeBytes, policyCode),
            cancellationToken);
        if (!scanResult.Clean)
        {
            throw Validation("Avatar scan rejected uploaded content.");
        }
    }

    private static string NormalizeSha256(string? value)
    {
        var normalized = NormalizeRequired(value, "sha256", 64).ToLowerInvariant();
        return normalized.Length == 64 && normalized.All(Uri.IsHexDigit) ? normalized : throw Validation("sha256 must be 64 hex characters.");
    }

    private static string NormalizeRequired(string? value, string name, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw Validation($"{name} is required.");
        }

        var normalized = value.Trim();
        return normalized.Length <= maxLength ? normalized : throw Validation($"value must be {maxLength} characters or fewer.");
    }

    private static DomainException Validation(string message) => new(ApiCodes.ValidationError, message);
}
