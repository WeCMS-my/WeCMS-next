using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using WeCms.Modules.System.Permissions;
using WeCms.Shared;
using WeCms.Shared.Data;
using WeCms.Shared.Security;
using WeCms.Shared.Time;

namespace WeCms.Modules.System.System;

public static class SystemEndpoints
{
    public static void Map(WebApplication app)
    {
        var healthGroup = app.MapGroup("/health");
        healthGroup.MapGet("/live", (RequestDelegate)HealthLiveRequestHandler);
        healthGroup.MapGet("/ready", (RequestDelegate)HealthReadyRequestHandler);

        var systemGroup = app.MapGroup("/api/v1/system");
        systemGroup.MapGet("/ping", (RequestDelegate)PingRequestHandler);
        systemGroup.MapGet("/version", (RequestDelegate)VersionRequestHandler);
        systemGroup.MapGet("/db-check", (RequestDelegate)DbCheckRequestHandler);

        // M0-BE-009: secure-ping with RequirePermission
        ((RouteHandlerBuilder)systemGroup.MapGet("/secure-ping", (RequestDelegate)SecurePingRequestHandler))
            .RequirePermission(SystemPermissions.SystemSecurePing);
    }

    private static Task HealthLiveRequestHandler(HttpContext context)
        => HealthLiveAsync(context, context.RequestServices.GetRequiredService<IClock>());

    private static Task HealthReadyRequestHandler(HttpContext context)
    {
        var clock = context.RequestServices.GetRequiredService<IClock>();
        var db = context.RequestServices.GetRequiredService<IDbConnectionFactory>();
        return HealthReadyAsync(context, db, clock);
    }

    private static Task PingRequestHandler(HttpContext context)
        => PingAsync(context, context.RequestServices.GetRequiredService<IClock>());

    private static Task VersionRequestHandler(HttpContext context)
        => VersionAsync(context);

    private static Task DbCheckRequestHandler(HttpContext context)
        => DbCheckAsync(context, context.RequestServices.GetRequiredService<IDbConnectionFactory>());

    private static Task SecurePingRequestHandler(HttpContext context)
        => SecurePingAsync(context, context.RequestServices.GetRequiredService<IClock>());

    private static async Task HealthLiveAsync(HttpContext context, IClock clock)
    {
        var cancellationToken = context.RequestAborted;
        await WriteJsonResponse(
            context,
            ApiResult<HealthLiveResponse>.Ok(new HealthLiveResponse("healthy", clock.UtcNow)),
            typeof(ApiResult<HealthLiveResponse>),
            cancellationToken);
    }

    private static async Task HealthReadyAsync(HttpContext context, IDbConnectionFactory db, IClock clock)
    {
        var cancellationToken = context.RequestAborted;
        var sw = Stopwatch.StartNew();
        try
        {
            await using var connection = await db.OpenAsync(cancellationToken);
            sw.Stop();
            await WriteJsonResponse(
                context,
                ApiResult<HealthReadyResponse>.Ok(new HealthReadyResponse("ready", true, sw.ElapsedMilliseconds)),
                typeof(ApiResult<HealthReadyResponse>),
                cancellationToken);
        }
        catch (Exception)
        {
            sw.Stop();
            await WriteJsonResponse(
                context,
                ApiResult<HealthReadyResponse>.Ok(new HealthReadyResponse("not ready", false, null)),
                typeof(ApiResult<HealthReadyResponse>),
                cancellationToken);
        }
    }

    private static async Task PingAsync(HttpContext context, IClock clock)
    {
        var cancellationToken = context.RequestAborted;
        await WriteJsonResponse(
            context,
            ApiResult<SystemPingResponse>.Ok(new SystemPingResponse("pong", TimeZoneInfo.Local.Id, clock.UtcNow)),
            typeof(ApiResult<SystemPingResponse>),
            cancellationToken);
    }

    private static async Task VersionAsync(HttpContext context)
    {
        var cancellationToken = context.RequestAborted;
        await WriteJsonResponse(
            context,
            ApiResult<SystemVersionResponse>.Ok(new SystemVersionResponse("0.1.0", "M0-BE", "net10.0")),
            typeof(ApiResult<SystemVersionResponse>),
            cancellationToken);
    }

    private static async Task DbCheckAsync(HttpContext context, IDbConnectionFactory db)
    {
        var cancellationToken = context.RequestAborted;
        try
        {
            await using var connection = await db.OpenAsync(cancellationToken);
            await WriteJsonResponse(
                context,
                ApiResult<DbCheckResponse>.Ok(new DbCheckResponse("connected", connection.Database, null)),
                typeof(ApiResult<DbCheckResponse>),
                cancellationToken);
        }
        catch (Exception ex)
        {
            await WriteJsonResponse(
                context,
                ApiResult<DbCheckResponse>.Ok(new DbCheckResponse("failed", "unknown", ex.Message)),
                typeof(ApiResult<DbCheckResponse>),
                cancellationToken);
        }
    }

    private static async Task SecurePingAsync(HttpContext context, IClock clock)
    {
        var cancellationToken = context.RequestAborted;
        await WriteJsonResponse(
            context,
            ApiResult<object?>.Ok(new { status = "secure-pong", timestamp = clock.UtcNow }),
            typeof(ApiResult<object?>),
            cancellationToken);
    }

    private static async Task WriteJsonResponse(
        HttpContext context,
        object result,
        Type resultType,
        CancellationToken cancellationToken)
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        await context.Response.WriteAsJsonAsync(result, resultType, WeCmsModulesSystemJsonContext.Default, cancellationToken: cancellationToken);
    }
}
