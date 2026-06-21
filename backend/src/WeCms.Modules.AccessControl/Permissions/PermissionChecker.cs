using WeCms.Modules.AccessControl.Records;
using WeCms.Modules.AccessControl.Repositories;
using WeCms.Shared.Endpoints;

namespace WeCms.Modules.AccessControl.Permissions;

public sealed class PermissionChecker : IPermissionChecker, IEndpointPermissionChecker
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

    async Task<EndpointPermissionCheckResult> IEndpointPermissionChecker.CheckAsync(
        long userId,
        string permissionCode,
        CancellationToken cancellationToken)
    {
        return await CheckAsync(userId, permissionCode, cancellationToken) switch
        {
            PermissionCheckResult.Allowed => EndpointPermissionCheckResult.Allowed,
            PermissionCheckResult.UserDisabled => EndpointPermissionCheckResult.UserDisabled,
            PermissionCheckResult.Forbidden => EndpointPermissionCheckResult.Forbidden,
            _ => throw new InvalidOperationException("Unknown permission check result.")
        };
    }
}
