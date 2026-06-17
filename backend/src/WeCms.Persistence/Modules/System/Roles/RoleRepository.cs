using SqlSugar;
using WeCms.Modules.System.Roles;
using WeCms.Shared;

namespace WeCms.Persistence.Modules.System.Roles;

public sealed class RoleRepository : IRoleRepository
{
    private readonly ISqlSugarClient _db;

    public RoleRepository(ISqlSugarClient db)
    {
        _db = db;
    }

    public async Task<PagedResult<RoleSummaryDto>> ListAsync(RoleListCriteria criteria, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var where = "WHERE r.deleted_at IS NULL";
        var parameters = new List<SugarParameter>();
        if (!string.IsNullOrWhiteSpace(criteria.Keyword))
        {
            where += " AND (r.code LIKE @keyword OR r.name LIKE @keyword)";
            parameters.Add(new SugarParameter("@keyword", $"%{criteria.Keyword}%"));
        }

        if (!string.IsNullOrWhiteSpace(criteria.Status))
        {
            where += " AND r.status = @status";
            parameters.Add(new SugarParameter("@status", criteria.Status));
        }

        var total = Convert.ToInt64(await _db.Ado.GetScalarAsync(
            $"SELECT COUNT(1) FROM sys_role r {where}",
            parameters), global::System.Globalization.CultureInfo.InvariantCulture);

        var offset = (criteria.Page - 1) * criteria.PageSize;
        parameters.Add(new SugarParameter("@offset", offset));
        parameters.Add(new SugarParameter("@pageSize", criteria.PageSize));

        var rows = await _db.Ado.SqlQueryAsync<RoleSummaryRow>(
            $"""
            SELECT r.id AS Id,
                   r.code AS Code,
                   r.name AS Name,
                   r.status AS Status,
                   r.is_builtin AS IsBuiltin,
                   r.is_locked AS IsLocked,
                   r.created_at AS CreatedAt
            FROM sys_role r
            {where}
            ORDER BY r.id DESC
            LIMIT @pageSize OFFSET @offset
            """,
            parameters);

        return new PagedResult<RoleSummaryDto>(
            rows.Select(row => row.ToDto()).ToArray(),
            criteria.Page,
            criteria.PageSize,
            total);
    }

    public async Task<RoleDetailDto?> GetAsync(long id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var row = await _db.Ado.SqlQuerySingleAsync<RoleDetailRow>(
            """
            SELECT r.id AS Id,
                   r.code AS Code,
                   r.name AS Name,
                   r.status AS Status,
                   r.is_builtin AS IsBuiltin,
                   r.is_locked AS IsLocked,
                   r.created_at AS CreatedAt,
                   r.updated_at AS UpdatedAt
            FROM sys_role r
            WHERE r.id = @id
              AND r.deleted_at IS NULL
            LIMIT 1
            """,
            new SugarParameter("@id", id));

        if (row is null)
        {
            return null;
        }

        var permissionIds = await _db.Ado.SqlQueryAsync<long>(
            """
            SELECT permission_id
            FROM sys_role_permission
            WHERE role_id = @id
            ORDER BY permission_id
            """,
            new SugarParameter("@id", id));
        var menuIds = await _db.Ado.SqlQueryAsync<long>(
            """
            SELECT menu_id
            FROM sys_role_menu
            WHERE role_id = @id
            ORDER BY menu_id
            """,
            new SugarParameter("@id", id));

        return row.ToDto(permissionIds, menuIds);
    }

    public async Task<bool> CodeExistsAsync(string code, long? exceptRoleId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var sql = "SELECT COUNT(1) FROM sys_role WHERE code = @code AND deleted_at IS NULL";
        var parameters = new List<SugarParameter> { new("@code", code) };
        if (exceptRoleId is not null)
        {
            sql += " AND id <> @exceptRoleId";
            parameters.Add(new SugarParameter("@exceptRoleId", exceptRoleId.Value));
        }

        return Convert.ToInt32(await _db.Ado.GetScalarAsync(sql, parameters), global::System.Globalization.CultureInfo.InvariantCulture) > 0;
    }

    public Task<IReadOnlySet<long>> ExistingPermissionIdsAsync(IReadOnlyList<long> permissionIds, CancellationToken cancellationToken)
    {
        return ExistingPermissionIdsInternalAsync(permissionIds, cancellationToken);
    }

    public Task<IReadOnlySet<long>> ExistingMenuIdsAsync(IReadOnlyList<long> menuIds, CancellationToken cancellationToken)
    {
        return ExistingMenuIdsInternalAsync(menuIds, cancellationToken);
    }

    public async Task<long> CreateAsync(RoleCreateRecord record, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await _db.Ado.ExecuteCommandAsync(
            """
            INSERT INTO sys_role (code, name, status, is_builtin, is_locked, created_at, updated_at, deleted_at)
            VALUES (@code, @name, 'enabled', FALSE, FALSE, @createdAt, @updatedAt, NULL)
            """,
            new SugarParameter("@code", record.Code),
            new SugarParameter("@name", record.Name),
            new SugarParameter("@createdAt", record.Now.UtcDateTime),
            new SugarParameter("@updatedAt", record.Now.UtcDateTime));

        return Convert.ToInt64(await _db.Ado.GetScalarAsync("SELECT LAST_INSERT_ID()"), global::System.Globalization.CultureInfo.InvariantCulture);
    }

    public Task UpdateAsync(RoleUpdateRecord record, CancellationToken cancellationToken)
    {
        return ExpectOneAsync(
            """
            UPDATE sys_role
            SET name = @name,
                updated_at = @updatedAt
            WHERE id = @id
              AND deleted_at IS NULL
            """,
            cancellationToken,
            new SugarParameter("@name", record.Name),
            new SugarParameter("@updatedAt", record.Now.UtcDateTime),
            new SugarParameter("@id", record.Id));
    }

    public Task SoftDeleteAsync(long id, DateTimeOffset now, CancellationToken cancellationToken)
    {
        return ExpectOneAsync(
            """
            UPDATE sys_role
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
            UPDATE sys_role
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

    public async Task ReplacePermissionsAsync(long id, IReadOnlyList<long> permissionIds, DateTimeOffset now, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await _db.Ado.ExecuteCommandAsync("DELETE FROM sys_role_permission WHERE role_id = @id", new SugarParameter("@id", id));
        foreach (var permissionId in permissionIds)
        {
            await _db.Ado.ExecuteCommandAsync(
                "INSERT INTO sys_role_permission (role_id, permission_id, created_at) VALUES (@id, @permissionId, @createdAt)",
                new SugarParameter("@id", id),
                new SugarParameter("@permissionId", permissionId),
                new SugarParameter("@createdAt", now.UtcDateTime));
        }

        await BumpPermissionVersionForRoleUsersAsync(id, now);
    }

    public async Task ReplaceMenusAsync(long id, IReadOnlyList<long> menuIds, DateTimeOffset now, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await _db.Ado.ExecuteCommandAsync("DELETE FROM sys_role_menu WHERE role_id = @id", new SugarParameter("@id", id));
        foreach (var menuId in menuIds)
        {
            await _db.Ado.ExecuteCommandAsync(
                "INSERT INTO sys_role_menu (role_id, menu_id, created_at) VALUES (@id, @menuId, @createdAt)",
                new SugarParameter("@id", id),
                new SugarParameter("@menuId", menuId),
                new SugarParameter("@createdAt", now.UtcDateTime));
        }

        await BumpPermissionVersionForRoleUsersAsync(id, now);
    }

    public Task RecordAuditAsync(RoleAuditRecord record, CancellationToken cancellationToken)
    {
        return ExpectOneAsync(
            """
            INSERT INTO sys_audit_log (user_id, username, module, resource, action, target_id, request_method, request_path, ip_address, user_agent, trace_id, result, detail, created_at)
            VALUES (@userId, @username, 'system', 'role', @action, @targetId, @requestMethod, @requestPath, @ipAddress, @userAgent, @traceId, @result, @detail, @createdAt)
            """,
            cancellationToken,
            new SugarParameter("@userId", record.ActorUserId),
            new SugarParameter("@username", record.ActorUsername),
            new SugarParameter("@action", record.Action),
            new SugarParameter("@targetId", record.TargetRoleId.ToString(global::System.Globalization.CultureInfo.InvariantCulture)),
            new SugarParameter("@requestMethod", string.Empty),
            new SugarParameter("@requestPath", "/api/v1/system/roles"),
            new SugarParameter("@ipAddress", record.Ip),
            new SugarParameter("@userAgent", record.UserAgent),
            new SugarParameter("@traceId", record.TraceId),
            new SugarParameter("@result", record.Result),
            new SugarParameter("@detail", record.Detail),
            new SugarParameter("@createdAt", record.Now.UtcDateTime));
    }

    private async Task<IReadOnlySet<long>> ExistingPermissionIdsInternalAsync(IReadOnlyList<long> ids, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (ids.Count == 0)
        {
            return new HashSet<long>();
        }

        var parameters = ids.Select((id, index) => new SugarParameter($"@id{index}", id)).ToArray();
        var placeholders = string.Join(", ", parameters.Select(parameter => parameter.ParameterName));
        var rows = await _db.Ado.SqlQueryAsync<long>($"SELECT id FROM sys_permission WHERE id IN ({placeholders})", parameters);

        return rows.ToHashSet();
    }

    private async Task<IReadOnlySet<long>> ExistingMenuIdsInternalAsync(IReadOnlyList<long> ids, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (ids.Count == 0)
        {
            return new HashSet<long>();
        }

        var parameters = ids.Select((id, index) => new SugarParameter($"@id{index}", id)).ToArray();
        var placeholders = string.Join(", ", parameters.Select(parameter => parameter.ParameterName));
        var rows = await _db.Ado.SqlQueryAsync<long>($"SELECT id FROM sys_menu WHERE id IN ({placeholders})", parameters);

        return rows.ToHashSet();
    }

    private Task BumpPermissionVersionForRoleUsersAsync(long roleId, DateTimeOffset now)
    {
        return _db.Ado.ExecuteCommandAsync(
            """
            UPDATE sys_user u
            INNER JOIN sys_user_role ur ON ur.user_id = u.id
            SET u.permission_version = u.permission_version + 1,
                u.updated_at = @updatedAt
            WHERE ur.role_id = @roleId
              AND u.deleted_at IS NULL
            """,
            new SugarParameter("@updatedAt", now.UtcDateTime),
            new SugarParameter("@roleId", roleId));
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

    private class RoleSummaryRow
    {
        public long Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsBuiltin { get; set; }
        public bool IsLocked { get; set; }
        public DateTime CreatedAt { get; set; }

        public RoleSummaryDto ToDto()
        {
            return new RoleSummaryDto(Id, Code, Name, Status, IsBuiltin, IsLocked, ToOffset(CreatedAt)!.Value);
        }
    }

    private sealed class RoleDetailRow : RoleSummaryRow
    {
        public DateTime UpdatedAt { get; set; }

        public RoleDetailDto ToDto(IReadOnlyList<long> permissionIds, IReadOnlyList<long> menuIds)
        {
            return new RoleDetailDto(
                Id,
                Code,
                Name,
                Status,
                IsBuiltin,
                IsLocked,
                permissionIds,
                menuIds,
                ToOffset(CreatedAt)!.Value,
                ToOffset(UpdatedAt)!.Value);
        }
    }

    private static DateTimeOffset? ToOffset(DateTime? value)
    {
        return value is null ? null : new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc));
    }
}
