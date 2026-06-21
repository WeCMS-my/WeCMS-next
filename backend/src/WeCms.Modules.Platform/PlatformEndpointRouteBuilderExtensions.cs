using Microsoft.AspNetCore.Routing;
using WeCms.Modules.Platform.System;

namespace WeCms.Modules.Platform;

public static class PlatformEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapPlatformEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapPlatformSystemEndpoints();
        return endpoints;
    }
}
