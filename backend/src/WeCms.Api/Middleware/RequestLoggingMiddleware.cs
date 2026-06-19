using System.Diagnostics;
using System.Security.Claims;

namespace WeCms.Api.Middleware;

public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var started = Stopwatch.GetTimestamp();

        using (_logger.BeginScope(new Dictionary<string, object?>
        {
            ["traceId"] = context.TraceIdentifier,
            ["requestId"] = context.TraceIdentifier,
            ["path"] = context.Request.Path.Value ?? string.Empty,
            ["method"] = context.Request.Method
        }))
        {
            try
            {
                await _next(context);
            }
            finally
            {
                var elapsedMs = Stopwatch.GetElapsedTime(started).TotalMilliseconds;
                var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
                var username = context.User.Identity?.Name
                    ?? context.User.FindFirstValue(ClaimTypes.Name);

                _logger.LogInformation(
                    "HTTP request completed. TraceId: {TraceId}; UserId: {UserId}; Username: {Username}; Method: {Method}; Path: {Path}; StatusCode: {StatusCode}; ElapsedMs: {ElapsedMs}; EventType: {EventType}",
                    context.TraceIdentifier,
                    userId,
                    username,
                    context.Request.Method,
                    context.Request.Path.Value ?? string.Empty,
                    context.Response.StatusCode,
                    Math.Round(elapsedMs, 3),
                    "http_request_completed");
            }
        }
    }
}
