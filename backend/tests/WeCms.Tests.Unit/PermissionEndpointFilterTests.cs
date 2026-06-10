using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using WeCms.Modules.System;
using WeCms.Shared;
using WeCms.Shared.Contracts;
using Xunit;

namespace WeCms.Tests.Unit;

public class PermissionEndpointFilterTests
{
    private static Endpoint CreateEndpoint(string? permissionCode = null)
    {
        var builder = new RouteEndpointBuilder(_ => Task.CompletedTask, Microsoft.AspNetCore.Routing.Patterns.RoutePatternFactory.Parse("/"), 0);
        if (permissionCode is not null)
            builder.Metadata.Add(new PermissionMetadata(permissionCode));
        return builder.Build();
    }

    private static DefaultHttpContext CreateContext(bool isAuthenticated = true, bool isSuperAdmin = false, string sub = "1", string permissionVersion = "1")
    {
        var ctx = new DefaultHttpContext();
        if (isAuthenticated)
        {
            var claims = new List<Claim>
            {
                new("sub", sub),
                new("permission_version", permissionVersion),
                new("is_super_admin", isSuperAdmin ? "true" : "false")
            };
            ctx.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
        }
        return ctx;
    }

    [Fact]
    public async Task ShouldAllowRequest_WhenNoPermissionMetadata()
    {
        var dbMock = new Mock<IDbConnectionFactory>();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var filter = new PermissionEndpointFilter(dbMock.Object, cache);
        var endpoint = CreateEndpoint(null);
        var httpContext = CreateContext();
        httpContext.SetEndpoint(endpoint);

        var ctx = new DefaultEndpointFilterInvocationContext(httpContext);
        var called = false;
        var result = await filter.InvokeAsync(ctx, (c) => { called = true; return ValueTask.FromResult<object?>(null); });
        Assert.Null(result);
        Assert.True(called);
    }

    [Fact]
    public async Task ShouldReturn401_WhenUserNotAuthenticated()
    {
        var dbMock = new Mock<IDbConnectionFactory>();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var filter = new PermissionEndpointFilter(dbMock.Object, cache);
        var endpoint = CreateEndpoint("sys:user:list");
        var httpContext = CreateContext(isAuthenticated: false);
        httpContext.SetEndpoint(endpoint);

        var ctx = new DefaultEndpointFilterInvocationContext(httpContext);
        var result = await filter.InvokeAsync(ctx, (c) => ValueTask.FromResult<object?>(null));
        Assert.NotNull(result);
        var httpResult = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResult<string>>>(result);
        Assert.NotNull(httpResult.Value);
        Assert.Equal(ApiCodes.Unauthorized, httpResult.Value.Code);
    }

    [Fact]
    public async Task ShouldAllowRequest_WhenSuperAdmin()
    {
        var dbMock = new Mock<IDbConnectionFactory>();
        dbMock.Setup(d => d.OpenAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("DB should not be called for super admin"));
        var cache = new MemoryCache(new MemoryCacheOptions());
        var filter = new PermissionEndpointFilter(dbMock.Object, cache);
        var endpoint = CreateEndpoint("sys:user:list");
        var httpContext = CreateContext(isSuperAdmin: true);
        httpContext.SetEndpoint(endpoint);

        var ctx = new DefaultEndpointFilterInvocationContext(httpContext);
        var called = false;
        var result = await filter.InvokeAsync(ctx, (c) => { called = true; return ValueTask.FromResult<object?>(null); });
        Assert.Null(result);
        Assert.True(called);
    }

    [Fact]
    public async Task ShouldReturn401_WhenInvalidTokenSub()
    {
        var dbMock = new Mock<IDbConnectionFactory>();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var filter = new PermissionEndpointFilter(dbMock.Object, cache);
        var endpoint = CreateEndpoint("sys:user:list");
        var httpContext = CreateContext(sub: "not-a-number");
        httpContext.SetEndpoint(endpoint);

        var ctx = new DefaultEndpointFilterInvocationContext(httpContext);
        var result = await filter.InvokeAsync(ctx, (c) => ValueTask.FromResult<object?>(null));
        Assert.NotNull(result);
        var httpResult = Assert.IsType<Microsoft.AspNetCore.Http.HttpResults.Ok<ApiResult<string>>>(result);
        Assert.NotNull(httpResult.Value);
        Assert.Equal(ApiCodes.Unauthorized, httpResult.Value.Code);
    }
}
