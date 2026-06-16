using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using WeCms.Api.Middleware;
using WeCms.Shared;

namespace WeCms.Tests.Unit.Api;

public sealed class ResponseAndExceptionTests
{
    [Fact]
    public async Task RequestIdMiddleware_WritesTraceIdHeader()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Request.Headers["X-Trace-Id"] = "trace-from-client";
        var middleware = new RequestIdMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);
        await context.Response.StartAsync();

        Assert.Equal("trace-from-client", context.TraceIdentifier);
        Assert.Equal("trace-from-client", context.Response.Headers["X-Trace-Id"]);
    }

    [Fact]
    public async Task RequestIdMiddleware_RejectsInvalidIncomingTraceId()
    {
        var context = new DefaultHttpContext();
        context.TraceIdentifier = string.Empty;
        context.Response.Body = new MemoryStream();
        context.Request.Headers["X-Trace-Id"] = new string('x', 65);
        var middleware = new RequestIdMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        Assert.NotEqual(new string('x', 65), context.TraceIdentifier);
        Assert.Matches("^[A-Za-z0-9._-]{1,64}$", context.TraceIdentifier);
        Assert.Equal(context.TraceIdentifier, context.Response.Headers["X-Trace-Id"]);
    }

    [Fact]
    public async Task ExceptionMiddleware_MapsDomainExceptionToStatusAndTraceId()
    {
        var context = new DefaultHttpContext();
        context.TraceIdentifier = "trace-domain";
        context.Response.Body = new MemoryStream();
        var middleware = new ExceptionMiddleware(
            _ => throw new DomainException(ApiCodes.Conflict, "duplicate"),
            NullLogger<ExceptionMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        var json = ReadJson(context);
        Assert.Equal(StatusCodes.Status409Conflict, context.Response.StatusCode);
        Assert.Equal(ApiCodes.Conflict, json.RootElement.GetProperty("code").GetInt32());
        Assert.Equal("duplicate", json.RootElement.GetProperty("msg").GetString());
        Assert.Equal("trace-domain", json.RootElement.GetProperty("traceId").GetString());
    }

    [Fact]
    public async Task ExceptionMiddleware_HidesUnhandledExceptionMessage()
    {
        var context = new DefaultHttpContext();
        context.TraceIdentifier = "trace-system";
        context.Response.Body = new MemoryStream();
        var middleware = new ExceptionMiddleware(
            _ => throw new InvalidOperationException("database secret"),
            NullLogger<ExceptionMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        var json = ReadJson(context);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Equal(ApiCodes.SystemError, json.RootElement.GetProperty("code").GetInt32());
        Assert.DoesNotContain("database secret", json.RootElement.GetProperty("msg").GetString(), StringComparison.Ordinal);
        Assert.Equal("trace-system", json.RootElement.GetProperty("traceId").GetString());
    }

    [Theory]
    [InlineData(ApiCodes.ValidationError, StatusCodes.Status400BadRequest)]
    [InlineData(ApiCodes.Unauthorized, StatusCodes.Status401Unauthorized)]
    [InlineData(ApiCodes.Forbidden, StatusCodes.Status403Forbidden)]
    [InlineData(ApiCodes.NotFound, StatusCodes.Status404NotFound)]
    [InlineData(ApiCodes.Conflict, StatusCodes.Status409Conflict)]
    [InlineData(ApiCodes.TooManyRequests, StatusCodes.Status429TooManyRequests)]
    [InlineData(ApiCodes.BusinessError, StatusCodes.Status400BadRequest)]
    [InlineData(ApiCodes.SystemError, StatusCodes.Status500InternalServerError)]
    public void ApiCodes_MapsToHttpStatus(int code, int expectedStatus)
    {
        Assert.Equal(expectedStatus, ApiCodes.ToHttpStatus(code));
    }

    [Fact]
    public void DomainException_RejectsUnknownApiCode()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new DomainException(49999, "unknown"));
    }

    private static JsonDocument ReadJson(HttpContext context)
    {
        context.Response.Body.Position = 0;

        return JsonDocument.Parse(context.Response.Body);
    }
}
