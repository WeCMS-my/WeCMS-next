using Microsoft.AspNetCore.Routing;

namespace WeCms.Modules.Identity;

public static class IdentityEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        return endpoints;
    }
}
