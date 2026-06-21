using WeCms.Shared;

namespace WeCms.Modules.Organization.Positions;

public interface IPositionRepository
{
    Task<PagedResult<PositionSummaryDto>> ListAsync(PositionListCriteria criteria, CancellationToken cancellationToken);
    Task<PositionDetailDto?> GetAsync(long id, CancellationToken cancellationToken);
    Task<bool> CodeExistsAsync(string code, long? exceptPositionId, CancellationToken cancellationToken);
    Task<IReadOnlySet<long>> ExistingIdsAsync(IReadOnlyList<long> ids, CancellationToken cancellationToken);
    Task<bool> HasUsersAsync(long id, CancellationToken cancellationToken);
    Task<long> CreateAsync(PositionCreateRecord record, CancellationToken cancellationToken);
    Task UpdateAsync(PositionUpdateRecord record, CancellationToken cancellationToken);
    Task SoftDeleteAsync(long id, DateTimeOffset now, CancellationToken cancellationToken);
    Task SetStatusAsync(long id, string status, DateTimeOffset now, CancellationToken cancellationToken);
    Task RecordAuditAsync(PositionAuditRecord record, CancellationToken cancellationToken);
}
