using Microsoft.Extensions.DependencyInjection;
using WeCms.EventBus;
using WeCms.Modules.AccessControl.AccessProfiles;
using WeCms.Modules.AccessControl.Events;
using WeCms.Modules.AccessControl.Menus;
using WeCms.Modules.AccessControl.Permissions;
using WeCms.Modules.AccessControl.Roles;
using WeCms.Shared.Endpoints;

namespace WeCms.Modules.AccessControl;

public static class AccessControlServiceCollectionExtensions
{
    public static IServiceCollection AddWeCmsAccessControl(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IAccessControlClock, SystemAccessControlClock>();
        services.AddScoped<PermissionChecker>();
        services.AddScoped<IPermissionChecker>(provider => provider.GetRequiredService<PermissionChecker>());
        services.AddScoped<IEndpointPermissionFilter, PermissionEndpointFilter>();
        services.AddScoped<IAccessProfileService, AccessProfileService>();
        services.AddScoped<IPermissionManagementService, PermissionManagementService>();
        services.AddScoped<IMenuService, MenuService>();
        services.AddScoped<IRoleService, RoleService>();
        services
            .AddIntegrationEvent<RolePermissionsChangedEvent>(RolePermissionsChangedEvent.EventType)
            .AddIntegrationEvent<MenuChangedEvent>(MenuChangedEvent.EventType)
            .AddEventHandler<RolePermissionsChangedEvent, RolePermissionsChangedCacheHandler>()
            .AddEventHandler<MenuChangedEvent, MenuChangedCacheHandler>();

        return services;
    }
}
