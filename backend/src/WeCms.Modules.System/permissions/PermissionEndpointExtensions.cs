using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace WeCms.Modules.System.Permissions;

public static class PermissionEndpointExtensions
{
    public static RouteHandlerBuilder RequirePermission(
        this RouteHandlerBuilder builder,
        string permissionCode)
    {
        return builder
            .RequireAuthorization()
            .WithMetadata(new Shared.Security.PermissionMetadata(permissionCode))
            .AddEndpointFilter<PermissionEndpointFilter>();
    }
}
