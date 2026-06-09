namespace WeCms.Modules.System.Roles;

public interface IRoleService
{
    Task<(IReadOnlyList<RoleListItem> Items, long Total)> ListAsync(int page, int size, CancellationToken ct);
    Task<RoleDetail?> GetByIdAsync(long id, CancellationToken ct);
    Task<long> CreateAsync(CreateRoleRequest req, CancellationToken ct);
    Task UpdateAsync(long id, UpdateRoleRequest req, CancellationToken ct);
    Task DeleteAsync(long id, CancellationToken ct);
    Task AssignMenusAsync(long roleId, long[] menuIds, CancellationToken ct);
    Task AssignPermissionsAsync(long roleId, long[] permIds, CancellationToken ct);
}
