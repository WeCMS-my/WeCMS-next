namespace WeCms.Modules.AccessControl.Permissions;

public interface IAccessControlPermissionVersionService
{
    Task BumpUserAsync(long userId, DateTimeOffset now, CancellationToken cancellationToken);

    Task BumpUsersByRoleAsync(long roleId, DateTimeOffset now, CancellationToken cancellationToken);

    Task BumpUsersByPermissionAsync(long permissionId, DateTimeOffset now, CancellationToken cancellationToken);

    Task BumpUsersByMenuAsync(long menuId, DateTimeOffset now, CancellationToken cancellationToken);

    Task BumpUsersByMenusAsync(IReadOnlyList<long> menuIds, DateTimeOffset now, CancellationToken cancellationToken);
}
