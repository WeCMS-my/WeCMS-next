using WeCms.Shared;

namespace WeCms.Modules.System.Files;

public sealed class FileService : IFileService
{
    private const int MaxPageSize = 100;
    private const long MaxSizeBytes = 10 * 1024 * 1024;
    private static readonly HashSet<string> AllowedMimeTypes = new(StringComparer.OrdinalIgnoreCase) { "image/jpeg", "image/png", "image/webp", "application/pdf", "text/plain" };
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp", ".pdf", ".txt" };
    private readonly IFileRepository _repository;

    public FileService(IFileRepository repository)
    {
        _repository = repository;
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

    public async Task<FileMutationResponse> CreateAsync(CreateFileRequest request, FileRequestContext context, CancellationToken cancellationToken)
    {
        var originalName = NormalizeRequired(request.OriginalName, "originalName", 255);
        var mimeType = NormalizeRequired(request.MimeType, "mimeType", 120);
        var sha256 = NormalizeSha256(request.Sha256);
        if (request.SizeBytes <= 0 || request.SizeBytes > MaxSizeBytes)
        {
            throw Validation($"sizeBytes must be between 1 and {MaxSizeBytes}.");
        }

        if (!AllowedMimeTypes.Contains(mimeType))
        {
            throw Validation("mimeType is not allowed.");
        }

        var fileExt = Path.GetExtension(originalName);
        if (string.IsNullOrWhiteSpace(fileExt) || !AllowedExtensions.Contains(fileExt))
        {
            throw Validation("file extension is not allowed.");
        }

        var id = await _repository.CreateAsync(new FileCreateRecord("metadata", "system", sha256, originalName, fileExt.ToLowerInvariant(), mimeType, request.SizeBytes, sha256, "active", context.ActorUserId, context.Now), cancellationToken);
        await AuditAsync(context, "upload", id, "File metadata created.", cancellationToken);
        return new FileMutationResponse(id);
    }

    public async Task DeleteAsync(long id, FileRequestContext context, CancellationToken cancellationToken)
    {
        _ = await GetAsync(id, cancellationToken);
        await _repository.SoftDeleteAsync(id, context.Now, cancellationToken);
        await AuditAsync(context, "delete", id, "File metadata deleted.", cancellationToken);
    }

    private Task AuditAsync(FileRequestContext context, string action, long targetFileId, string detail, CancellationToken cancellationToken)
    {
        return _repository.RecordAuditAsync(new FileAuditRecord(context.ActorUserId, context.ActorUsername, action, targetFileId, context.Ip, context.UserAgent, context.TraceId, "success", detail, context.Now), cancellationToken);
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
