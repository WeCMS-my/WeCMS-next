 using WeCms.Shared.Contracts;
 
 namespace WeCms.Modules.System.Menus;
 
 public sealed class MenuService(IDbConnectionFactory db) : IMenuService
 {
     public async Task<List<MenuTreeItem>> GetTreeAsync(CancellationToken ct)
     {
         await using var conn = await db.OpenAsync(ct);
         var all = await conn.QueryAsync<MenuFlat>(new CommandDefinition(
             "SELECT id, parent_id, type, name, path, component, title, icon, sort, hidden, status FROM sys_menu WHERE deleted_at IS NULL ORDER BY sort, id", cancellationToken: ct));
         return BuildTree(all.AsList(), null);
     }
 
     public async Task<MenuDetail?> GetByIdAsync(long id, CancellationToken ct)
     {
         await using var conn = await db.OpenAsync(ct);
         return await conn.QueryFirstOrDefaultAsync<MenuDetail>(new CommandDefinition(
             "SELECT id, parent_id, type, name, path, component, title, i18n_key, icon, sort, hidden, keep_alive, external_url, permission_code, status, created_at, updated_at FROM sys_menu WHERE id=@Id AND deleted_at IS NULL", new { Id = id }, cancellationToken: ct));
     }
 
     public async Task<long> CreateAsync(CreateMenuRequest req, CancellationToken ct)
     {
         await using var conn = await db.OpenAsync(ct);
         if (req.ParentId.HasValue)
         {
             var parent = await conn.QueryFirstOrDefaultAsync<MenuParent>(new CommandDefinition(
                 "SELECT id, parent_id FROM sys_menu WHERE id=@Id AND deleted_at IS NULL", new { Id = req.ParentId }, cancellationToken: ct));
             if (parent is null) throw new InvalidOperationException("Parent not found");
            // Circular reference check: new node has no children yet, so parent cannot be its descendant
        }
        return await conn.ExecuteScalarAsync<long>(new CommandDefinition(
             "INSERT INTO sys_menu (parent_id,type,name,path,component,title,icon,sort,hidden,permission_code,status,created_at,updated_at) VALUES (@P,@T,@N,@Pa,@C,@Ti,@I,@S,@H,@Pc,'active',@Now,@Now); SELECT LAST_INSERT_ID();",
             new { P = req.ParentId, T = req.Type, N = req.Name, Pa = req.Path, C = req.Component, Ti = req.Title, req.Icon, S = req.Sort, H = req.Hidden, Pc = req.PermissionCode, Now = DateTime.UtcNow }, cancellationToken: ct));
     }
 
     public async Task UpdateAsync(long id, UpdateMenuRequest req, CancellationToken ct)
     {
         await using var conn = await db.OpenAsync(ct);
         if (req.ParentId.HasValue)
         {
             // Check parent exists
             var parent = await conn.QueryFirstOrDefaultAsync<MenuParent>(new CommandDefinition(
                 "SELECT id, parent_id FROM sys_menu WHERE id=@Id AND deleted_at IS NULL", new { Id = req.ParentId }, cancellationToken: ct));
             if (parent is null) throw new InvalidOperationException("Parent not found");
             // Circular reference check
             if (await IsDescendant(conn, req.ParentId.Value, id, ct))
                 throw new InvalidOperationException("Cannot create circular menu reference");
         }
         // COALESCE allows partial updates: passing null preserves the existing column value
        await conn.ExecuteAsync(new CommandDefinition(
            "UPDATE sys_menu SET title=COALESCE(@T,title), path=COALESCE(@P,path), component=COALESCE(@C,component), icon=COALESCE(@I,icon), sort=COALESCE(@S,sort), hidden=COALESCE(@H,hidden), parent_id=COALESCE(@Pid,parent_id), row_version = row_version + 1, updated_at=@Now WHERE id=@Id",
             new { req.Title, req.Path, req.Component, req.Icon, S = req.Sort, H = req.Hidden, Pid = req.ParentId, Now = DateTime.UtcNow, Id = id }, cancellationToken: ct));
     }
 
     public async Task DeleteAsync(long id, CancellationToken ct)
     {
         await using var conn = await db.OpenAsync(ct);
         // Collect all descendant IDs
         var ids = new List<long> { id };
         await CollectDescendants(conn, id, ids, ct);
         // Soft-delete all
         foreach (var mid in ids)
             await conn.ExecuteAsync(new CommandDefinition("UPDATE sys_menu SET deleted_at=@Now WHERE id=@Id", new { Now = DateTime.UtcNow, Id = mid }, cancellationToken: ct));
     }
 
     public async Task SortAsync(long[] orderedIds, CancellationToken ct)
     {
         await using var conn = await db.OpenAsync(ct);
         for (var i = 0; i < orderedIds.Length; i++)
             await conn.ExecuteAsync(new CommandDefinition("UPDATE sys_menu SET sort=@S WHERE id=@Id", new { S = i, Id = orderedIds[i] }, cancellationToken: ct));
     }
 
     private static async Task<bool> IsDescendant(DbConnection conn, long targetId, long excludeId, CancellationToken ct)
     {
         // Walk up from targetId to root, checking if we hit excludeId
         long? current = targetId;
         var visited = new HashSet<long>();
         while (current.HasValue && current != 0)
         {
             if (!visited.Add(current.Value)) break; // safety: prevent infinite loop
             if (current.Value == excludeId) return true;
             var row = await conn.QueryFirstOrDefaultAsync<MenuParent>(new CommandDefinition(
                 "SELECT id, parent_id FROM sys_menu WHERE id=@Id AND deleted_at IS NULL", new { Id = current }, cancellationToken: ct));
             current = row?.ParentId;
         }
         return false;
     }
 
     private static async Task CollectDescendants(DbConnection conn, long parentId, List<long> result, CancellationToken ct)
     {
         var children = await conn.QueryAsync<long>(new CommandDefinition(
             "SELECT id FROM sys_menu WHERE parent_id=@Id AND deleted_at IS NULL", new { Id = parentId }, cancellationToken: ct));
         foreach (var childId in children)
         {
             result.Add(childId);
             await CollectDescendants(conn, childId, result, ct);
         }
     }
 
     private static List<MenuTreeItem> BuildTree(List<MenuFlat> items, long? parentId)
    {
        var lookup = items.ToLookup(m => m.ParentId);
        return BuildNodes(lookup, parentId);
    }
    private static List<MenuTreeItem> BuildNodes(ILookup<long?, MenuFlat> lookup, long? parentId)
    {
        return lookup[parentId].Select(m => new MenuTreeItem(m.Id, m.ParentId, m.Type, m.Name, m.Path, m.Component, m.Title, m.Icon, m.Sort, m.Hidden, m.Status, BuildNodes(lookup, m.Id))).ToList();
    }
 
     private sealed record MenuFlat(long Id, long? ParentId, string Type, string Name, string? Path, string? Component, string Title, string? Icon, int Sort, bool Hidden, string Status);
     private sealed record MenuParent(long Id, long? ParentId);
 }
