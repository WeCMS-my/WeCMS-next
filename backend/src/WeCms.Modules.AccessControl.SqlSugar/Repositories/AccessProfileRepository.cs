using SqlSugar;
using WeCms.Modules.AccessControl.Contracts;
using WeCms.Modules.AccessControl.Repositories;

namespace WeCms.Modules.AccessControl.SqlSugar.Repositories;

public sealed class AccessProfileRepository : IAccessProfileRepository
{
    private readonly ISqlSugarClient _db;

    public AccessProfileRepository(ISqlSugarClient db)
    {
        _db = db;
    }

    public async Task<long> GetPermissionVersionAsync(long userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var value = await _db.Ado.GetScalarAsync(
            """
            SELECT permission_version
            FROM sys_user
            WHERE id = @userId
              AND deleted_at IS NULL
            LIMIT 1
            """,
            new SugarParameter("@userId", userId));

        if (value is null || value == DBNull.Value)
        {
            throw new InvalidOperationException("Access profile user was not found.");
        }

        return Convert.ToInt64(value, global::System.Globalization.CultureInfo.InvariantCulture);
    }

    public async Task<IReadOnlyList<string>> ListRoleCodesAsync(long userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _db.Ado.SqlQueryAsync<string>(
            """
            SELECT r.code
            FROM sys_role r
            INNER JOIN sys_user_role ur ON ur.role_id = r.id
            WHERE ur.user_id = @userId
              AND r.status = 'enabled'
            ORDER BY r.code
            """,
            new SugarParameter("@userId", userId));
    }

    public async Task<IReadOnlyList<string>> ListPermissionCodesAsync(long userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _db.Ado.SqlQueryAsync<string>(
            """
            SELECT DISTINCT p.code
            FROM sys_permission p
            INNER JOIN sys_role_permission rp ON rp.permission_id = p.id
            INNER JOIN sys_user_role ur ON ur.role_id = rp.role_id
            INNER JOIN sys_role r ON r.id = ur.role_id
            WHERE ur.user_id = @userId
              AND r.status = 'enabled'
            ORDER BY p.code
            """,
            new SugarParameter("@userId", userId));
    }

    public async Task<IReadOnlyList<MenuSummaryDto>> ListVisibleMenusAsync(long userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var rows = await ListVisibleMenuRowsForUserAsync(userId);

        return rows.Select(row => row.ToSummaryDto()).ToArray();
    }

    private async Task<IReadOnlyList<AccessProfileMenuRow>> ListAllVisibleMenuRowsAsync()
    {
        return await _db.Ado.SqlQueryAsync<AccessProfileMenuRow>(
            """
            SELECT m.id AS Id,
                   m.parent_id AS ParentId,
                   m.type AS Type,
                   m.name AS Code,
                   m.path AS Path,
                   m.component AS Component,
                   m.title AS Title,
                   m.i18n_key AS I18nKey,
                   m.icon AS Icon,
                   m.sort AS Sort,
                   m.hidden AS Hidden,
                   m.keep_alive AS KeepAlive,
                   m.external_url AS ExternalUrl,
                   m.permission_code AS PermissionCode,
                   m.status AS Status,
                   m.is_builtin AS IsBuiltin
            FROM sys_menu m
            WHERE m.deleted_at IS NULL
              AND m.status = 'enabled'
            ORDER BY m.parent_id IS NOT NULL, m.parent_id, m.sort, m.id
            """);
    }

    private async Task<IReadOnlyList<AccessProfileMenuRow>> ListVisibleMenuRowsForUserAsync(long userId)
    {
        var allMenus = await ListAllVisibleMenuRowsAsync();
        var directMenuIds = await _db.Ado.SqlQueryAsync<long>(
            """
            SELECT DISTINCT m.id
            FROM sys_menu m
            INNER JOIN sys_role_menu rm ON rm.menu_id = m.id
            INNER JOIN sys_user_role ur ON ur.role_id = rm.role_id
            INNER JOIN sys_role r ON r.id = ur.role_id
            WHERE ur.user_id = @userId
              AND r.deleted_at IS NULL
              AND r.status = 'enabled'
              AND m.deleted_at IS NULL
              AND m.status = 'enabled'
            """,
            new SugarParameter("@userId", userId));

        var menusById = allMenus.ToDictionary(menu => menu.Id);
        var visibleMenuIds = new HashSet<long>(directMenuIds);
        foreach (var directMenuId in directMenuIds)
        {
            var currentId = directMenuId;
            while (menusById.TryGetValue(currentId, out var menu) && menu.ParentId is { } parentId)
            {
                if (!visibleMenuIds.Add(parentId))
                {
                    break;
                }

                currentId = parentId;
            }
        }

        return allMenus
            .Where(menu => visibleMenuIds.Contains(menu.Id))
            .OrderBy(menu => menu.ParentId.HasValue)
            .ThenBy(menu => menu.ParentId)
            .ThenBy(menu => menu.Sort)
            .ThenBy(menu => menu.Id)
            .ToArray();
    }

    private sealed class AccessProfileMenuRow
    {
        public long Id { get; set; }
        public long? ParentId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string? Component { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? I18nKey { get; set; }
        public string? Icon { get; set; }
        public int Sort { get; set; }
        public bool Hidden { get; set; }
        public bool KeepAlive { get; set; }
        public string? ExternalUrl { get; set; }
        public string? PermissionCode { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsBuiltin { get; set; }

        public MenuSummaryDto ToSummaryDto()
        {
            return new MenuSummaryDto(Id, ParentId, Type, Code, Path, Component, Title, I18nKey, Icon, Sort, Hidden, KeepAlive, ExternalUrl, PermissionCode, Status, IsBuiltin);
        }
    }
}
