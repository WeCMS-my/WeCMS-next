using System.Net;
using System.Text.Json;
using WeCms.Api.Json;
using WeCms.Shared;

namespace WeCms.Api.Middleware;

public sealed class ExceptionMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (UnauthorizedAccessException)
        {
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            context.Response.ContentType = "application/json";
            var result = ApiResult<string>.Fail(ApiCodes.Unauthorized, "Unauthorized");
            await JsonSerializer.SerializeAsync(context.Response.Body, result, WeCmsJsonContext.Default.ApiResultString);
        }
        catch (Exception)
        {
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";
            var result = ApiResult<string>.Fail(ApiCodes.SystemError, "Internal server error");
            await JsonSerializer.SerializeAsync(context.Response.Body, result, WeCmsJsonContext.Default.ApiResultString);
        }
    }
}