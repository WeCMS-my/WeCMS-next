using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using WeCms.Modules.System.Permissions;
using WeCms.Shared;
using WeCms.Shared.Data;
using WeCms.Shared.Security;
using WeCms.Shared.Time;

namespace WeCms.Modules.System.System;

/// <summary>
/// System endpoint handlers with strict constructor injection (no Service Locator).
/// </summary>
public sealed class SystemEndpointHandlers
{
    private readonly IClock _clock;
    private readonly IDbConnectionFactory _db;
    private readonly ILoggerFactory _loggerFactory;

    public SystemEndpointHandlers(IClock clock, IDbConnectionFactory db, ILoggerFactory loggerFactory)
    {
        _clock = clock;
        _db = db;
        _loggerFactory = loggerFactory;
    }

    public async Task HealthLiveAsync(HttpContext context)
    {
        var cancellationToken = context.RequestAborted;
        await WriteJsonResponse(
            context,
            ApiResult<HealthLiveResponse>.Ok(new HealthLiveResponse("healthy", _clock.UtcNow)),
            typeof(ApiResult<HealthLiveResponse>),
            cancellationToken);
    }

    public async Task HealthReadyAsync(HttpContext context)
    {
        var cancellationToken = context.RequestAborted;
        var sw = Stopwatch.StartNew();
        try
        {
            await using var connection = await _db.OpenAsync(cancellationToken);
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

    public async Task PingAsync(HttpContext context)
    {
        var cancellationToken = context.RequestAborted;
        await WriteJsonResponse(
            context,
            ApiResult<SystemPingResponse>.Ok(new SystemPingResponse("pong", TimeZoneInfo.Local.Id, _clock.UtcNow)),
            typeof(ApiResult<SystemPingResponse>),
            cancellationToken);
    }

    public async Task VersionAsync(HttpContext context)
    {
        var cancellationToken = context.RequestAborted;
        await WriteJsonResponse(
            context,
            ApiResult<SystemVersionResponse>.Ok(new SystemVersionResponse("0.1.0", "M0-BE", "net10.0")),
            typeof(ApiResult<SystemVersionResponse>),
            cancellationToken);
    }

    public async Task DbCheckAsync(HttpContext context)
    {
        var cancellationToken = context.RequestAborted;
        try
        {
            await using var connection = await _db.OpenAsync(cancellationToken);
            await WriteJsonResponse(
                context,
                ApiResult<DbCheckResponse>.Ok(new DbCheckResponse("connected", connection.Database, null)),
                typeof(ApiResult<DbCheckResponse>),
                cancellationToken);
        }
        catch (Exception ex)
        {
            var logger = _loggerFactory.CreateLogger(nameof(SystemEndpointHandlers));
            logger.LogError(ex, "数据库连接检查失败, traceId={TraceId}", context.TraceIdentifier);
            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            await WriteJsonResponse(
                context,
                ApiResult<DbCheckResponse>.Fail(ApiCodes.SystemError, "数据库连接检查失败", context.TraceIdentifier),
                typeof(ApiResult<DbCheckResponse>),
                cancellationToken);
        }
    }

    public async Task SecurePingAsync(HttpContext context)
    {
        var cancellationToken = context.RequestAborted;
        await WriteJsonResponse(
            context,
            ApiResult<SecurePingResponse>.Ok(new SecurePingResponse("secure-pong", _clock.UtcNow)),
            typeof(ApiResult<SecurePingResponse>),
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

/// <summary>
/// Endpoint route registration — thin AOT-compatible RequestDelegate wrappers with explicit OpenAPI metadata.
/// </summary>
public static class SystemEndpoints
{
    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Minimal API registration is validated by integration tests and AOT publish.")]
    [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Minimal API registration is validated by integration tests and AOT publish.")]
    public static void Map(WebApplication app)
    {
        var healthGroup = app.MapGroup("/health");
        var healthLive = (RouteHandlerBuilder)healthGroup.MapGet("/live", (RequestDelegate)HandleHealthLive);
        healthLive.Produces<ApiResult<HealthLiveResponse>>(StatusCodes.Status200OK);
        healthLive.WithName("System_HealthLive");

        var healthReady = (RouteHandlerBuilder)healthGroup.MapGet("/ready", (RequestDelegate)HandleHealthReady);
        healthReady.Produces<ApiResult<HealthReadyResponse>>(StatusCodes.Status200OK);
        healthReady.WithName("System_HealthReady");

        var systemGroup = app.MapGroup("/api/v1/system");
        var ping = (RouteHandlerBuilder)systemGroup.MapGet("/ping", (RequestDelegate)HandlePing);
        ping.Produces<ApiResult<SystemPingResponse>>(StatusCodes.Status200OK);
        ping.WithName("System_Ping");

        var version = (RouteHandlerBuilder)systemGroup.MapGet("/version", (RequestDelegate)HandleVersion);
        version.Produces<ApiResult<SystemVersionResponse>>(StatusCodes.Status200OK);
        version.WithName("System_Version");

        var dbCheck = (RouteHandlerBuilder)systemGroup.MapGet("/db-check", (RequestDelegate)HandleDbCheck);
        dbCheck.Produces<ApiResult<DbCheckResponse>>(StatusCodes.Status200OK);
        dbCheck.Produces<ApiResult<DbCheckResponse>>(StatusCodes.Status503ServiceUnavailable);
        dbCheck.WithName("System_DbCheck");

        // M0-BE-009: secure-ping with RequirePermission
        var securePing = (RouteHandlerBuilder)systemGroup.MapGet("/secure-ping", (RequestDelegate)HandleSecurePing);
        securePing.Produces<ApiResult<SecurePingResponse>>(StatusCodes.Status200OK);
        securePing.WithName("System_SecurePing");
        securePing.RequirePermission(SystemPermissions.SystemSecurePing);
    }

    private static Task HandleHealthLive(HttpContext context) =>
        context.RequestServices.GetRequiredService<SystemEndpointHandlers>().HealthLiveAsync(context);

    private static Task HandleHealthReady(HttpContext context) =>
        context.RequestServices.GetRequiredService<SystemEndpointHandlers>().HealthReadyAsync(context);

    private static Task HandlePing(HttpContext context) =>
        context.RequestServices.GetRequiredService<SystemEndpointHandlers>().PingAsync(context);

    private static Task HandleVersion(HttpContext context) =>
        context.RequestServices.GetRequiredService<SystemEndpointHandlers>().VersionAsync(context);

    private static Task HandleDbCheck(HttpContext context) =>
        context.RequestServices.GetRequiredService<SystemEndpointHandlers>().DbCheckAsync(context);

    private static Task HandleSecurePing(HttpContext context) =>
        context.RequestServices.GetRequiredService<SystemEndpointHandlers>().SecurePingAsync(context);
}
