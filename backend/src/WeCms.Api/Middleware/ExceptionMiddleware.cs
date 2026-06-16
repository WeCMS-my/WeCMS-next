using System.Text.Json;
using WeCms.Api.Json;
using WeCms.Shared;

namespace WeCms.Api.Middleware;

public sealed class ExceptionMiddleware
{
    private const string SystemErrorMessage = "An unexpected server error occurred.";

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (DomainException exception)
        {
            await WriteErrorAsync(
                context,
                exception.Code,
                exception.Message,
                exception.FieldErrors,
                context.RequestAborted);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled request exception. TraceId: {TraceId}", context.TraceIdentifier);
            await WriteErrorAsync(
                context,
                ApiCodes.SystemError,
                SystemErrorMessage,
                fieldErrors: null,
                context.RequestAborted);
        }
    }

    private static async Task WriteErrorAsync(
        HttpContext context,
        int code,
        string message,
        IReadOnlyDictionary<string, string[]>? fieldErrors,
        CancellationToken cancellationToken)
    {
        if (context.Response.HasStarted)
        {
            throw new InvalidOperationException("Cannot write API error response after the response has started.");
        }

        context.Response.Clear();
        context.Response.StatusCode = ApiCodes.ToHttpStatus(code);
        context.Response.ContentType = "application/json; charset=utf-8";

        var result = ApiResult<object>.Error(code, message, context.TraceIdentifier, fieldErrors);
        await JsonSerializer.SerializeAsync(
            context.Response.Body,
            result,
            WeCmsJsonSerializerContext.Default.ApiResultObject,
            cancellationToken);
    }
}
