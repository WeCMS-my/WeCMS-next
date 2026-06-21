using Microsoft.AspNetCore.Routing;
using WeCms.Modules.Audit.Logs;

namespace WeCms.Modules.Audit;

public static class AuditEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapAuditEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapAuditLogEndpoints();
        endpoints.MapLoginLogEndpoints();
        return endpoints;
    }
}
