using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json.Serialization.Metadata;
using WeCms.Shared;

namespace WeCms.Modules.System.System;

internal static class SystemEndpointRequestDelegates
{
    public static async Task HandleHealthLiveAsync(HttpContext context)
    {
        var handlers = context.RequestServices.GetRequiredService<SystemEndpointHandlers>();
        await WriteOkAsync(context, handlers.GetHealthLive(), WeCmsModulesSystemJsonContext.Default.ApiResultHealthLiveResponse);
    }

    public static async Task HandleHealthReadyAsync(HttpContext context)
    {
        var handlers = context.RequestServices.GetRequiredService<SystemEndpointHandlers>();
        var result = await handlers.GetHealthReadyAsync(context.RequestAborted);
        await WriteOkAsync(context, result, WeCmsModulesSystemJsonContext.Default.ApiResultHealthReadyResponse);
    }

    public static async Task HandlePingAsync(HttpContext context)
    {
        var handlers = context.RequestServices.GetRequiredService<SystemEndpointHandlers>();
        await WriteOkAsync(context, handlers.GetPing(), WeCmsModulesSystemJsonContext.Default.ApiResultSystemPingResponse);
    }

    public static async Task HandleVersionAsync(HttpContext context)
    {
        var handlers = context.RequestServices.GetRequiredService<SystemEndpointHandlers>();
        await WriteOkAsync(context, handlers.GetVersion(), WeCmsModulesSystemJsonContext.Default.ApiResultSystemVersionResponse);
    }

    public static async Task HandleDbCheckAsync(HttpContext context)
    {
        var handlers = context.RequestServices.GetRequiredService<SystemEndpointHandlers>();
        var result = await handlers.GetDbCheckAsync(context, context.RequestAborted);
        await result.ExecuteAsync(context);
    }

    public static async Task HandleSecurePingAsync(HttpContext context)
    {
        var handlers = context.RequestServices.GetRequiredService<SystemEndpointHandlers>();
        await WriteOkAsync(context, handlers.GetSecurePing(), WeCmsModulesSystemJsonContext.Default.ApiResultSecurePingResponse);
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
