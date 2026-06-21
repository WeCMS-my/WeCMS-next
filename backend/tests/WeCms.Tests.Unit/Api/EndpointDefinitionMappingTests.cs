using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using WeCms.Api.Endpoints;
using WeCms.Shared.Endpoints;

namespace WeCms.Tests.Unit.Api;

public sealed class EndpointDefinitionMappingTests
{
    [Fact]
    public void MapEndpointDefinitions_RegistersRoutesFromExplicitDefinitions()
    {
        var endpoints = new TestEndpointRouteBuilder();

        endpoints.MapEndpointDefinitions([new PingEndpointDefinition("/s2/ping")]);

        Assert.Contains("/s2/ping", GetRoutePatterns(endpoints));
    }

    [Fact]
    public void MapEndpointDefinitions_RegistersRoutesFromRegistryConfiguration()
    {
        var endpoints = new TestEndpointRouteBuilder();

        endpoints.MapEndpointDefinitions(registry =>
        {
            registry.Add(new PingEndpointDefinition("/s2/registry-ping"));
        });

        Assert.Contains("/s2/registry-ping", GetRoutePatterns(endpoints));
    }

    private static IReadOnlyList<string?> GetRoutePatterns(IEndpointRouteBuilder endpoints)
    {
        return endpoints.DataSources
            .SelectMany(static dataSource => dataSource.Endpoints)
            .OfType<RouteEndpoint>()
            .Select(static endpoint => endpoint.RoutePattern.RawText)
            .ToArray();
    }

    private sealed class PingEndpointDefinition(string route) : IEndpointDefinition
    {
        public void MapEndpoints(IEndpointRouteBuilder endpoints)
        {
            endpoints.MapGet(route, () => "pong");
        }
    }

    private sealed class TestEndpointRouteBuilder : IEndpointRouteBuilder
    {
        public IServiceProvider ServiceProvider { get; } = new ServiceCollection().BuildServiceProvider();

        public ICollection<EndpointDataSource> DataSources { get; } = [];

        public IApplicationBuilder CreateApplicationBuilder()
        {
            return new ApplicationBuilder(ServiceProvider);
        }
    }
}
