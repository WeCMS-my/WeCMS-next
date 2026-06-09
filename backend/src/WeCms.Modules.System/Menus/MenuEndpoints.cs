using global::WeCms.Shared;
 
 
 namespace WeCms.Modules.System.Menus;
 
 public static class MenuEndpoints
 {
     public static RouteGroupBuilder MapMenuEndpoints(this RouteGroupBuilder group)
     {
         group.MapGet("/system/menus/tree", GetTreeAsync).RequirePermission("sys:menu:list");
         group.MapGet("/system/menus/{id:long}", GetAsync).RequirePermission("sys:menu:list");
         group.MapPost("/system/menus", CreateAsync).RequirePermission("sys:menu:create");
         group.MapPut("/system/menus/{id:long}", UpdateAsync).RequirePermission("sys:menu:update");
         group.MapDelete("/system/menus/{id:long}", DeleteAsync).RequirePermission("sys:menu:delete");
         group.MapPut("/system/menus/sort", SortAsync).RequirePermission("sys:menu:sort");
         return group;
     }
     private static async Task<IResult> GetTreeAsync(MenuService svc, CancellationToken ct) => Results.Ok(ApiResult<List<MenuTreeItem>>.Ok(await svc.GetTreeAsync(ct)));
     private static async Task<IResult> GetAsync(long id, MenuService svc, CancellationToken ct) => (await svc.GetByIdAsync(id, ct)) is MenuDetail m ? Results.Ok(ApiResult<MenuDetail>.Ok(m)) : Results.Ok(ApiResult<MenuDetail>.Fail(ApiCodes.NotFound, "Not found"));
     private static async Task<IResult> CreateAsync(CreateMenuRequest req, MenuService svc, CancellationToken ct) => Results.Ok(ApiResult<object>.Ok(new { id = await svc.CreateAsync(req, ct) }));
     private static async Task<IResult> UpdateAsync(long id, UpdateMenuRequest req, MenuService svc, CancellationToken ct) { await svc.UpdateAsync(id, req, ct); return Results.Ok(ApiResult<string>.Ok("updated")); }
     private static async Task<IResult> DeleteAsync(long id, MenuService svc, CancellationToken ct) { await svc.DeleteAsync(id, ct); return Results.Ok(ApiResult<string>.Ok("deleted")); }
     private static async Task<IResult> SortAsync(MenuSortRequest req, MenuService svc, CancellationToken ct) { await svc.SortAsync(req.OrderedIds, ct); return Results.Ok(ApiResult<string>.Ok("sorted")); }
 }
