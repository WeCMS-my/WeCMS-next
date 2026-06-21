using Microsoft.AspNetCore.Routing;
using WeCms.Modules.Security.Events;

namespace WeCms.Modules.Security;

public static class SecurityEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapSecurityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapSecurityManagementEndpoints();
        endpoints.MapSecurityEventEndpoints();
        return endpoints;
    }
}
