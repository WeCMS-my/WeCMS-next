namespace WeCms.Api.Middleware;

public sealed class RequestIdMiddleware
{
    public const string HeaderName = "X-Trace-Id";
    private const int MaxTraceIdLength = 64;

    private readonly RequestDelegate _next;

    public RequestIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var traceId = ResolveTraceId(context);
        context.TraceIdentifier = traceId;
        context.Response.Headers[HeaderName] = traceId;
        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = traceId;
            return Task.CompletedTask;
        });

        await _next(context);
    }

    private static string ResolveTraceId(HttpContext context)
    {
        var incomingTraceId = context.Request.Headers[HeaderName].ToString();
        if (IsValidTraceId(incomingTraceId))
        {
            return incomingTraceId;
        }

        if (IsValidTraceId(context.TraceIdentifier))
        {
            return context.TraceIdentifier;
        }

        return Guid.NewGuid().ToString("N");
    }

    private static bool IsValidTraceId(string? traceId)
    {
        return !string.IsNullOrWhiteSpace(traceId)
            && traceId.Length <= MaxTraceIdLength
            && traceId.All(IsTraceIdCharacter);
    }

    private static bool IsTraceIdCharacter(char value)
    {
        return char.IsAsciiLetterOrDigit(value)
            || value is '-' or '_' or '.';
    }
}
