namespace WeCms.Modules.System.Permissions;

public interface IPermissionRepository
{
    Task<PermissionUserRecord?> FindUserAsync(long userId, CancellationToken cancellationToken);

    Task<bool> UserHasPermissionAsync(long userId, string permissionCode, CancellationToken cancellationToken);
}
