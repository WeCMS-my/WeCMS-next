using Microsoft.AspNetCore.Routing;

namespace WeCms.Modules.Security;

public static class SecurityEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapSecurityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        return endpoints;
    }
}
