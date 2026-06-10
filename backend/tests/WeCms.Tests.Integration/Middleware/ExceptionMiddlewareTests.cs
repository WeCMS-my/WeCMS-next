using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using WeCms.Shared;

namespace WeCms.Tests.Integration.Middleware;

public sealed class ExceptionMiddlewareTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ExceptionMiddlewareTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Ping_ShouldReturnSuccess_WhenNoError()
    {
        var response = await _client.GetAsync("/api/v1/system/ping");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("X-Trace-Id", response.Headers.Select(h => h.Key));
    }

    [Fact]
    public async Task DomainException_ShouldReturnApiResult_WithBusinessCode()
    {
        var response = await _client.GetAsync("/test/throw-domain-exception");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2001, body.GetProperty("code").GetInt32());
        Assert.Equal("测试业务异常", body.GetProperty("msg").GetString());
        Assert.Equal(JsonValueKind.Null, body.GetProperty("data").ValueKind);
        Assert.NotNull(body.GetProperty("traceId").GetString());
    }

    [Fact]
    public async Task UnhandledException_ShouldReturn500_WithGenericMessage()
    {
        var response = await _client.GetAsync("/test/throw-exception");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
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
        using var errorResponse = await _client.GetAsync("/test/throw-domain-exception");
        using var serverErrorResponse = await _client.GetAsync("/test/throw-exception");

        Assert.Contains("X-Trace-Id", pingResponse.Headers.Select(h => h.Key));
        Assert.Contains("X-Trace-Id", errorResponse.Headers.Select(h => h.Key));
        Assert.Contains("X-Trace-Id", serverErrorResponse.Headers.Select(h => h.Key));
    }

    [Fact]
    public async Task ErrorResponse_ShouldNotContain_StackTrace()
    {
        var response = await _client.GetAsync("/test/throw-exception");
        var rawJson = await response.Content.ReadAsStringAsync();

        Assert.DoesNotContain("   at ", rawJson);
        Assert.DoesNotContain("stack", rawJson.ToLowerInvariant());
    }
}
