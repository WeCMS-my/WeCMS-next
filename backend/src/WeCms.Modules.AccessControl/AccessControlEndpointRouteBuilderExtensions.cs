using Microsoft.AspNetCore.Routing;
using WeCms.Modules.AccessControl.Menus;
using WeCms.Modules.AccessControl.Permissions;
using WeCms.Modules.AccessControl.Roles;

namespace WeCms.Modules.AccessControl;

public static class AccessControlEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapAccessControlEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapMenuEndpoints();
        endpoints.MapPermissionManagementEndpoints();
        endpoints.MapRoleEndpoints();

        return endpoints;
    }
}
