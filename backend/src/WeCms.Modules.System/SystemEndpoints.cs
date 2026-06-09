using WeCms.Shared.Contracts;
using WeCms.Shared;

namespace WeCms.Modules.System;

public static class SystemEndpoints
{
    public static RouteGroupBuilder MapSystemEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/ping", () => ApiResult<PongResponse>.Ok(new PongResponse("pong"))).AllowAnonymous();
        group.MapGet("/version", () => ApiResult<VersionResponse>.Ok(new VersionResponse("0.1.0-m0", "net10.0-aot"))).AllowAnonymous();
        group.MapGet("/db-check", async (IDbConnectionFactory db, CancellationToken ct) =>
        {
            try { await using var c = await db.OpenAsync(ct); await using var cmd = c.CreateCommand(); cmd.CommandText = "SELECT 1"; await cmd.ExecuteScalarAsync(ct); return Results.Ok(ApiResult<DbCheckResponse>.Ok(new DbCheckResponse("connected", "mysql"))); }
            catch { return Results.Ok(ApiResult<DbCheckResponse>.Fail(ApiCodes.SystemError, "DB connection failed")); }
        }).AllowAnonymous();
        group.MapGet("/health/ready", async (IDbConnectionFactory db, CancellationToken ct) =>
        {
            try { await using var c = await db.OpenAsync(ct); await using var cmd = c.CreateCommand(); cmd.CommandText = "SELECT 1"; await cmd.ExecuteScalarAsync(ct); return Results.Ok(new HealthReadyResponse("ready", "connected")); }
            catch { return Results.Problem("Database not reachable", statusCode: 503); }
        }).AllowAnonymous();
        return group;
    }
}
