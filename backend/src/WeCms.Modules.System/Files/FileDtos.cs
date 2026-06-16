using WeCms.Shared;

namespace WeCms.Modules.System.Files;

public sealed record FileListQuery(int Page = 1, int PageSize = 20, string? Keyword = null, string? MimeType = null, string? Status = null);

public sealed record FileSummaryDto(long Id, string OriginalName, string FileExt, string MimeType, long SizeBytes, string Sha256, string Status, long CreatedBy, DateTimeOffset CreatedAt);

public sealed record FileDetailDto(long Id, string OriginalName, string FileExt, string MimeType, long SizeBytes, string Sha256, string Status, long CreatedBy, DateTimeOffset CreatedAt);

public sealed record CreateFileRequest(string OriginalName, string MimeType, long SizeBytes, string Sha256);

public sealed record FileMutationResponse(long Id);

public interface IFileService
{
    Task<PagedResult<FileSummaryDto>> ListAsync(FileListQuery query, CancellationToken cancellationToken);
    Task<FileDetailDto> GetAsync(long id, CancellationToken cancellationToken);
    Task<FileMutationResponse> CreateAsync(CreateFileRequest request, FileRequestContext context, CancellationToken cancellationToken);
    Task DeleteAsync(long id, FileRequestContext context, CancellationToken cancellationToken);
}
