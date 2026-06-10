using WeCms.Shared;

namespace WeCms.Modules.System.I18n;

public static class I18nEndpoints
{
    public static RouteGroupBuilder MapI18nEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/system/i18n", ListAsync).RequirePermission("sys:i18n:list");
        group.MapPost("/system/i18n", CreateAsync).RequirePermission("sys:i18n:create");
        group.MapPut("/system/i18n/{id:long}", UpdateAsync).RequirePermission("sys:i18n:update");
        group.MapDelete("/system/i18n/{id:long}", DeleteAsync).RequirePermission("sys:i18n:delete");
        return group;
    }

    private static async Task<IResult> ListAsync(HttpContext ctx, II18nService svc, CancellationToken ct)
    {
        var locale = ctx.Request.Query["locale"].FirstOrDefault();
        var key = ctx.Request.Query["key"].FirstOrDefault();
        var p = int.TryParse(ctx.Request.Query["page"], out var pp) ? Math.Max(pp, 1) : 1;
        var s = int.TryParse(ctx.Request.Query["pageSize"], out var ps) ? Math.Clamp(ps, 1, 100) : 20;
        var (items, total) = await svc.ListAsync(locale, key, p, s, ct);
        return Results.Ok(ApiResult<PagedResult<I18nMessageItem>>.Ok(new PagedResult<I18nMessageItem>(items, p, s, total)));
    }

    private static async Task<IResult> CreateAsync(CreateI18nRequest req, II18nService svc, CancellationToken ct) => Results.Ok(ApiResult<IdResponse>.Ok(new IdResponse(await svc.CreateAsync(req, ct))));
    private static async Task<IResult> UpdateAsync(long id, UpdateI18nRequest req, II18nService svc, CancellationToken ct) { await svc.UpdateAsync(id, req, ct); return Results.Ok(ApiResult<string>.Ok("updated")); }
    private static async Task<IResult> DeleteAsync(long id, II18nService svc, CancellationToken ct) { await svc.DeleteAsync(id, ct); return Results.Ok(ApiResult<string>.Ok("deleted")); }
}
