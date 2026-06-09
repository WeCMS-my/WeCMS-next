using WeCms.Shared.Contracts;
 using WeCms.Shared;
 
 namespace WeCms.Modules.System.Permissions;
 
 public static class PermissionEndpoints
 {
     public static RouteGroupBuilder MapPermissionEndpoints(this RouteGroupBuilder group)
     {
         group.MapGet("/system/permissions", ListAsync).RequirePermission("sys:permission:list");
         group.MapPost("/system/permissions/sync", SyncAsync).RequirePermission("sys:permission:sync");
         return group;
     }
     private static async Task<IResult> ListAsync(IDbConnectionFactory db, CancellationToken ct)
     { await using var c = await db.OpenAsync(ct); var items = await c.QueryAsync<PermissionItem>(new CommandDefinition("SELECT id, code, name, module, resource, action, status FROM sys_permission ORDER BY module, resource, action", cancellationToken: ct)); return Results.Ok(ApiResult<IReadOnlyList<PermissionItem>>.Ok(items.AsList())); }
     private static async Task<IResult> SyncAsync(PermissionSyncService svc, CancellationToken ct)
         => Results.Ok(ApiResult<SyncResultResponse>.Ok(new SyncResultResponse(await svc.SyncAsync(ct))));
 }
