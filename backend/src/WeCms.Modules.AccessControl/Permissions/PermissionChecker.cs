using WeCms.Modules.AccessControl.Records;
using WeCms.Modules.AccessControl.Repositories;

namespace WeCms.Modules.AccessControl.Permissions;

public sealed class PermissionChecker : IPermissionChecker
{
    private const string EnabledStatus = "enabled";
    private readonly IPermissionRepository _repository;

    public PermissionChecker(IPermissionRepository repository)
    {
        _repository = repository;
    }

    public async Task<PermissionCheckResult> CheckAsync(
        long userId,
        string permissionCode,
        CancellationToken cancellationToken)
    {
        var user = await _repository.FindUserAsync(userId, cancellationToken);
        if (user is null || !string.Equals(user.Status, EnabledStatus, StringComparison.Ordinal))
        {
            return PermissionCheckResult.UserDisabled;
        }

        return await _repository.UserHasPermissionAsync(userId, permissionCode, cancellationToken)
            ? PermissionCheckResult.Allowed
            : PermissionCheckResult.Forbidden;
    }
}
