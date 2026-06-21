using Microsoft.AspNetCore.Routing;
using WeCms.Shared.Endpoints;

namespace WeCms.Api.Endpoints;

public static class EndpointMappingExtensions
{
    public static IEndpointRouteBuilder MapEndpointDefinitions(
        this IEndpointRouteBuilder endpoints,
        IEnumerable<IEndpointDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(definitions);

        foreach (var definition in definitions)
        {
            ArgumentNullException.ThrowIfNull(definition);
            definition.MapEndpoints(endpoints);
        }

        return endpoints;
    }

    public static IEndpointRouteBuilder MapEndpointDefinitions(
        this IEndpointRouteBuilder endpoints,
        Action<EndpointDefinitionRegistry> configure)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        ArgumentNullException.ThrowIfNull(configure);

        var registry = new EndpointDefinitionRegistry();
        configure(registry);

        return endpoints.MapEndpointDefinitions(registry.Definitions);
    }
}
