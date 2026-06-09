using global::WeCms.Shared;
 
 
 namespace WeCms.Modules.System.Users;
 
 public static class UserEndpoints
 {
     public static RouteGroupBuilder MapUserEndpoints(this RouteGroupBuilder group)
     {
         group.MapGet("/system/users", ListAsync).RequirePermission("sys:user:list");
         group.MapGet("/system/users/{id:long}", GetAsync).RequirePermission("sys:user:list");
         group.MapPost("/system/users", CreateAsync).RequirePermission("sys:user:create");
         group.MapPut("/system/users/{id:long}", UpdateAsync).RequirePermission("sys:user:update");
         group.MapDelete("/system/users/{id:long}", DeleteAsync).RequirePermission("sys:user:delete");
         group.MapPatch("/system/users/{id:long}/status", SetStatusAsync).RequirePermission("sys:user:update");
         return group;
     }
 
     private static long GetOperatorId(HttpContext ctx) { var s = ctx.User.FindFirst("sub")?.Value; if (s is null || !long.TryParse(s, out var id)) throw new InvalidOperationException("User identity not found"); return id; }
 
     private static async Task<IResult> ListAsync([AsParameters] UserQueryParams q, IUserService svc, CancellationToken ct)
     {
         var (items, total) = await svc.ListAsync(q, ct);
         return Results.Ok(ApiResult<PagedResult<UserListItem>>.Ok(new(items, q.Page, q.PageSize, total)));
     }
 
     private static async Task<IResult> GetAsync(long id, IUserService svc, CancellationToken ct)
         => (await svc.GetByIdAsync(id, ct)) is UserDetail u ? Results.Ok(ApiResult<UserDetail>.Ok(u)) : Results.Ok(ApiResult<UserDetail>.Fail(ApiCodes.NotFound, "Not found"));

     private static async Task<IResult> CreateAsync(CreateUserRequest req, HttpContext ctx, IUserService svc, CancellationToken ct)
        => Results.Ok(ApiResult<IdResponse>.Ok(new IdResponse(await svc.CreateAsync(req, GetOperatorId(ctx), ct))));

     private static async Task<IResult> UpdateAsync(long id, UpdateUserRequest req, HttpContext ctx, IUserService svc, CancellationToken ct)
     { await svc.UpdateAsync(id, req, GetOperatorId(ctx), ct); return Results.Ok(ApiResult<string>.Ok("updated")); }
 
     private static async Task<IResult> DeleteAsync(long id, HttpContext ctx, IUserService svc, CancellationToken ct)
     { await svc.DeleteAsync(id, GetOperatorId(ctx), ct); return Results.Ok(ApiResult<string>.Ok("deleted")); }
 
     private static async Task<IResult> SetStatusAsync(long id, HttpContext ctx, IUserService svc, CancellationToken ct)
     { await svc.SetStatusAsync(id, ctx.Request.Query["status"].FirstOrDefault() ?? "active", GetOperatorId(ctx), ct); return Results.Ok(ApiResult<string>.Ok("updated")); }
 }
