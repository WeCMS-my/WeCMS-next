using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using WeCms.Modules.Platform;
using WeCms.Modules.Platform.System;
using WeCms.Shared;

namespace WeCms.Tests.Unit.Api;

public sealed class PlatformEndpointDefinitionTests
{
    [Fact]
    public void MapPlatformEndpoints_MapsSystemPingEndpoint()
    {
        var endpoints = new TestEndpointRouteBuilder();

        endpoints.MapPlatformEndpoints();

        var endpoint = endpoints.DataSources
            .SelectMany(static dataSource => dataSource.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(static routeEndpoint => routeEndpoint.RoutePattern.RawText == "/api/v1/system/ping");
        var responseMetadata = endpoint.Metadata.GetMetadata<OpenApiResponseMetadata>();

        Assert.Equal("GET", Assert.Single(endpoint.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods));
        Assert.NotNull(endpoint.Metadata.GetMetadata<IAllowAnonymous>());
        Assert.NotNull(responseMetadata);
        Assert.Equal(typeof(SystemPingResponse), responseMetadata.ResponseType);
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
