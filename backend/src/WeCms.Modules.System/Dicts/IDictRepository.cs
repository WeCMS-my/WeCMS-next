using WeCms.Shared;

namespace WeCms.Modules.System.Dicts;

public interface IDictRepository
{
    Task<PagedResult<DictTypeSummaryDto>> ListTypesAsync(DictTypeListCriteria criteria, CancellationToken cancellationToken);
    Task<DictTypeDetailDto?> GetTypeAsync(long id, CancellationToken cancellationToken);
    Task<DictTypeDetailDto?> GetTypeByCodeAsync(string code, CancellationToken cancellationToken);
    Task<bool> TypeCodeExistsAsync(string code, long? exceptTypeId, CancellationToken cancellationToken);
    Task<bool> TypeHasValuesAsync(long id, CancellationToken cancellationToken);
    Task<long> CreateTypeAsync(DictTypeCreateRecord record, CancellationToken cancellationToken);
    Task UpdateTypeAsync(DictTypeUpdateRecord record, CancellationToken cancellationToken);
    Task SetTypeStatusAsync(long id, string status, DateTimeOffset now, CancellationToken cancellationToken);
    Task DisableValuesByTypeAsync(long typeId, DateTimeOffset now, CancellationToken cancellationToken);
    Task SoftDeleteTypeAsync(long id, DateTimeOffset now, CancellationToken cancellationToken);
    Task<IReadOnlyList<DictValueDto>> ListValuesAsync(string typeCode, CancellationToken cancellationToken);
    Task<DictValueDto?> GetValueAsync(long id, CancellationToken cancellationToken);
    Task<bool> ValueExistsAsync(long typeId, string value, long? exceptValueId, CancellationToken cancellationToken);
    Task<long> CreateValueAsync(DictValueCreateRecord record, CancellationToken cancellationToken);
    Task UpdateValueAsync(DictValueUpdateRecord record, CancellationToken cancellationToken);
    Task SetValueStatusAsync(long id, string status, DateTimeOffset now, CancellationToken cancellationToken);
    Task SoftDeleteValueAsync(long id, DateTimeOffset now, CancellationToken cancellationToken);
    Task RecordAuditAsync(DictAuditRecord record, CancellationToken cancellationToken);
}
