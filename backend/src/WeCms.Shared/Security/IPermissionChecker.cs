namespace WeCms.Shared.Security;

public interface IPermissionChecker
{
    Task<PermissionCheckResult> CheckAsync(long userId, string permissionCode, CancellationToken cancellationToken);
}

public sealed record PermissionCheckResult(bool IsActive, bool HasPermission);
