using WeCms.Shared;

namespace WeCms.Modules.System.Files;

public interface IFileRepository
{
    Task<PagedResult<FileSummaryDto>> ListAsync(FileListCriteria criteria, CancellationToken cancellationToken);
    Task<FileDetailDto?> GetAsync(long id, CancellationToken cancellationToken);
    Task<long> CreateAsync(FileCreateRecord record, CancellationToken cancellationToken);
    Task SoftDeleteAsync(long id, DateTimeOffset now, CancellationToken cancellationToken);
    Task RecordAuditAsync(FileAuditRecord record, CancellationToken cancellationToken);
}
