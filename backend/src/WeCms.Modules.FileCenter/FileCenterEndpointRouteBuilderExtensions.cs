using Microsoft.AspNetCore.Routing;
using WeCms.Modules.FileCenter.Files;

namespace WeCms.Modules.FileCenter;

public static class FileCenterEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapFileCenterEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapFileEndpoints();
        return endpoints;
    }
}
