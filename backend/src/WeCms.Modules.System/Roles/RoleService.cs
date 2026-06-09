 using WeCms.Shared.Contracts;
 
 namespace WeCms.Modules.System.Roles;
 
 public sealed class RoleService(IDbConnectionFactory db)
 {
     public async Task<(IReadOnlyList<RoleListItem> Items, long Total)> ListAsync(int page, int size, CancellationToken ct)
     { await using var c = await db.OpenAsync(ct); var items = await c.QueryAsync<RoleListItem>(new CommandDefinition("SELECT id, code, name, status, sort, created_at FROM sys_role WHERE deleted_at IS NULL ORDER BY sort, id LIMIT @L OFFSET @O", new { L = Math.Min(size, 100), O = (page - 1) * size }, cancellationToken: ct)); var total = await c.ExecuteScalarAsync<long>(new CommandDefinition("SELECT COUNT(1) FROM sys_role WHERE deleted_at IS NULL", cancellationToken: ct)); return (items.AsList(), total); }
     public async Task<RoleDetail?> GetByIdAsync(long id, CancellationToken ct)
     { await using var c = await db.OpenAsync(ct); return await c.QueryFirstOrDefaultAsync<RoleDetail>(new CommandDefinition("SELECT id, code, name, description, status, sort, data_scope, created_at, updated_at FROM sys_role WHERE id=@Id AND deleted_at IS NULL", new { Id = id }, cancellationToken: ct)); }
     public async Task<long> CreateAsync(CreateRoleRequest req, CancellationToken ct)
     { await using var c = await db.OpenAsync(ct); return await c.ExecuteScalarAsync<long>(new CommandDefinition("INSERT INTO sys_role (code,name,description,status,sort,created_at,updated_at) VALUES (@C,@N,@D,'active',@S,@Now,@Now); SELECT LAST_INSERT_ID();", new { C = req.Code, N = req.Name, req.Description, S = req.Sort, Now = DateTime.UtcNow }, cancellationToken: ct)); }
     public async Task UpdateAsync(long id, UpdateRoleRequest req, CancellationToken ct)
     { await using var c = await db.OpenAsync(ct); await c.ExecuteAsync(new CommandDefinition("UPDATE sys_role SET name=COALESCE(@N,name), description=@D, status=COALESCE(@S,status), sort=COALESCE(@So,sort), updated_at=@Now WHERE id=@Id", new { req.Name, req.Description, req.Status, So = req.Sort, Now = DateTime.UtcNow, Id = id }, cancellationToken: ct)); }
     public async Task DeleteAsync(long id, CancellationToken ct)
     { await using var c = await db.OpenAsync(ct); var used = await c.ExecuteScalarAsync<int>(new CommandDefinition("SELECT COUNT(1) FROM sys_user_role WHERE role_id=@Id", new { Id = id }, cancellationToken: ct)); if (used > 0) throw new InvalidOperationException("Role is assigned to users"); await c.ExecuteAsync(new CommandDefinition("UPDATE sys_role SET deleted_at=@Now WHERE id=@Id", new { Now = DateTime.UtcNow, Id = id }, cancellationToken: ct)); }
     public async Task AssignMenusAsync(long roleId, long[] menuIds, CancellationToken ct)
     { await using var c = await db.OpenAsync(ct); await c.ExecuteAsync(new CommandDefinition("DELETE FROM sys_role_menu WHERE role_id=@Id", new { Id = roleId }, cancellationToken: ct)); if (menuIds.Length > 0) await c.ExecuteAsync(new CommandDefinition("INSERT INTO sys_role_menu (role_id,menu_id) VALUES (@R,@M)", menuIds.Select(m => new { R = roleId, M = m }), cancellationToken: ct)); await BumpPermissionVersion(c, roleId, ct); }
     public async Task AssignPermissionsAsync(long roleId, long[] permIds, CancellationToken ct)
     { await using var c = await db.OpenAsync(ct); await c.ExecuteAsync(new CommandDefinition("DELETE FROM sys_role_permission WHERE role_id=@Id", new { Id = roleId }, cancellationToken: ct)); if (permIds.Length > 0) await c.ExecuteAsync(new CommandDefinition("INSERT INTO sys_role_permission (role_id,permission_id) VALUES (@R,@P)", permIds.Select(p => new { R = roleId, P = p }), cancellationToken: ct)); await BumpPermissionVersion(c, roleId, ct); }
 
     private static async Task BumpPermissionVersion(DbConnection conn, long roleId, CancellationToken ct)
     { await conn.ExecuteAsync(new CommandDefinition("UPDATE sys_user SET permission_version=permission_version+1 WHERE id IN (SELECT user_id FROM sys_user_role WHERE role_id=@Id)", new { Id = roleId }, cancellationToken: ct)); }
 }
