using WeCms.Shared;

namespace WeCms.Modules.FileCenter.Files;

public interface IFileRepository
{
    Task<PagedResult<FileSummaryDto>> ListAsync(FileListCriteria criteria, CancellationToken cancellationToken);
    Task<FileDetailDto?> GetAsync(long id, CancellationToken cancellationToken);
    Task<FileDownloadRecord?> GetDownloadAsync(long id, CancellationToken cancellationToken);
    Task<long> CreateAsync(FileCreateRecord record, CancellationToken cancellationToken);
    Task SoftDeleteAsync(long id, DateTimeOffset now, CancellationToken cancellationToken);
    Task RecordAuditAsync(FileAuditRecord record, CancellationToken cancellationToken);
    Task RecordSecurityEventAsync(FileSecurityEventRecord record, CancellationToken cancellationToken);
}
