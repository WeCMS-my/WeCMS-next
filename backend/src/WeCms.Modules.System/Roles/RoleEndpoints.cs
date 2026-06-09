using global::WeCms.Shared;
 
 
 namespace WeCms.Modules.System.Roles;
 
 public static class RoleEndpoints
 {
     public static RouteGroupBuilder MapRoleEndpoints(this RouteGroupBuilder group)
     {
         group.MapGet("/system/roles", ListAsync).RequirePermission("sys:role:list");
         group.MapGet("/system/roles/{id:long}", GetAsync).RequirePermission("sys:role:list");
         group.MapPost("/system/roles", CreateAsync).RequirePermission("sys:role:create");
         group.MapPut("/system/roles/{id:long}", UpdateAsync).RequirePermission("sys:role:update");
         group.MapDelete("/system/roles/{id:long}", DeleteAsync).RequirePermission("sys:role:delete");
         group.MapPut("/system/roles/{id:long}/menus", AssignMenusAsync).RequirePermission("sys:role:assign-menu");
         group.MapPut("/system/roles/{id:long}/permissions", AssignPermissionsAsync).RequirePermission("sys:role:assign-permission");
         return group;
     }
 
     private static async Task<IResult> ListAsync(HttpContext ctx, IRoleService svc, CancellationToken ct)
     { var p = int.TryParse(ctx.Request.Query["page"], out var pp) ? pp : 1; var s = int.TryParse(ctx.Request.Query["pageSize"], out var ps) ? ps : 20; var (items, total) = await svc.ListAsync(p, s, ct); return Results.Ok(ApiResult<PagedResult<RoleListItem>>.Ok(new PagedResult<RoleListItem>(items, p, s, total))); }
     private static async Task<IResult> GetAsync(long id, IRoleService svc, CancellationToken ct) => (await svc.GetByIdAsync(id, ct)) is RoleDetail r ? Results.Ok(ApiResult<RoleDetail>.Ok(r)) : Results.Ok(ApiResult<RoleDetail>.Fail(ApiCodes.NotFound, "Not found"));
    private static async Task<IResult> CreateAsync(CreateRoleRequest req, IRoleService svc, CancellationToken ct) => Results.Ok(ApiResult<IdResponse>.Ok(new IdResponse(await svc.CreateAsync(req, ct))));
    private static async Task<IResult> UpdateAsync(long id, UpdateRoleRequest req, IRoleService svc, CancellationToken ct) { await svc.UpdateAsync(id, req, ct); return Results.Ok(ApiResult<string>.Ok("updated")); }
    private static async Task<IResult> DeleteAsync(long id, IRoleService svc, CancellationToken ct) { await svc.DeleteAsync(id, ct); return Results.Ok(ApiResult<string>.Ok("deleted")); }
    private static async Task<IResult> AssignMenusAsync(long id, AssignMenusRequest req, IRoleService svc, CancellationToken ct) { await svc.AssignMenusAsync(id, req.MenuIds, ct); return Results.Ok(ApiResult<string>.Ok("assigned")); }
    private static async Task<IResult> AssignPermissionsAsync(long id, AssignPermissionsRequest req, IRoleService svc, CancellationToken ct) { await svc.AssignPermissionsAsync(id, req.PermissionIds, ct); return Results.Ok(ApiResult<string>.Ok("assigned")); }
 }
