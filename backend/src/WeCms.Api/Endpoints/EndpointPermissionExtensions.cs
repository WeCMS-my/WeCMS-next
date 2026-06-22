using Microsoft.AspNetCore.Builder;
using WeCms.Modules.AccessControl.Permissions;
using WeCms.Shared.Endpoints;

namespace WeCms.Api.Endpoints;

public static class EndpointPermissionExtensions
{
    public static RouteHandlerBuilder RequireEndpointPermission(
        this RouteHandlerBuilder builder,
        string permissionCode)
    {
        return RequirePermission(builder, permissionCode, EndpointPermissionKind.Api);
    }

    public static RouteHandlerBuilder RequirePermission(
        this RouteHandlerBuilder builder,
        string permissionCode)
    {
        return RequirePermission(builder, permissionCode, EndpointPermissionKind.Api);
    }

    public static RouteHandlerBuilder RequireButtonPermission(
        this RouteHandlerBuilder builder,
        string permissionCode)
    {
        return RequirePermission(builder, permissionCode, EndpointPermissionKind.Button);
    }

    public static RouteHandlerBuilder RequireUrlPermission(
        this RouteHandlerBuilder builder,
        string permissionCode)
    {
        return RequirePermission(builder, permissionCode, EndpointPermissionKind.Url);
    }

    private static RouteHandlerBuilder RequirePermission(
        RouteHandlerBuilder builder,
        string permissionCode,
        EndpointPermissionKind kind)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionCode);

        return builder
            .WithMetadata(new EndpointPermissionMetadata(permissionCode, kind))
            .AddEndpointFilter<PermissionEndpointFilter>();
    }
}
