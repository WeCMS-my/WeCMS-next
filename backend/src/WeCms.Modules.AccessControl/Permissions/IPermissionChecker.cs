using WeCms.Modules.AccessControl.Records;

namespace WeCms.Modules.AccessControl.Permissions;

public interface IPermissionChecker
{
    Task<PermissionCheckResult> CheckAsync(long userId, string permissionCode, CancellationToken cancellationToken);
}
