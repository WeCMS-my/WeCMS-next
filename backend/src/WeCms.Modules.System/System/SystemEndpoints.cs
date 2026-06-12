using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
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

    public ApiResult<HealthLiveResponse> GetHealthLive()
        => ApiResult<HealthLiveResponse>.Ok(new HealthLiveResponse("healthy", _clock.UtcNow));

    public async Task<ApiResult<HealthReadyResponse>> GetHealthReadyAsync(CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await using var connection = await _db.OpenAsync(cancellationToken);
            sw.Stop();
            return ApiResult<HealthReadyResponse>.Ok(new HealthReadyResponse("ready", true, sw.ElapsedMilliseconds));
        }
        catch (Exception)
        {
            sw.Stop();
            return new ApiResult<HealthReadyResponse>(
                ApiCodes.SystemError,
                "数据库未就绪",
                new HealthReadyResponse("not ready", false, null));
        }
    }

    public ApiResult<SystemPingResponse> GetPing()
        => ApiResult<SystemPingResponse>.Ok(new SystemPingResponse("pong", TimeZoneInfo.Local.Id, _clock.UtcNow));

    public ApiResult<SystemVersionResponse> GetVersion()
        => ApiResult<SystemVersionResponse>.Ok(new SystemVersionResponse("0.1.0", "M0-BE", "net10.0"));

    public async Task<ApiResult<DbCheckResponse>> GetDbCheckAsync(
        HttpContext context,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = await _db.OpenAsync(cancellationToken);
            return ApiResult<DbCheckResponse>.Ok(new DbCheckResponse("connected", connection.Database, null));
        }
        catch (Exception ex)
        {
            var logger = _loggerFactory.CreateLogger(nameof(SystemEndpointHandlers));
            logger.LogError(ex, "数据库连接检查失败, traceId={TraceId}", context.TraceIdentifier);
            return new ApiResult<DbCheckResponse>(
                ApiCodes.SystemError,
                "数据库连接检查失败",
                new DbCheckResponse("disconnected", "", "数据库连接检查失败"),
                context.TraceIdentifier);
        }
    }

    public ApiResult<SecurePingResponse> GetSecurePing()
        => ApiResult<SecurePingResponse>.Ok(new SecurePingResponse("secure-pong", _clock.UtcNow));
}

/// <summary>
/// Endpoint route registration — thin AOT-compatible RequestDelegate wrappers with explicit OpenAPI metadata.
/// </summary>
public static class SystemEndpoints
{
    public static void Map(WebApplication app, SystemEndpointHandlers handlers)
    {
        var healthGroup = app.MapGroup("/health");
        var healthLive = (RouteHandlerBuilder)healthGroup.MapGet("/live", (RequestDelegate)(context => SystemEndpointRequestDelegates.HandleHealthLiveAsync(context, handlers)));
        healthLive.Produces<ApiResult<HealthLiveResponse>>(StatusCodes.Status200OK);
        healthLive.WithName("System_HealthLive");

        var healthReady = (RouteHandlerBuilder)healthGroup.MapGet("/ready", (RequestDelegate)(context => SystemEndpointRequestDelegates.HandleHealthReadyAsync(context, handlers)));
        healthReady.Produces<ApiResult<HealthReadyResponse>>(StatusCodes.Status200OK);
        healthReady.Produces<ApiResult<HealthReadyResponse>>(StatusCodes.Status503ServiceUnavailable);
        healthReady.WithName("System_HealthReady");

        var systemGroup = app.MapGroup("/api/v1/system");
        var ping = (RouteHandlerBuilder)systemGroup.MapGet("/ping", (RequestDelegate)(context => SystemEndpointRequestDelegates.HandlePingAsync(context, handlers)));
        ping.Produces<ApiResult<SystemPingResponse>>(StatusCodes.Status200OK);
        ping.WithName("System_Ping");

        var version = (RouteHandlerBuilder)systemGroup.MapGet("/version", (RequestDelegate)(context => SystemEndpointRequestDelegates.HandleVersionAsync(context, handlers)));
        version.Produces<ApiResult<SystemVersionResponse>>(StatusCodes.Status200OK);
        version.WithName("System_Version");

        var dbCheck = (RouteHandlerBuilder)systemGroup.MapGet("/db-check", (RequestDelegate)(context => SystemEndpointRequestDelegates.HandleDbCheckAsync(context, handlers)));
        dbCheck.Produces<ApiResult<DbCheckResponse>>(StatusCodes.Status200OK);
        dbCheck.Produces<ApiResult<DbCheckResponse>>(StatusCodes.Status503ServiceUnavailable);
        dbCheck.WithName("System_DbCheck");

        // M0-BE-009: secure-ping with RequirePermission
        var securePing = (RouteHandlerBuilder)systemGroup.MapGet("/secure-ping", (RequestDelegate)(context => SystemEndpointRequestDelegates.HandleSecurePingAsync(context, handlers)));
        securePing.Produces<ApiResult<SecurePingResponse>>(StatusCodes.Status200OK);
        securePing.WithName("System_SecurePing");
        securePing.RequirePermission(SystemPermissions.SystemSecurePing);
    }
}
