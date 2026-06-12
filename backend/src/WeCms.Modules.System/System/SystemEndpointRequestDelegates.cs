using Microsoft.AspNetCore.Http;
using System.Text.Json.Serialization.Metadata;
using WeCms.Shared;

namespace WeCms.Modules.System.System;

internal static class SystemEndpointRequestDelegates
{
    public static async Task HandleHealthLiveAsync(HttpContext context, SystemEndpointHandlers handlers)
    {
        await WriteOkAsync(context, handlers.GetHealthLive(), WeCmsModulesSystemJsonContext.Default.ApiResultHealthLiveResponse);
    }

    public static async Task HandleHealthReadyAsync(HttpContext context, SystemEndpointHandlers handlers)
    {
        var result = await handlers.GetHealthReadyAsync(context.RequestAborted);
        var statusCode = result.Data?.DatabaseReady == true
            ? StatusCodes.Status200OK
            : StatusCodes.Status503ServiceUnavailable;
        await WriteAsync(context, statusCode, result, WeCmsModulesSystemJsonContext.Default.ApiResultHealthReadyResponse);
    }

    public static async Task HandlePingAsync(HttpContext context, SystemEndpointHandlers handlers)
    {
        await WriteOkAsync(context, handlers.GetPing(), WeCmsModulesSystemJsonContext.Default.ApiResultSystemPingResponse);
    }

    public static async Task HandleVersionAsync(HttpContext context, SystemEndpointHandlers handlers)
    {
        await WriteOkAsync(context, handlers.GetVersion(), WeCmsModulesSystemJsonContext.Default.ApiResultSystemVersionResponse);
    }

    public static async Task HandleDbCheckAsync(HttpContext context, SystemEndpointHandlers handlers)
    {
        var result = await handlers.GetDbCheckAsync(context, context.RequestAborted);
        var statusCode = result.Code == ApiCodes.SystemError
            ? StatusCodes.Status503ServiceUnavailable
            : StatusCodes.Status200OK;
        await WriteAsync(context, statusCode, result, WeCmsModulesSystemJsonContext.Default.ApiResultDbCheckResponse);
    }

    public static async Task HandleSecurePingAsync(HttpContext context, SystemEndpointHandlers handlers)
    {
        await WriteOkAsync(context, handlers.GetSecurePing(), WeCmsModulesSystemJsonContext.Default.ApiResultSecurePingResponse);
    }

    private static async Task WriteOkAsync<T>(
        HttpContext context,
        ApiResult<T> result,
        JsonTypeInfo<ApiResult<T>> jsonTypeInfo)
        => await WriteAsync(context, StatusCodes.Status200OK, result, jsonTypeInfo);

    private static async Task WriteAsync<T>(
        HttpContext context,
        int statusCode,
        ApiResult<T> result,
        JsonTypeInfo<ApiResult<T>> jsonTypeInfo)
    {
        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(result, jsonTypeInfo, cancellationToken: context.RequestAborted);
    }
}
