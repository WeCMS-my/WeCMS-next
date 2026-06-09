namespace WeCms.Modules.System.Menus;

public interface IMenuService
{
    Task<List<MenuTreeItem>> GetTreeAsync(CancellationToken ct);
    Task<MenuDetail?> GetByIdAsync(long id, CancellationToken ct);
    Task<long> CreateAsync(CreateMenuRequest req, CancellationToken ct);
    Task UpdateAsync(long id, UpdateMenuRequest req, CancellationToken ct);
    Task DeleteAsync(long id, CancellationToken ct);
    Task SortAsync(long[] orderedIds, CancellationToken ct);
}
