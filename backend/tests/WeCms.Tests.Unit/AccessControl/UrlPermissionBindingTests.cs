using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using WeCms.Modules.AccessControl.Permissions;
using WeCms.Shared.Endpoints;

namespace WeCms.Tests.Unit.AccessControl;

public sealed class UrlPermissionBindingTests
{
    [Fact]
    public void FromEndpoint_ReturnsUrlPermissionBindingsOnly()
    {
        var endpoint = CreateRouteEndpoint(
            "/api/v1/system/users",
            ["GET"],
            new EndpointPermissionMetadata("sys:user:list", EndpointPermissionKind.Url),
            new EndpointPermissionMetadata("sys:user:create-button", EndpointPermissionKind.Button),
            new EndpointPermissionMetadata("sys:user:create", EndpointPermissionKind.Api));

        var binding = Assert.Single(UrlPermissionBindingFactory.FromEndpoint(endpoint));

        Assert.Equal("sys:user:list", binding.PermissionCode);
        Assert.Equal("identity", binding.Module);
        Assert.Equal("GET", binding.HttpMethod);
        Assert.Equal("/api/v1/system/users", binding.RoutePattern);
    }

    [Fact]
    public void FromEndpoint_ReturnsOneBindingPerHttpMethod()
    {
        var endpoint = CreateRouteEndpoint(
            "/api/v1/system/users/{id:long}",
            ["GET", "HEAD"],
            new EndpointPermissionMetadata("sys:user:detail", EndpointPermissionKind.Url));

        var bindings = UrlPermissionBindingFactory.FromEndpoint(endpoint);

        Assert.Equal(["GET", "HEAD"], bindings.Select(static binding => binding.HttpMethod));
        Assert.All(bindings, binding =>
        {
            Assert.Equal("sys:user:detail", binding.PermissionCode);
            Assert.Equal("/api/v1/system/users/{id:long}", binding.RoutePattern);
        });
    }

    [Fact]
    public void FromEndpoint_ReturnsEmptyWhenEndpointHasNoUrlPermission()
    {
        var endpoint = CreateRouteEndpoint(
            "/api/v1/system/users",
            ["POST"],
            new EndpointPermissionMetadata("sys:user:create", EndpointPermissionKind.Api));

        Assert.Empty(UrlPermissionBindingFactory.FromEndpoint(endpoint));
    }

    [Fact]
    public void FromEndpoint_FailsFastWhenUrlPermissionEndpointHasNoHttpMethod()
    {
        var endpoint = new RouteEndpoint(
            static _ => Task.CompletedTask,
            RoutePatternFactory.Parse("/api/v1/system/users"),
            order: 0,
            new EndpointMetadataCollection(
                new EndpointModuleMetadata("identity"),
                new EndpointPermissionMetadata("sys:user:list", EndpointPermissionKind.Url)),
            "users");

        var exception = Assert.Throws<InvalidOperationException>(() => UrlPermissionBindingFactory.FromEndpoint(endpoint));

        Assert.Equal("URL permission endpoint must declare HTTP methods.", exception.Message);
    }

    [Fact]
    public void FromEndpoint_FailsFastWhenUrlPermissionEndpointHasNoModule()
    {
        var endpoint = new RouteEndpoint(
            static _ => Task.CompletedTask,
            RoutePatternFactory.Parse("/api/v1/system/users"),
            order: 0,
            new EndpointMetadataCollection(
                new HttpMethodMetadata(["GET"]),
                new EndpointPermissionMetadata("sys:user:list", EndpointPermissionKind.Url)),
            "users");

        var exception = Assert.Throws<InvalidOperationException>(() => UrlPermissionBindingFactory.FromEndpoint(endpoint));

        Assert.Equal("URL permission endpoint must declare module metadata.", exception.Message);
    }

    private static RouteEndpoint CreateRouteEndpoint(
        string routePattern,
        IReadOnlyList<string> httpMethods,
        params EndpointPermissionMetadata[] permissions)
    {
        var metadata = new List<object>
        {
            new HttpMethodMetadata(httpMethods),
            new EndpointModuleMetadata("identity")
        };
        metadata.AddRange(permissions);

        return new RouteEndpoint(
            static _ => Task.CompletedTask,
            RoutePatternFactory.Parse(routePattern),
            order: 0,
            new EndpointMetadataCollection(metadata),
            routePattern);
    }
}
