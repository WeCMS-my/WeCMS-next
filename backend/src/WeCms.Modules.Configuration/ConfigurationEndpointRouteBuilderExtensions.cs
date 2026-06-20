using Microsoft.AspNetCore.Routing;

namespace WeCms.Modules.Configuration;

public static class ConfigurationEndpointRouteBuilderExtensions
{
    public static IEndpointRouteBuilder MapConfigurationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        return endpoints;
    }
}
