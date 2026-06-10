using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using WeCms.Infrastructure.Data;
using WeCms.Modules.System.Permissions;
using WeCms.Shared;
using WeCms.Shared.Security;
using WeCms.Shared.Time;

#pragma warning disable IL2026, IL3050
// Minimal API MapGet/MapPost use delegate reflection — handled by ASP.NET Core source generators at publish time.

namespace WeCms.Modules.System.System;

public static class SystemEndpoints
{
    public static void Map(WebApplication app)
    {
        var healthGroup = app.MapGroup("/health");
        healthGroup.MapGet("/live", HealthLive);
        healthGroup.MapGet("/ready", HealthReady);

        var systemGroup = app.MapGroup("/api/v1/system");
        systemGroup.MapGet("/ping", Ping);
        systemGroup.MapGet("/version", Version);
        systemGroup.MapGet("/db-check", DbCheck);

        // M0-BE-009: secure-ping with RequirePermission
        systemGroup.MapGet("/secure-ping", SecurePing)
            .RequirePermission(SystemPermissions.SystemSecurePing);
    }

    private static IResult HealthLive(IClock clock)
        => Results.Ok(ApiResult<HealthLiveResponse>.Ok(
            new HealthLiveResponse("healthy", clock.UtcNow)));

    private static async Task<IResult> HealthReady(
        IDbConnectionFactory db,
        IClock clock,
        CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            await using var connection = await db.OpenAsync(cancellationToken);
            sw.Stop();
            return Results.Ok(ApiResult<HealthReadyResponse>.Ok(
                new HealthReadyResponse("ready", true, sw.ElapsedMilliseconds)));
        }
        catch (Exception)
        {
            sw.Stop();
            return Results.Ok(ApiResult<HealthReadyResponse>.Ok(
                new HealthReadyResponse("not ready", false, null)));
        }
    }

    private static IResult Ping(IClock clock)
        => Results.Ok(ApiResult<SystemPingResponse>.Ok(
            new SystemPingResponse("pong", TimeZoneInfo.Local.Id, clock.UtcNow)));

    private static IResult Version()
        => Results.Ok(ApiResult<SystemVersionResponse>.Ok(
            new SystemVersionResponse("0.1.0", "M0-BE", "net10.0")));

    private static async Task<IResult> DbCheck(
        IDbConnectionFactory db,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = await db.OpenAsync(cancellationToken);
            return Results.Ok(ApiResult<DbCheckResponse>.Ok(
                new DbCheckResponse("connected", connection.Database, null)));
        }
        catch (Exception ex)
        {
            return Results.Ok(ApiResult<DbCheckResponse>.Ok(
                new DbCheckResponse("failed", "unknown", ex.Message)));
        }
    }

    private static IResult SecurePing(IClock clock)
        => Results.Ok(ApiResult<object?>.Ok(new { status = "secure-pong", timestamp = clock.UtcNow }));
}
