using Microsoft.AspNetCore.Routing;

namespace WeCms.Modules.AccessControl;

public static class AccessControlEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapAccessControlEndpoints(this IEndpointRouteBuilder endpoints)
    {
        return endpoints;
    }
}
