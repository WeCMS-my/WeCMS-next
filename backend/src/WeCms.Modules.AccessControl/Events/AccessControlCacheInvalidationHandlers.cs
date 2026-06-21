using WeCms.EventBus;
using WeCms.Modules.AccessControl.Permissions;

namespace WeCms.Modules.AccessControl.Events;

public sealed class RolePermissionsChangedCacheHandler(IAccessControlPermissionVersionService permissionVersionService)
    : IEventHandler<RolePermissionsChangedEvent>
{
    public Task HandleAsync(RolePermissionsChangedEvent integrationEvent, CancellationToken cancellationToken)
    {
        return permissionVersionService.BumpUsersByRoleAsync(integrationEvent.RoleId, integrationEvent.OccurredAt, cancellationToken);
    }
}

public sealed class MenuChangedCacheHandler(IAccessControlPermissionVersionService permissionVersionService)
    : IEventHandler<MenuChangedEvent>
{
    public Task HandleAsync(MenuChangedEvent integrationEvent, CancellationToken cancellationToken)
    {
        return permissionVersionService.BumpUsersByMenusAsync(integrationEvent.MenuIds, integrationEvent.OccurredAt, cancellationToken);
    }
}
