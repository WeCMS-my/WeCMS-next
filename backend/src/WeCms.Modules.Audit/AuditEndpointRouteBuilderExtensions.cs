using Microsoft.AspNetCore.Routing;

namespace WeCms.Modules.Audit;

public static class AuditEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapAuditEndpoints(this IEndpointRouteBuilder endpoints)
    {
        return endpoints;
    }
}
