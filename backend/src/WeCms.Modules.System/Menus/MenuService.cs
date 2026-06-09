using WeCms.Shared.Contracts;

namespace WeCms.Modules.System.Menus;

public sealed class MenuService(IDbConnectionFactory db, IClock clock, IAuditWriter audit) : IMenuService
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
        var id = await conn.ExecuteScalarAsync<long>(new CommandDefinition(
            "INSERT INTO sys_menu (parent_id,type,name,path,component,title,i18n_key,icon,sort,hidden,keep_alive,external_url,permission_code,status,created_at,updated_at) VALUES (@P,@T,@N,@Pa,@C,@Ti,@Ik,@I,@S,@H,@Ka,@Eu,@Pc,'active',@Now,@Now); SELECT LAST_INSERT_ID();",
            new { P = req.ParentId, T = req.Type, N = req.Name, Pa = req.Path, C = req.Component, Ti = req.Title, Ik = req.I18nKey, req.Icon, S = req.Sort, H = req.Hidden, Ka = req.KeepAlive, Eu = req.ExternalUrl, Pc = req.PermissionCode, Now = clock.UtcNow.DateTime }, cancellationToken: ct));
        await audit.LogAsync("system", "menu:create", null, null, null, null, 200, "success", ct);
        return id;
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
            // Circular reference check: walk up from proposed parent to root in memory
            var allMenus = await conn.QueryAsync<MenuParent>(new CommandDefinition(
                "SELECT id, parent_id FROM sys_menu WHERE deleted_at IS NULL", cancellationToken: ct));
            var parentById = allMenus.ToDictionary(m => m.Id, m => m.ParentId);
            var visited = new HashSet<long>();
            long? current = req.ParentId;
            while (current.HasValue && current != 0)
            {
                if (!visited.Add(current.Value)) break;
                if (current.Value == id)
                    throw new InvalidOperationException("Cannot create circular menu reference");
                parentById.TryGetValue(current.Value, out current);
            }
        }
        // COALESCE allows partial updates: passing null preserves the existing column value
        await conn.ExecuteAsync(new CommandDefinition(
            "UPDATE sys_menu SET title=COALESCE(@T,title), path=COALESCE(@P,path), component=COALESCE(@C,component), icon=COALESCE(@I,icon), sort=COALESCE(@S,sort), hidden=COALESCE(@H,hidden), parent_id=COALESCE(@Pid,parent_id), updated_at=@Now WHERE id=@Id",
            new { req.Title, req.Path, req.Component, req.Icon, S = req.Sort, H = req.Hidden, Pid = req.ParentId, Now = clock.UtcNow.DateTime, Id = id }, cancellationToken: ct));
    }

    public async Task DeleteAsync(long id, CancellationToken ct)
    {
        await using var conn = await db.OpenAsync(ct);
        // Query all non-deleted menus once
        var allMenus = await conn.QueryAsync<MenuParent>(new CommandDefinition(
            "SELECT id, parent_id FROM sys_menu WHERE deleted_at IS NULL", cancellationToken: ct));
        // Build parent->children lookup in memory
        var childrenByParent = allMenus.ToLookup(m => m.ParentId);
        // Collect all descendant IDs via BFS
        var ids = new List<long> { id };
        var queue = new Queue<long>(new[] { id });
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            foreach (var child in childrenByParent[current])
            {
                ids.Add(child.Id);
                queue.Enqueue(child.Id);
            }
        }
        // Soft-delete all collected IDs in one batch
        await conn.ExecuteAsync(new CommandDefinition(
            "UPDATE sys_menu SET deleted_at=@Now WHERE id IN @Ids",
            new { Now = clock.UtcNow.DateTime, Ids = ids }, cancellationToken: ct));
        await audit.LogAsync("system", "menu:delete", null, null, null, null, 200, "success", ct);
    }

    public async Task SortAsync(long[] orderedIds, CancellationToken ct)
    {
        // Deduplicate: keep first occurrence order, prevent duplicate CASE WHEN branches
        var uniqueIds = orderedIds.Distinct().ToArray();
        if (uniqueIds.Length == 0) return;
        await using var conn = await db.OpenAsync(ct);
        // H7: Batch update with CASE WHEN instead of N individual UPDATEs
        var cases = new List<string>(uniqueIds.Length);
        var parameters = new DynamicParameters();
        for (var i = 0; i < uniqueIds.Length; i++)
        {
            cases.Add($"WHEN @Id{i} THEN @Sort{i}");
            parameters.Add($"Id{i}", uniqueIds[i]);
            parameters.Add($"Sort{i}", i);
        }
        parameters.Add("Ids", uniqueIds);
        var sql = $"UPDATE sys_menu SET sort = CASE id {string.Join(" ", cases)} ELSE sort END WHERE id IN @Ids";
        await conn.ExecuteAsync(new CommandDefinition(sql, parameters, cancellationToken: ct));
        await audit.LogAsync("system", "menu:sort", null, null, null, null, 200, "success", ct);
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
