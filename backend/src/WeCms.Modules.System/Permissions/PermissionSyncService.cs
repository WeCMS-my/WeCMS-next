 using WeCms.Shared.Contracts;
 
 namespace WeCms.Modules.System.Permissions;
 
 public sealed class PermissionSyncService(IDbConnectionFactory db)
 {
     public async Task<int> SyncAsync(CancellationToken ct)
     {
         await using var conn = await db.OpenAsync(ct);
         var codes = new (string Code, string Name)[]
         {
             ("sys:user:list", "View users"), ("sys:user:create", "Create user"), ("sys:user:update", "Update user"), ("sys:user:delete", "Delete user"),
             ("sys:user:reset-password", "Reset user password"), ("sys:user:assign-role", "Assign user roles"),
             ("sys:role:list", "View roles"), ("sys:role:create", "Create role"), ("sys:role:update", "Update role"), ("sys:role:delete", "Delete role"),
             ("sys:role:assign-menu", "Assign role menus"), ("sys:role:assign-permission", "Assign role permissions"),
             ("sys:menu:list", "View menus"), ("sys:menu:create", "Create menu"), ("sys:menu:update", "Update menu"), ("sys:menu:delete", "Delete menu"), ("sys:menu:sort", "Sort menus"),
             ("sys:permission:list", "View permissions"), ("sys:permission:sync", "Sync permissions")
         };
         var count = 0;
         foreach (var (code, name) in codes)
         {
             var affected = await conn.ExecuteAsync(new CommandDefinition(
                 "INSERT INTO sys_permission (code, name, module, resource, action, status, created_at, updated_at) VALUES (@C,@N,'system',@R,@A,'active',@Now,@Now) ON DUPLICATE KEY UPDATE name=@N, updated_at=@Now",
                 new { C = code, N = name, R = code.Split(':')[1], A = code.Split(':')[2], Now = DateTime.UtcNow }, cancellationToken: ct));
             count += affected;
         }
         return count;
     }
 }
