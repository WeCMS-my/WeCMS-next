namespace WeCms.Api.Middleware;

public sealed class RequestIdMiddleware(RequestDelegate next)
{
    public const string HeaderName = "X-Request-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(HeaderName, out var requestIds)
            && requestIds.Count == 1
            && !string.IsNullOrWhiteSpace(requestIds[0]))
        {
            context.TraceIdentifier = requestIds[0]!;
        }

        context.Response.Headers[HeaderName] = context.TraceIdentifier;

        await next(context);
    }
}
