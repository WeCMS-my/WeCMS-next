namespace WeCms.Modules.System.Dicts;

public interface IDictService
{
    Task<List<DictTypeItem>> GetTypesAsync(CancellationToken ct);
    Task<List<DictValueItem>> GetValuesAsync(long typeId, CancellationToken ct);
    Task<long> CreateTypeAsync(CreateDictTypeRequest req, CancellationToken ct);
    Task<long> CreateValueAsync(CreateDictValueRequest req, CancellationToken ct);
    Task DeleteTypeAsync(long id, CancellationToken ct);
    Task DeleteValueAsync(long id, CancellationToken ct);
}
