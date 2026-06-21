using SqlSugar;
using WeCms.Modules.AccessControl.Contracts;
using WeCms.Modules.AccessControl.Records;
using WeCms.Modules.AccessControl.Repositories;

namespace WeCms.Modules.AccessControl.SqlSugar.Repositories;

public sealed class PermissionRepository : IPermissionRepository
{
    private readonly ISqlSugarClient _db;

    public PermissionRepository(ISqlSugarClient db)
    {
        _db = db;
    }

    public async Task<PermissionUserRecord?> FindUserAsync(long userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var row = await _db.Ado.SqlQuerySingleAsync<PermissionUserRow>(
            """
            SELECT id AS Id,
                   status AS Status
            FROM sys_user
            WHERE id = @userId
              AND deleted_at IS NULL
            LIMIT 1
            """,
            new SugarParameter("@userId", userId));

        return row is null ? null : new PermissionUserRecord(row.Id, row.Status);
    }

    public async Task<bool> UserHasPermissionAsync(long userId, string permissionCode, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var count = await _db.Ado.SqlQuerySingleAsync<int>(
            """
            SELECT COUNT(1)
            FROM sys_permission p
            INNER JOIN sys_role_permission rp ON rp.permission_id = p.id
            INNER JOIN sys_user_role ur ON ur.role_id = rp.role_id
            INNER JOIN sys_role r ON r.id = ur.role_id
            WHERE ur.user_id = @userId
              AND p.code = @permissionCode
              AND p.status = 'enabled'
              AND p.deleted_at IS NULL
              AND r.status = 'enabled'
              AND r.deleted_at IS NULL
            """,
            new SugarParameter("@userId", userId),
            new SugarParameter("@permissionCode", permissionCode));

        return count > 0;
    }

    public async Task<IReadOnlyList<PermissionSummaryDto>> ListManagementAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var rows = await _db.Ado.SqlQueryAsync<PermissionRow>(
            """
            SELECT p.id AS Id,
                   p.code AS Code,
                   p.name AS Name,
                   p.module AS Module,
                   p.description AS Description,
                   p.status AS Status,
                   p.is_builtin AS IsBuiltin,
                   CASE WHEN EXISTS (
                       SELECT 1
                       FROM sys_role_permission rp
                       WHERE rp.permission_id = p.id
                   ) THEN TRUE ELSE FALSE END AS IsRoleBound,
                   p.created_at AS CreatedAt,
                   p.updated_at AS UpdatedAt
            FROM sys_permission p
            WHERE p.deleted_at IS NULL
            ORDER BY p.module, p.code
            """);

        return rows.Select(row => row.ToSummaryDto()).ToArray();
    }

    public async Task<PermissionDetailDto?> GetManagementAsync(long id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var row = await _db.Ado.SqlQuerySingleAsync<PermissionRow>(
            """
            SELECT p.id AS Id,
                   p.code AS Code,
                   p.name AS Name,
                   p.module AS Module,
                   p.description AS Description,
                   p.status AS Status,
                   p.is_builtin AS IsBuiltin,
                   CASE WHEN EXISTS (
                       SELECT 1
                       FROM sys_role_permission rp
                       WHERE rp.permission_id = p.id
                   ) THEN TRUE ELSE FALSE END AS IsRoleBound,
                   p.created_at AS CreatedAt,
                   p.updated_at AS UpdatedAt
            FROM sys_permission p
            WHERE p.id = @id
              AND p.deleted_at IS NULL
            LIMIT 1
            """,
            new SugarParameter("@id", id));

        return row?.ToDetailDto();
    }

    public async Task<bool> CodeExistsAsync(string code, long? exceptPermissionId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var sql = "SELECT COUNT(1) FROM sys_permission WHERE code = @code AND deleted_at IS NULL";
        var parameters = new List<SugarParameter> { new("@code", code) };
        if (exceptPermissionId is not null)
        {
            sql += " AND id <> @exceptPermissionId";
            parameters.Add(new SugarParameter("@exceptPermissionId", exceptPermissionId.Value));
        }

        return Convert.ToInt32(await _db.Ado.GetScalarAsync(sql, parameters), global::System.Globalization.CultureInfo.InvariantCulture) > 0;
    }

    public async Task<long> CreateManagementAsync(PermissionCreateRecord record, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await ExpectOneAsync(
            """
            INSERT INTO sys_permission (code, name, module, description, status, is_builtin, created_at, updated_at, deleted_at)
            VALUES (@code, @name, @module, @description, 'enabled', FALSE, @createdAt, @updatedAt, NULL)
            """,
            cancellationToken,
            new SugarParameter("@code", record.Code),
            new SugarParameter("@name", record.Name),
            new SugarParameter("@module", record.Module),
            new SugarParameter("@description", record.Description),
            new SugarParameter("@createdAt", record.Now.UtcDateTime),
            new SugarParameter("@updatedAt", record.Now.UtcDateTime));

        return Convert.ToInt64(await _db.Ado.GetScalarAsync("SELECT LAST_INSERT_ID()"), global::System.Globalization.CultureInfo.InvariantCulture);
    }

    public Task UpdateManagementAsync(PermissionUpdateRecord record, CancellationToken cancellationToken)
    {
        return ExpectOneAsync(
            """
            UPDATE sys_permission
            SET name = @name,
                module = @module,
                description = @description,
                updated_at = @updatedAt
            WHERE id = @id
              AND deleted_at IS NULL
            """,
            cancellationToken,
            new SugarParameter("@name", record.Name),
            new SugarParameter("@module", record.Module),
            new SugarParameter("@description", record.Description),
            new SugarParameter("@updatedAt", record.Now.UtcDateTime),
            new SugarParameter("@id", record.Id));
    }

    public Task SoftDeleteManagementAsync(long id, DateTimeOffset now, CancellationToken cancellationToken)
    {
        return ExpectOneAsync(
            """
            UPDATE sys_permission
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

    public Task SetManagementStatusAsync(long id, string status, DateTimeOffset now, CancellationToken cancellationToken)
    {
        return ExpectOneAsync(
            """
            UPDATE sys_permission
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

    public Task RecordManagementAuditAsync(PermissionAuditRecord record, CancellationToken cancellationToken)
    {
        return ExpectOneAsync(
            """
            INSERT INTO sys_audit_log (user_id, username, module, resource, action, target_id, request_method, request_path, ip_address, user_agent, trace_id, result, detail, created_at)
            VALUES (@userId, @username, 'system', 'permission', @action, @targetId, @requestMethod, @requestPath, @ipAddress, @userAgent, @traceId, @result, @detail, @createdAt)
            """,
            cancellationToken,
            new SugarParameter("@userId", record.ActorUserId),
            new SugarParameter("@username", record.ActorUsername),
            new SugarParameter("@action", record.Action),
            new SugarParameter("@targetId", record.TargetPermissionId.ToString(global::System.Globalization.CultureInfo.InvariantCulture)),
            new SugarParameter("@requestMethod", string.Empty),
            new SugarParameter("@requestPath", "/api/v1/system/permissions"),
            new SugarParameter("@ipAddress", record.Ip),
            new SugarParameter("@userAgent", record.UserAgent),
            new SugarParameter("@traceId", record.TraceId),
            new SugarParameter("@result", record.Result),
            new SugarParameter("@detail", record.Detail),
            new SugarParameter("@createdAt", record.Now.UtcDateTime));
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

    private sealed class PermissionUserRow
    {
        public long Id { get; set; }

        public string Status { get; set; } = string.Empty;
    }

    private sealed class PermissionRow
    {
        public long Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsBuiltin { get; set; }
        public bool IsRoleBound { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public PermissionSummaryDto ToSummaryDto()
        {
            return new PermissionSummaryDto(Id, Code, Name, Module, Description, Status, IsBuiltin, IsRoleBound);
        }

        public PermissionDetailDto ToDetailDto()
        {
            return new PermissionDetailDto(Id, Code, Name, Module, Description, Status, IsBuiltin, IsRoleBound, ToOffset(CreatedAt), ToOffset(UpdatedAt));
        }

        private static DateTimeOffset ToOffset(DateTime value)
        {
            return new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc));
        }
    }
}
