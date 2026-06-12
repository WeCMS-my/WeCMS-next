using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using WeCms.Api.Json;
using WeCms.Modules.System.Auth;
using WeCms.Shared;
using WeCms.Shared.Security;

namespace WeCms.Api.Extensions;

internal static class AuthEndpointRequestDelegates
{
    public static async Task HandleLoginAsync(HttpContext context)
    {
        var request = await ReadRequiredRequestAsync(
            context,
            WeCmsJsonContext.Default.LoginRequest,
            "用户名和密码不能为空");
        var handlers = context.RequestServices.GetRequiredService<AuthEndpointHandlers>();
        var result = await handlers.LoginAsync(
            request,
            context.GetClientIp(),
            context.Request.Headers.UserAgent.ToString(),
            context.RequestAborted);

        await WriteOkAsync(context, result, WeCmsJsonContext.Default.ApiResultLoginResponse);
    }

    public static async Task HandleRefreshAsync(HttpContext context)
    {
        var request = await ReadRequiredRequestAsync(
            context,
            WeCmsJsonContext.Default.RefreshRequest,
            "刷新令牌不能为空");
        var handlers = context.RequestServices.GetRequiredService<AuthEndpointHandlers>();
        var result = await handlers.RefreshAsync(
            request,
            context.GetClientIp(),
            context.Request.Headers.UserAgent.ToString(),
            context.RequestAborted);

        await WriteOkAsync(context, result, WeCmsJsonContext.Default.ApiResultRefreshResponse);
    }

    public static async Task HandleLogoutAsync(HttpContext context)
    {
        var request = await ReadRequiredRequestAsync(
            context,
            WeCmsJsonContext.Default.LogoutRequest,
            "刷新令牌不能为空");
        var handlers = context.RequestServices.GetRequiredService<AuthEndpointHandlers>();
        var result = await handlers.LogoutAsync(request, context.RequestAborted);

        await WriteOkAsync(context, result, WeCmsJsonContext.Default.ApiResultObject);
    }

    public static async Task HandleCurrentUserAsync(HttpContext context)
    {
        var handlers = context.RequestServices.GetRequiredService<AuthEndpointHandlers>();
        var result = await handlers.GetCurrentUserAsync(context.User, context.RequestAborted);

        await WriteOkAsync(context, result, WeCmsJsonContext.Default.ApiResultCurrentUserResponse);
    }

    private static async Task<T> ReadRequiredRequestAsync<T>(
        HttpContext context,
        JsonTypeInfo<T> jsonTypeInfo,
        string emptyMessage)
    {
        try
        {
            var request = await context.Request.ReadFromJsonAsync(jsonTypeInfo, context.RequestAborted);
            if (request is null)
            {
                throw new DomainException(ApiCodes.ValidationError, emptyMessage);
            }

            return request;
        }
        catch (JsonException)
        {
            throw new DomainException(ApiCodes.ValidationError, "请求体格式无效");
        }
    }

    private static async Task WriteOkAsync<T>(
        HttpContext context,
        ApiResult<T> result,
        JsonTypeInfo<ApiResult<T>> jsonTypeInfo)
    {
        context.Response.StatusCode = StatusCodes.Status200OK;
        await context.Response.WriteAsJsonAsync(result, jsonTypeInfo, cancellationToken: context.RequestAborted);
    }
}
