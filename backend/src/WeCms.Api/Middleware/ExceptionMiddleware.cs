using System.Text.Json;
using WeCms.Api.Json;
using WeCms.Shared;

namespace WeCms.Api.Middleware;

public sealed class ExceptionMiddleware
{
    private readonly RequestDelegate _next;

    public ExceptionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (DomainException ex)
        {
            await WriteErrorResponse(context, StatusCodes.Status200OK, ex.Code, ex.Message);
        }
        catch (Exception)
        {
            await WriteErrorResponse(context, StatusCodes.Status500InternalServerError,
                ApiCodes.SystemError, "系统内部错误");
        }
    }

    private static async Task WriteErrorResponse(HttpContext context, int statusCode, int code, string msg)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json; charset=utf-8";

        var result = ApiResult<object?>.Fail(code, msg, context.TraceIdentifier);
        var json = JsonSerializer.Serialize(result, WeCmsJsonContext.Default.ApiResultObject);
        await context.Response.WriteAsync(json);
    }
}
