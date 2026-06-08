using WeCms.Shared.Contracts;
using WeCms.Shared;

namespace WeCms.Modules.System;

public static class SystemEndpoints
{
    public static RouteGroupBuilder MapSystemEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/ping", () => ApiResult<object>.Ok(new { ping = "pong" })).AllowAnonymous();
        group.MapGet("/version", () => ApiResult<object>.Ok(new { version = "0.1.0-m0", runtime = "net10.0-aot" })).AllowAnonymous();
        group.MapGet("/db-check", async (IDbConnectionFactory db, CancellationToken ct) =>
        {
            try { await using var c = await db.OpenAsync(ct); await using var cmd = c.CreateCommand(); cmd.CommandText = "SELECT 1"; await cmd.ExecuteScalarAsync(ct); return Results.Ok(ApiResult<object>.Ok(new { status = "connected", database = "mysql" })); }
            catch (Exception ex) { return Results.Ok(ApiResult<object>.Fail(ApiCodes.SystemError, $"DB connection failed: {ex.Message}")); }
        }).AllowAnonymous();
        return group;
    }
}