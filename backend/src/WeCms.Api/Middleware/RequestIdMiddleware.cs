using WeCms.Shared.Id;

namespace WeCms.Api.Middleware;

public sealed class RequestIdMiddleware
{
    public const string HeaderName = "X-Trace-Id";
    private const int MaxTraceIdLength = 64;

    private readonly RequestDelegate _next;
    private readonly IIdGenerator _idGenerator;

    public RequestIdMiddleware(RequestDelegate next, IIdGenerator idGenerator)
    {
        _next = next;
        _idGenerator = idGenerator;
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

    private string ResolveTraceId(HttpContext context)
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

        return _idGenerator.NewId();
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
