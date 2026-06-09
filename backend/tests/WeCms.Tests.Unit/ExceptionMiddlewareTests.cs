 using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using WeCms.Api.Middleware;
using Xunit;

namespace WeCms.Tests.Unit;

public class ExceptionMiddlewareTests
{
    [Fact]
    public async Task NoException_ShouldCallNextDelegate()
    {
        var context = new DefaultHttpContext();
        var called = false;
        RequestDelegate next = (ctx) => { called = true; return Task.CompletedTask; };
        var middleware = new ExceptionMiddleware(next, NullLogger<ExceptionMiddleware>.Instance);
        await middleware.InvokeAsync(context);
        Assert.True(called);
        Assert.Equal(200, context.Response.StatusCode);
    }

    [Fact]
    public async Task UnauthorizedAccessException_ShouldReturn403()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        RequestDelegate next = (ctx) => throw new UnauthorizedAccessException("forbidden");
        var middleware = new ExceptionMiddleware(next, NullLogger<ExceptionMiddleware>.Instance);
        await middleware.InvokeAsync(context);
        Assert.Equal(403, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvalidOperationException_ShouldReturn400()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        RequestDelegate next = (ctx) => throw new InvalidOperationException("business error");
        var middleware = new ExceptionMiddleware(next, NullLogger<ExceptionMiddleware>.Instance);
        await middleware.InvokeAsync(context);
        Assert.Equal(400, context.Response.StatusCode);
    }

    [Fact]
    public async Task UnhandledException_ShouldReturn500()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        RequestDelegate next = (ctx) => throw new ArgumentException("unexpected");
        var middleware = new ExceptionMiddleware(next, NullLogger<ExceptionMiddleware>.Instance);
        await middleware.InvokeAsync(context);
        Assert.Equal(500, context.Response.StatusCode);
    }
}
