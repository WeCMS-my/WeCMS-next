using WeCms.Modules.AccessControl.Contracts;
using WeCms.Modules.AccessControl.Menus;
using WeCms.Modules.AccessControl.Repositories;

namespace WeCms.Modules.AccessControl.AccessProfiles;

public sealed class AccessProfileService : IAccessProfileService
{
    private const string ButtonPermissionMarker = ":button:";
    private readonly IAccessProfileRepository _repository;

    public AccessProfileService(IAccessProfileRepository repository)
    {
        _repository = repository;
    }

    public async Task<AccessProfileDto> GetAsync(long userId, bool isSuperAdmin, CancellationToken cancellationToken)
    {
        var permissionVersion = await _repository.GetPermissionVersionAsync(userId, cancellationToken);
        var roles = await _repository.ListRoleCodesAsync(userId, cancellationToken);
        var permissions = await _repository.ListPermissionCodesAsync(userId, cancellationToken);
        var menus = await _repository.ListVisibleMenusAsync(userId, isSuperAdmin, cancellationToken);
        var buttons = permissions
            .Where(static permission => permission.Contains(ButtonPermissionMarker, StringComparison.Ordinal))
            .OrderBy(static permission => permission, StringComparer.Ordinal)
            .ToArray();

        return new AccessProfileDto(
            permissionVersion,
            roles,
            permissions,
            buttons,
            MenuTreeBuilder.Build(menus));
    }
}
