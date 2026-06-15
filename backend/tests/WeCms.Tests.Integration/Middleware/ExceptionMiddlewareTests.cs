using System.Text.Json;
using Microsoft.AspNetCore.Http;
using WeCms.Api.Middleware;
using WeCms.Shared;

namespace WeCms.Tests.Integration.Middleware;

public sealed class ExceptionMiddlewareTests
{
    public static TheoryData<int, int> DomainStatuses => new()
    {
        { ApiCodes.ValidationError, StatusCodes.Status400BadRequest },
        { ApiCodes.Unauthorized, StatusCodes.Status401Unauthorized },
        { ApiCodes.Forbidden, StatusCodes.Status403Forbidden },
        { ApiCodes.NotFound, StatusCodes.Status404NotFound },
        { ApiCodes.Conflict, StatusCodes.Status409Conflict },
        { ApiCodes.TooManyRequests, StatusCodes.Status429TooManyRequests }
    };

    [Theory]
    [MemberData(nameof(DomainStatuses))]
    public async Task InvokeAsync_ShouldReturnUnifiedErrorContract_WhenDomainExceptionIsThrown(
        int code,
        int statusCode)
    {
        var context = CreateContext("trace-domain");
        var middleware = new ExceptionMiddleware(_ => throw new DomainException(code, "domain error", statusCode));

        await middleware.InvokeAsync(context);

        Assert.Equal(statusCode, context.Response.StatusCode);
        using var json = await ReadJsonAsync(context);
        Assert.Equal(code, json.RootElement.GetProperty("code").GetInt32());
        Assert.Equal("domain error", json.RootElement.GetProperty("msg").GetString());
        Assert.True(json.RootElement.GetProperty("data").ValueKind is JsonValueKind.Null);
        Assert.Equal("trace-domain", json.RootElement.GetProperty("traceId").GetString());
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturnSystemErrorWithoutStack_WhenUnhandledExceptionIsThrown()
    {
        var context = CreateContext("trace-500");
        var middleware = new ExceptionMiddleware(_ => throw new InvalidOperationException("sensitive stack detail"));

        await middleware.InvokeAsync(context);

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        using var json = await ReadJsonAsync(context);
        Assert.Equal(ApiCodes.SystemError, json.RootElement.GetProperty("code").GetInt32());
        Assert.Equal("服务器内部错误", json.RootElement.GetProperty("msg").GetString());
        Assert.Equal("trace-500", json.RootElement.GetProperty("traceId").GetString());
        Assert.DoesNotContain("sensitive stack detail", await ReadBodyAsync(context));
    }

    private static DefaultHttpContext CreateContext(string traceId)
    {
        var context = new DefaultHttpContext
        {
            TraceIdentifier = traceId
        };
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpContext context)
    {
        var body = await ReadBodyAsync(context);
        return JsonDocument.Parse(body);
    }

    private static async Task<string> ReadBodyAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body, leaveOpen: true);
        return await reader.ReadToEndAsync();
    }
}
