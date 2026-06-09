 using System.Net;
 using System.Text.Json;
 using Microsoft.IdentityModel.Tokens;
using WeCms.Shared;
using WeCms.Api.Json;

namespace WeCms.Api.Middleware;
 
 public sealed class ExceptionMiddleware(RequestDelegate next)
 {
     public async Task InvokeAsync(HttpContext context)
     {
         try { await next(context); }
         catch (SecurityTokenException)
         { await WriteError(context, 401, ApiCodes.Unauthorized, "Authentication failed"); }
         catch (InvalidOperationException ex)
         { await WriteError(context, 400, ApiCodes.BusinessError, ex.Message); }
         catch (Exception)
         { await WriteError(context, 500, ApiCodes.SystemError, "Internal server error"); }
     }
 
     private static async Task WriteError(HttpContext ctx, int statusCode, int apiCode, string msg)
     {
         ctx.Response.StatusCode = statusCode;
         ctx.Response.ContentType = "application/json";
         var result = ApiResult<string>.Fail(apiCode, msg);
         await JsonSerializer.SerializeAsync(ctx.Response.Body, result, WeCmsJsonContext.Default.ApiResultString);
     }
 }
