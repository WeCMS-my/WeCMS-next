namespace WeCms.Modules.System.Dicts;

public interface IDictService
{
    Task<(IReadOnlyList<DictTypeItem> Types, long Total)> GetTypesAsync(int page, int size, CancellationToken ct);
    Task<(IReadOnlyList<DictValueItem> Values, long Total)> GetValuesAsync(long typeId, int page, int size, CancellationToken ct);
    Task<long> CreateTypeAsync(CreateDictTypeRequest req, CancellationToken ct);
    Task<long> CreateValueAsync(CreateDictValueRequest req, CancellationToken ct);
    Task DeleteTypeAsync(long id, CancellationToken ct);
    Task DeleteValueAsync(long id, CancellationToken ct);
}
