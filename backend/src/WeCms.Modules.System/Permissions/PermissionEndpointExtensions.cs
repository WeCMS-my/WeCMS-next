using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WeCms.Shared;

namespace WeCms.Modules.System.Permissions;

public static class PermissionEndpointExtensions
{
    public static RouteHandlerBuilder RequirePermission(
        this RouteHandlerBuilder builder,
        string permissionCode)
    {
        return builder
            .WithMetadata(new PermissionMetadata(permissionCode))
            .AddEndpointFilter<PermissionEndpointFilter>();
    }

    public static IEndpointRouteBuilder MapSystemPermissionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/v1/system/secure-ping", () =>
                Results.Ok(ApiResult<SecurePingResponse>.Ok(new SecurePingResponse("ok"))))
            .WithMetadata(new OpenApiResponseMetadata(typeof(SecurePingResponse)))
            .RequireAuthorization()
            .RequirePermission(SystemPermissions.SecurePing);

        return endpoints;
    }
}
