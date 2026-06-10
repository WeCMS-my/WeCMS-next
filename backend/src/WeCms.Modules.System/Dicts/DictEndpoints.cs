using WeCms.Shared;

namespace WeCms.Modules.System.Dicts;

public static class DictEndpoints
{
    public static RouteGroupBuilder MapDictEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/system/dicts/types", ListTypesAsync).RequirePermission("sys:dict:list");
        group.MapPost("/system/dicts/types", CreateTypeAsync).RequirePermission("sys:dict:create");
        group.MapDelete("/system/dicts/types/{id:long}", DeleteTypeAsync).RequirePermission("sys:dict:delete");
        group.MapGet("/system/dicts/types/{typeId:long}/values", ListValuesAsync).RequirePermission("sys:dict:list");
        group.MapPost("/system/dicts/values", CreateValueAsync).RequirePermission("sys:dict:create");
        group.MapDelete("/system/dicts/values/{id:long}", DeleteValueAsync).RequirePermission("sys:dict:delete");
        return group;
    }

    private static async Task<IResult> ListTypesAsync(HttpContext ctx, IDictService svc, CancellationToken ct)
    {
        var p = int.TryParse(ctx.Request.Query["page"], out var pp) ? Math.Max(pp, 1) : 1;
        var s = int.TryParse(ctx.Request.Query["pageSize"], out var ps) ? Math.Clamp(ps, 1, 100) : 20;
        var (items, total) = await svc.GetTypesAsync(p, s, ct);
        return Results.Ok(ApiResult<PagedResult<DictTypeItem>>.Ok(new PagedResult<DictTypeItem>(items, p, s, total)));
    }

    private static async Task<IResult> ListValuesAsync(long typeId, HttpContext ctx, IDictService svc, CancellationToken ct)
    {
        var p = int.TryParse(ctx.Request.Query["page"], out var pp) ? Math.Max(pp, 1) : 1;
        var s = int.TryParse(ctx.Request.Query["pageSize"], out var ps) ? Math.Clamp(ps, 1, 100) : 20;
        var (items, total) = await svc.GetValuesAsync(typeId, p, s, ct);
        return Results.Ok(ApiResult<PagedResult<DictValueItem>>.Ok(new PagedResult<DictValueItem>(items, p, s, total)));
    }

    private static async Task<IResult> CreateTypeAsync(CreateDictTypeRequest req, IDictService svc, CancellationToken ct) => Results.Ok(ApiResult<IdResponse>.Ok(new IdResponse(await svc.CreateTypeAsync(req, ct))));
    private static async Task<IResult> DeleteTypeAsync(long id, IDictService svc, CancellationToken ct) { await svc.DeleteTypeAsync(id, ct); return Results.Ok(ApiResult<string>.Ok("deleted")); }
    private static async Task<IResult> CreateValueAsync(CreateDictValueRequest req, IDictService svc, CancellationToken ct) => Results.Ok(ApiResult<IdResponse>.Ok(new IdResponse(await svc.CreateValueAsync(req, ct))));
    private static async Task<IResult> DeleteValueAsync(long id, IDictService svc, CancellationToken ct) { await svc.DeleteValueAsync(id, ct); return Results.Ok(ApiResult<string>.Ok("deleted")); }
}
