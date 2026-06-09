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
            ("sys:role:list", "View roles"), ("sys:role:create", "Create role"), ("sys:role:update", "Update role"), ("sys:role:delete", "Delete role"),
            ("sys:role:assign-menu", "Assign role menus"), ("sys:role:assign-permission", "Assign role permissions"),
            ("sys:menu:list", "View menus"), ("sys:menu:create", "Create menu"), ("sys:menu:update", "Update menu"), ("sys:menu:delete", "Delete menu"), ("sys:menu:sort", "Sort menus"),
            ("sys:permission:list", "View permissions"), ("sys:permission:sync", "Sync permissions"),
            ("sys:dict:list", "View dicts"), ("sys:dict:create", "Create dict"), ("sys:dict:delete", "Delete dict"),
            ("sys:file:list", "View files"), ("sys:file:upload", "Upload file"), ("sys:file:download", "Download file"), ("sys:file:delete", "Delete file"),
            ("sys:setting:list", "View settings"), ("sys:setting:update", "Update setting"),
            ("sys:log:login:list", "View login logs"), ("sys:log:audit:list", "View audit logs"),
            ("sys:security:event:list", "View security events"),
            ("sys:i18n:list", "View i18n"), ("sys:i18n:create", "Create i18n"), ("sys:i18n:update", "Update i18n"), ("sys:i18n:delete", "Delete i18n")
        };
         var count = 0;
         foreach (var (code, name) in codes)
         {
             var lastColon = code.LastIndexOf(':');
            var resource = code[(code.IndexOf(':') + 1)..lastColon];
            var action = code[(lastColon + 1)..];
            var affected = await conn.ExecuteAsync(new CommandDefinition(
                "INSERT INTO sys_permission (code, name, module, resource, action, status, created_at, updated_at) VALUES (@C,@N,'system',@R,@A,'active',@Now,@Now) ON DUPLICATE KEY UPDATE name=@N, updated_at=@Now",
                new { C = code, N = name, R = resource, A = action, Now = DateTime.UtcNow }, cancellationToken: ct));
             count += affected;
         }
         return count;
     }
 }
