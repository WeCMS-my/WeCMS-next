using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using WeCms.Api.Middleware;
using WeCms.Modules.System.Auth;
using WeCms.Shared;

namespace WeCms.Tests.Integration.Middleware;

public sealed class ExceptionMiddlewareTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ExceptionMiddlewareTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Database:AutoMigrate"] = "false",
                    ["ConnectionStrings:Default"] = "Server=127.0.0.1;Port=1;Database=wecms_dev;User=wecms;Password=wecms-dev-123;Connection Timeout=1;Default Command Timeout=1;"
                });
            });
        }).CreateClient();
    }

    [Fact]
    public async Task Ping_ShouldReturnSuccess_WhenNoError()
    {
        var response = await _client.GetAsync("/api/v1/system/ping");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("X-Trace-Id", response.Headers.Select(h => h.Key));
    }

    [Fact]
    public async Task DomainException_ShouldReturnBusinessError_WithBadRequest()
    {
        var context = await InvokeExceptionMiddlewareAsync(_ =>
            throw new DomainException(ApiCodes.BusinessError, "测试业务异常"));

        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);

        var body = await ReadResponseBodyAsync(context);
        Assert.Equal(2001, body.GetProperty("code").GetInt32());
        Assert.Equal("测试业务异常", body.GetProperty("msg").GetString());
        Assert.Equal(JsonValueKind.Null, body.GetProperty("data").ValueKind);
        Assert.NotNull(body.GetProperty("traceId").GetString());
    }

    [Fact]
    public async Task UnhandledException_ShouldReturn500_WithGenericMessage()
    {
        var context = await InvokeExceptionMiddlewareAsync(_ =>
            throw new InvalidOperationException("测试未处理异常"));

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);

        var body = await ReadResponseBodyAsync(context);
        Assert.Equal(5000, body.GetProperty("code").GetInt32());
        Assert.Equal("系统内部错误", body.GetProperty("msg").GetString());
        // Must NOT contain original exception message or stack trace
        var rawJson = body.GetRawText();
        Assert.DoesNotContain("测试未处理异常", rawJson);
        Assert.DoesNotContain("InvalidOperationException", rawJson);
        Assert.NotNull(body.GetProperty("traceId").GetString());
    }

    [Fact]
    public async Task AllResponses_ShouldContain_TraceIdHeader()
    {
        using var pingResponse = await _client.GetAsync("/api/v1/system/ping");
        using var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(string.Empty, string.Empty));
        using var refreshResponse = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshRequest(string.Empty));

        Assert.Contains("X-Trace-Id", pingResponse.Headers.Select(h => h.Key));
        Assert.Contains("X-Trace-Id", loginResponse.Headers.Select(h => h.Key));
        Assert.Contains("X-Trace-Id", refreshResponse.Headers.Select(h => h.Key));
    }

    [Fact]
    public async Task ErrorResponse_ShouldNotContain_StackTrace()
    {
        var context = await InvokeExceptionMiddlewareAsync(_ =>
            throw new InvalidOperationException("测试未处理异常"));

        var rawJson = await ReadRawResponseBodyAsync(context);

        Assert.DoesNotContain("   at ", rawJson);
        Assert.DoesNotContain("stack", rawJson.ToLowerInvariant());
    }

    [Fact]
    public async Task Login_ShouldReturn400_WhenCredentialsMissing()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", new LoginRequest(string.Empty, string.Empty));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(ApiCodes.ValidationError, body.GetProperty("code").GetInt32());
    }

    [Fact]
    public async Task Refresh_ShouldReturn400_WhenTokenMissing()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/refresh", new RefreshRequest(string.Empty));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(ApiCodes.ValidationError, body.GetProperty("code").GetInt32());
    }

    [Fact]
    public async Task Me_ShouldReturn401_WhenUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static async Task<DefaultHttpContext> InvokeExceptionMiddlewareAsync(RequestDelegate next)
    {
        var context = new DefaultHttpContext
        {
            TraceIdentifier = "trace-test",
        };
        context.Response.Body = new MemoryStream();

        var middleware = new ExceptionMiddleware(next);
        await middleware.InvokeAsync(context);

        return context;
    }

    private static async Task<JsonElement> ReadResponseBodyAsync(HttpContext context)
    {
        var rawJson = await ReadRawResponseBodyAsync(context);
        using var document = JsonDocument.Parse(rawJson);
        return document.RootElement.Clone();
    }

    private static async Task<string> ReadRawResponseBodyAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body, leaveOpen: true);
        return await reader.ReadToEndAsync();
    }
}
