using System.Net;
using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
using WeCms.Shared;
using WeCms.Api.Json;

namespace WeCms.Api.Middleware;

public sealed class ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try { await next(context); }
        catch (SecurityTokenException)
        { await WriteError(context, 401, ApiCodes.Unauthorized, "Authentication failed"); }
        catch (UnauthorizedAccessException)
        { await WriteError(context, 403, ApiCodes.Forbidden, "Access denied"); }
        catch (InvalidOperationException ex)
        { logger.LogError(ex, "Business error"); await WriteError(context, 400, ApiCodes.BusinessError, "Business error"); }
        catch (Exception ex)
        { logger.LogError(ex, "Unhandled exception"); context.Response.Headers["X-Trace-Id"] = context.TraceIdentifier; await WriteError(context, 500, ApiCodes.SystemError, "Internal server error"); }
    }

    private static async Task WriteError(HttpContext ctx, int statusCode, int apiCode, string msg)
    {
        ctx.Response.StatusCode = statusCode;
        ctx.Response.ContentType = "application/json";
        var result = ApiResult<string>.Fail(apiCode, msg);
        await JsonSerializer.SerializeAsync(ctx.Response.Body, result, WeCmsJsonContext.Default.ApiResultString);
    }
}
