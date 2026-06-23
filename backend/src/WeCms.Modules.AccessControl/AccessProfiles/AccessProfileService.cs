using WeCms.Modules.AccessControl.Contracts;
using WeCms.Modules.AccessControl.Menus;
using WeCms.Modules.AccessControl.Repositories;

namespace WeCms.Modules.AccessControl.AccessProfiles;

public sealed class AccessProfileService : IAccessProfileService
{
    private const string ButtonPermissionMarker = ":button:";
    private readonly IAccessProfileRepository _repository;
    private readonly IAccessProfileCache _cache;

    public AccessProfileService(IAccessProfileRepository repository, IAccessProfileCache cache)
    {
        _repository = repository;
        _cache = cache;
    }

    public async Task<AccessProfileDto> GetAsync(long userId, CancellationToken cancellationToken)
    {
        var permissionVersion = await _repository.GetPermissionVersionAsync(userId, cancellationToken);
        var cached = await _cache.GetAsync(userId, permissionVersion, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var roles = await _repository.ListRoleCodesAsync(userId, cancellationToken);
        var permissions = await _repository.ListPermissionCodesAsync(userId, cancellationToken);
        var menus = await _repository.ListVisibleMenusAsync(userId, cancellationToken);
        var buttons = permissions
            .Where(static permission => permission.Contains(ButtonPermissionMarker, StringComparison.Ordinal))
            .OrderBy(static permission => permission, StringComparer.Ordinal)
            .ToArray();

        var profile = new AccessProfileDto(
            permissionVersion,
            roles,
            permissions,
            buttons,
            MenuTreeBuilder.Build(menus));
        await _cache.SetAsync(userId, profile, cancellationToken);
        return profile;
    }
}
