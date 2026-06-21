using WeCms.Modules.AccessControl.Contracts;
using WeCms.Modules.AccessControl.Records;

namespace WeCms.Modules.AccessControl.Repositories;

public interface IMenuRepository
{
    Task<IReadOnlyList<MenuSummaryDto>> ListAsync(CancellationToken cancellationToken);
    Task<MenuDetailDto?> GetAsync(long id, CancellationToken cancellationToken);
    Task<bool> CodeExistsAsync(string code, long? exceptMenuId, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(long id, CancellationToken cancellationToken);
    Task<bool> HasChildrenAsync(long id, CancellationToken cancellationToken);
    Task<bool> IsDescendantAsync(long id, long candidateParentId, CancellationToken cancellationToken);
    Task<long> CreateAsync(MenuCreateRecord record, CancellationToken cancellationToken);
    Task UpdateAsync(MenuUpdateRecord record, CancellationToken cancellationToken);
    Task SortAsync(IReadOnlyList<MenuSortRecord> records, CancellationToken cancellationToken);
    Task SoftDeleteAsync(long id, DateTimeOffset now, CancellationToken cancellationToken);
    Task SetStatusAsync(long id, string status, DateTimeOffset now, CancellationToken cancellationToken);
    Task RecordAuditAsync(MenuAuditRecord record, CancellationToken cancellationToken);
}
