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
     { var locale = ctx.Request.Query["locale"].FirstOrDefault(); var key = ctx.Request.Query["key"].FirstOrDefault(); return Results.Ok(ApiResult<List<I18nMessageItem>>.Ok(await svc.ListAsync(locale, key, ct))); }
     private static async Task<IResult> CreateAsync(CreateI18nRequest req, II18nService svc, CancellationToken ct) => Results.Ok(ApiResult<IdResponse>.Ok(new IdResponse(await svc.CreateAsync(req, ct))));
    private static async Task<IResult> UpdateAsync(long id, UpdateI18nRequest req, II18nService svc, CancellationToken ct) { await svc.UpdateAsync(id, req, ct); return Results.Ok(ApiResult<string>.Ok("updated")); }
    private static async Task<IResult> DeleteAsync(long id, II18nService svc, CancellationToken ct) { await svc.DeleteAsync(id, ct); return Results.Ok(ApiResult<string>.Ok("deleted")); }
 }
