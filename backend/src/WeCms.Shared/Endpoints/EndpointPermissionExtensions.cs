using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace WeCms.Shared.Endpoints;

public interface IEndpointPermissionFilter : IEndpointFilter
{
}

public static class EndpointPermissionExtensions
{
    public static RouteHandlerBuilder RequireEndpointPermission(
        this RouteHandlerBuilder builder,
        string permissionCode)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionCode);

        return builder
            .WithMetadata(new EndpointPermissionMetadata(permissionCode, EndpointPermissionKind.Api))
            .AddEndpointFilterFactory(static (_, next) =>
            {
                return async context =>
                {
                    var filter = context.HttpContext.RequestServices.GetRequiredService<IEndpointPermissionFilter>();

                    return await filter.InvokeAsync(context, next);
                };
            });
    }
}
