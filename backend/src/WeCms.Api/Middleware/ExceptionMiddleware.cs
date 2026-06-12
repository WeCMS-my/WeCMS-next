using System.Text.Json;
using WeCms.Api.Json;
using WeCms.Shared;

namespace WeCms.Api.Middleware;

public sealed class ExceptionMiddleware
{
    private static readonly Action<ILogger, string, Exception?> LogUnhandledException =
        LoggerMessage.Define<string>(
            LogLevel.Error,
            new EventId(1, nameof(LogUnhandledException)),
            "Unhandled exception, traceId={TraceId}");

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
        catch (DomainException ex)
        {
            var statusCode = MapDomainExceptionStatusCode(ex.Code);
            await WriteErrorResponse(context, statusCode, ex.Code, ex.Message);
        }
        catch (Exception ex)
        {
            LogUnhandledException(_logger, context.TraceIdentifier, ex);
            await WriteErrorResponse(context, StatusCodes.Status500InternalServerError,
                ApiCodes.SystemError, "系统内部错误");
        }
    }

    private static int MapDomainExceptionStatusCode(int code) =>
        code switch
        {
            ApiCodes.ValidationError => StatusCodes.Status400BadRequest,
            ApiCodes.Unauthorized => StatusCodes.Status401Unauthorized,
            ApiCodes.Forbidden => StatusCodes.Status403Forbidden,
            ApiCodes.NotFound => StatusCodes.Status404NotFound,
            ApiCodes.Conflict => StatusCodes.Status409Conflict,
            ApiCodes.TooManyRequests => StatusCodes.Status429TooManyRequests,
            ApiCodes.BusinessError => StatusCodes.Status400BadRequest,
            ApiCodes.SystemError => StatusCodes.Status500InternalServerError,
            _ => StatusCodes.Status500InternalServerError
        };

    private static async Task WriteErrorResponse(HttpContext context, int statusCode, int code, string msg)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json; charset=utf-8";

        var result = ApiResult<object?>.Fail(code, msg, context.TraceIdentifier);
        var json = JsonSerializer.Serialize(result, WeCmsJsonContext.Default.ApiResultObject);
        await context.Response.WriteAsync(json);
    }
}
