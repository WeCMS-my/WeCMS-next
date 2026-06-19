using System.Text.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
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
        await context.Response.StartAsync(TestContext.Current.CancellationToken);

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
    public async Task RequestLoggingMiddleware_LogsRequestMetadataWithoutSensitiveHeadersOrBody()
    {
        var context = new DefaultHttpContext();
        context.TraceIdentifier = "trace-log";
        context.Request.Method = "POST";
        context.Request.Path = "/api/v1/auth/login";
        context.Request.Headers.Authorization = "Bearer secret-token";
        context.Request.Headers.Cookie = "__Host-wecms_refresh=secret-cookie";
        context.Request.Body = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("""{"password":"secret-password"}"""));
        var logger = new CapturingLogger<RequestLoggingMiddleware>();
        var middleware = new RequestLoggingMiddleware(
            next =>
            {
                next.User = new ClaimsPrincipal(new ClaimsIdentity(
                    [
                        new Claim(ClaimTypes.NameIdentifier, "42"),
                        new Claim(ClaimTypes.Name, "admin")
                    ],
                    authenticationType: "unit"));
                next.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            },
            logger);

        await middleware.InvokeAsync(context);

        var entry = Assert.Single(logger.Messages);
        Assert.Contains("trace-log", entry, StringComparison.Ordinal);
        Assert.Contains("42", entry, StringComparison.Ordinal);
        Assert.Contains("admin", entry, StringComparison.Ordinal);
        Assert.Contains("/api/v1/auth/login", entry, StringComparison.Ordinal);
        Assert.Contains("401", entry, StringComparison.Ordinal);
        Assert.DoesNotContain("Authorization", entry, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Cookie", entry, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret-token", entry, StringComparison.Ordinal);
        Assert.DoesNotContain("secret-cookie", entry, StringComparison.Ordinal);
        Assert.DoesNotContain("secret-password", entry, StringComparison.Ordinal);
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

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];

        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
        {
            return null;
        }

        public bool IsEnabled(LogLevel logLevel)
        {
            return true;
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            Messages.Add(formatter(state, exception));
        }
    }
}
