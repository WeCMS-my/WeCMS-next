using WeCms.Shared;

namespace WeCms.Modules.System.Settings;

public static class SettingEndpoints
{
    public static RouteGroupBuilder MapSettingEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/system/settings", ListAsync).RequirePermission("sys:setting:list");
        group.MapPut("/system/settings/{key}", UpdateAsync).RequirePermission("sys:setting:update");
        return group;
    }

    private static async Task<IResult> ListAsync(HttpContext ctx, ISettingService svc, CancellationToken ct)
    {
        var p = int.TryParse(ctx.Request.Query["page"], out var pp) ? Math.Max(pp, 1) : 1;
        var s = int.TryParse(ctx.Request.Query["pageSize"], out var ps) ? Math.Clamp(ps, 1, 100) : 20;
        var (items, total) = await svc.ListAsync(p, s, ct);
        return Results.Ok(ApiResult<PagedResult<SettingItem>>.Ok(new PagedResult<SettingItem>(items, p, s, total)));
    }

    private static async Task<IResult> UpdateAsync(string key, UpdateSettingRequest req, ISettingService svc, CancellationToken ct)
    { await svc.UpdateAsync(key, req.Value, ct); return Results.Ok(ApiResult<string>.Ok("saved")); }
}
