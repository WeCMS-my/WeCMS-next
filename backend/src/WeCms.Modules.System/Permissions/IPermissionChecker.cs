namespace WeCms.Modules.System.Permissions;

public interface IPermissionChecker
{
    Task<PermissionCheckResult> CheckAsync(long userId, string permissionCode, CancellationToken cancellationToken);
}
