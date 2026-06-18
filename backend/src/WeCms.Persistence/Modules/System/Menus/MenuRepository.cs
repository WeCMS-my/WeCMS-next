using SqlSugar;
using WeCms.Modules.System.Menus;

namespace WeCms.Persistence.Modules.System.Menus;

public sealed class MenuRepository : IMenuRepository
{
    private readonly ISqlSugarClient _db;

    public MenuRepository(ISqlSugarClient db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<MenuSummaryDto>> ListAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var rows = await _db.Ado.SqlQueryAsync<MenuRow>(
            """
            SELECT id AS Id,
                   parent_id AS ParentId,
                   type AS Type,
                   name AS Code,
                   path AS Path,
                   component AS Component,
                   title AS Title,
                   i18n_key AS I18nKey,
                   icon AS Icon,
                   sort AS Sort,
                   hidden AS Hidden,
                   keep_alive AS KeepAlive,
                   external_url AS ExternalUrl,
                   permission_code AS PermissionCode,
                   status AS Status,
                   is_builtin AS IsBuiltin,
                   created_at AS CreatedAt,
                   updated_at AS UpdatedAt
            FROM sys_menu
            WHERE deleted_at IS NULL
            ORDER BY parent_id IS NOT NULL, parent_id, sort, id
            """);

        return rows.Select(row => row.ToSummaryDto()).ToArray();
    }

    public async Task<MenuDetailDto?> GetAsync(long id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var row = await _db.Ado.SqlQuerySingleAsync<MenuRow>(
            """
            SELECT id AS Id,
                   parent_id AS ParentId,
                   type AS Type,
                   name AS Code,
                   path AS Path,
                   component AS Component,
                   title AS Title,
                   i18n_key AS I18nKey,
                   icon AS Icon,
                   sort AS Sort,
                   hidden AS Hidden,
                   keep_alive AS KeepAlive,
                   external_url AS ExternalUrl,
                   permission_code AS PermissionCode,
                   status AS Status,
                   is_builtin AS IsBuiltin,
                   created_at AS CreatedAt,
                   updated_at AS UpdatedAt
            FROM sys_menu
            WHERE id = @id
              AND deleted_at IS NULL
            LIMIT 1
            """,
            new SugarParameter("@id", id));

        return row?.ToDetailDto();
    }

    public async Task<bool> CodeExistsAsync(string code, long? exceptMenuId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var sql = "SELECT COUNT(1) FROM sys_menu WHERE name = @code AND deleted_at IS NULL";
        var parameters = new List<SugarParameter> { new("@code", code) };
        if (exceptMenuId is not null)
        {
            sql += " AND id <> @exceptMenuId";
            parameters.Add(new SugarParameter("@exceptMenuId", exceptMenuId.Value));
        }

        return Convert.ToInt32(await _db.Ado.GetScalarAsync(sql, parameters), global::System.Globalization.CultureInfo.InvariantCulture) > 0;
    }

    public async Task<bool> ExistsAsync(long id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Convert.ToInt32(await _db.Ado.GetScalarAsync(
            "SELECT COUNT(1) FROM sys_menu WHERE id = @id AND deleted_at IS NULL",
            new SugarParameter("@id", id)), global::System.Globalization.CultureInfo.InvariantCulture) == 1;
    }

    public async Task<bool> HasChildrenAsync(long id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return Convert.ToInt32(await _db.Ado.GetScalarAsync(
            "SELECT COUNT(1) FROM sys_menu WHERE parent_id = @id AND deleted_at IS NULL",
            new SugarParameter("@id", id)), global::System.Globalization.CultureInfo.InvariantCulture) > 0;
    }

    public async Task<bool> IsDescendantAsync(long id, long candidateParentId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var current = candidateParentId;
        while (true)
        {
            if (current == id)
            {
                return true;
            }

            var parent = await _db.Ado.GetScalarAsync(
                "SELECT parent_id FROM sys_menu WHERE id = @id AND deleted_at IS NULL",
                new SugarParameter("@id", current));
            if (parent is null || parent == DBNull.Value)
            {
                return false;
            }

            current = Convert.ToInt64(parent, global::System.Globalization.CultureInfo.InvariantCulture);
        }
    }

    public async Task<long> CreateAsync(MenuCreateRecord record, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await _db.Ado.ExecuteCommandAsync(
            """
            INSERT INTO sys_menu (parent_id, type, name, path, component, title, i18n_key, icon, sort, hidden, keep_alive, external_url, permission_code, status, is_builtin, created_at, updated_at, deleted_at)
            VALUES (@parentId, @type, @code, @path, @component, @title, @i18nKey, @icon, @sort, @hidden, @keepAlive, @externalUrl, @permissionCode, @status, FALSE, @createdAt, @updatedAt, NULL)
            """,
            Parameters(record));

        return Convert.ToInt64(await _db.Ado.GetScalarAsync("SELECT LAST_INSERT_ID()"), global::System.Globalization.CultureInfo.InvariantCulture);
    }

    public Task UpdateAsync(MenuUpdateRecord record, CancellationToken cancellationToken)
    {
        return ExpectOneAsync(
            """
            UPDATE sys_menu
            SET parent_id = @parentId,
                type = @type,
                path = @path,
                component = @component,
                title = @title,
                i18n_key = @i18nKey,
                icon = @icon,
                sort = @sort,
                hidden = @hidden,
                keep_alive = @keepAlive,
                external_url = @externalUrl,
                permission_code = @permissionCode,
                status = @status,
                updated_at = @updatedAt
            WHERE id = @id
              AND deleted_at IS NULL
            """,
            cancellationToken,
            new SugarParameter("@id", record.Id),
            new SugarParameter("@parentId", record.ParentId),
            new SugarParameter("@type", record.Type),
            new SugarParameter("@path", record.Path),
            new SugarParameter("@component", record.Component),
            new SugarParameter("@title", record.Title),
            new SugarParameter("@i18nKey", record.I18nKey),
            new SugarParameter("@icon", record.Icon),
            new SugarParameter("@sort", record.Sort),
            new SugarParameter("@hidden", record.Hidden),
            new SugarParameter("@keepAlive", record.KeepAlive),
            new SugarParameter("@externalUrl", record.ExternalUrl),
            new SugarParameter("@permissionCode", record.PermissionCode),
            new SugarParameter("@status", record.Status),
            new SugarParameter("@updatedAt", record.Now.UtcDateTime));
    }

    public async Task SortAsync(IReadOnlyList<MenuSortRecord> records, CancellationToken cancellationToken)
    {
        foreach (var record in records)
        {
            await ExpectOneAsync(
                """
                UPDATE sys_menu
                SET parent_id = @parentId,
                    sort = @sort,
                    updated_at = @updatedAt
                WHERE id = @id
                  AND deleted_at IS NULL
                """,
                cancellationToken,
                new SugarParameter("@id", record.Id),
                new SugarParameter("@parentId", record.ParentId),
                new SugarParameter("@sort", record.Sort),
                new SugarParameter("@updatedAt", record.Now.UtcDateTime));
        }
    }

    public Task SoftDeleteAsync(long id, DateTimeOffset now, CancellationToken cancellationToken)
    {
        return ExpectOneAsync(
            """
            UPDATE sys_menu
            SET deleted_at = @deletedAt,
                updated_at = @updatedAt
            WHERE id = @id
              AND deleted_at IS NULL
            """,
            cancellationToken,
            new SugarParameter("@deletedAt", now.UtcDateTime),
            new SugarParameter("@updatedAt", now.UtcDateTime),
            new SugarParameter("@id", id));
    }

    public Task SetStatusAsync(long id, string status, DateTimeOffset now, CancellationToken cancellationToken)
    {
        return ExpectOneAsync(
            """
            UPDATE sys_menu
            SET status = @status,
                updated_at = @updatedAt
            WHERE id = @id
              AND deleted_at IS NULL
            """,
            cancellationToken,
            new SugarParameter("@status", status),
            new SugarParameter("@updatedAt", now.UtcDateTime),
            new SugarParameter("@id", id));
    }

    public Task RecordAuditAsync(MenuAuditRecord record, CancellationToken cancellationToken)
    {
        return ExpectOneAsync(
            """
            INSERT INTO sys_audit_log (user_id, username, module, resource, action, target_id, request_method, request_path, ip_address, user_agent, trace_id, result, detail, created_at)
            VALUES (@userId, @username, 'system', 'menu', @action, @targetId, @requestMethod, @requestPath, @ipAddress, @userAgent, @traceId, @result, @detail, @createdAt)
            """,
            cancellationToken,
            new SugarParameter("@userId", record.ActorUserId),
            new SugarParameter("@username", record.ActorUsername),
            new SugarParameter("@action", record.Action),
            new SugarParameter("@targetId", record.TargetMenuId.ToString(global::System.Globalization.CultureInfo.InvariantCulture)),
            new SugarParameter("@requestMethod", string.Empty),
            new SugarParameter("@requestPath", "/api/v1/system/menus"),
            new SugarParameter("@ipAddress", record.Ip),
            new SugarParameter("@userAgent", record.UserAgent),
            new SugarParameter("@traceId", record.TraceId),
            new SugarParameter("@result", record.Result),
            new SugarParameter("@detail", record.Detail),
            new SugarParameter("@createdAt", record.Now.UtcDateTime));
    }

    private static SugarParameter[] Parameters(MenuCreateRecord record)
    {
        return
        [
            new SugarParameter("@parentId", record.ParentId),
            new SugarParameter("@type", record.Type),
            new SugarParameter("@code", record.Code),
            new SugarParameter("@path", record.Path),
            new SugarParameter("@component", record.Component),
            new SugarParameter("@title", record.Title),
            new SugarParameter("@i18nKey", record.I18nKey),
            new SugarParameter("@icon", record.Icon),
            new SugarParameter("@sort", record.Sort),
            new SugarParameter("@hidden", record.Hidden),
            new SugarParameter("@keepAlive", record.KeepAlive),
            new SugarParameter("@externalUrl", record.ExternalUrl),
            new SugarParameter("@permissionCode", record.PermissionCode),
            new SugarParameter("@status", record.Status),
            new SugarParameter("@createdAt", record.Now.UtcDateTime),
            new SugarParameter("@updatedAt", record.Now.UtcDateTime)
        ];
    }

    private async Task ExpectOneAsync(string sql, CancellationToken cancellationToken, params SugarParameter[] parameters)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var rows = await _db.Ado.ExecuteCommandAsync(sql, parameters);
        if (rows != 1)
        {
            throw new InvalidOperationException($"Expected one affected row, got {rows}.");
        }
    }

    private sealed class MenuRow
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
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public MenuSummaryDto ToSummaryDto()
        {
            return new MenuSummaryDto(Id, ParentId, Type, Code, Path, Component, Title, I18nKey, Icon, Sort, Hidden, KeepAlive, ExternalUrl, PermissionCode, Status, IsBuiltin);
        }

        public MenuDetailDto ToDetailDto()
        {
            return new MenuDetailDto(Id, ParentId, Type, Code, Path, Component, Title, I18nKey, Icon, Sort, Hidden, KeepAlive, ExternalUrl, PermissionCode, Status, IsBuiltin, ToOffset(CreatedAt), ToOffset(UpdatedAt));
        }

        private static DateTimeOffset ToOffset(DateTime value)
        {
            return new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc));
        }
    }
}
