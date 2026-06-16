using SqlSugar;
using WeCms.Modules.System.Departments;

namespace WeCms.Persistence.Modules.System.Departments;

public sealed class DepartmentRepository : IDepartmentRepository
{
    private readonly ISqlSugarClient _db;

    public DepartmentRepository(ISqlSugarClient db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<DepartmentSummaryDto>> ListAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var rows = await _db.Ado.SqlQueryAsync<DepartmentRow>(
            """
            SELECT id AS Id,
                   parent_id AS ParentId,
                   code AS Code,
                   name AS Name,
                   sort_order AS SortOrder,
                   status AS Status,
                   created_at AS CreatedAt,
                   updated_at AS UpdatedAt
            FROM sys_dept
            WHERE deleted_at IS NULL
            ORDER BY parent_id IS NOT NULL, parent_id, sort_order, id
            """);

        return rows.Select(row => row.ToSummaryDto()).ToArray();
    }

    public async Task<DepartmentDetailDto?> GetAsync(long id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var row = await _db.Ado.SqlQuerySingleAsync<DepartmentRow>(
            """
            SELECT id AS Id,
                   parent_id AS ParentId,
                   code AS Code,
                   name AS Name,
                   sort_order AS SortOrder,
                   status AS Status,
                   created_at AS CreatedAt,
                   updated_at AS UpdatedAt
            FROM sys_dept
            WHERE id = @id
              AND deleted_at IS NULL
            LIMIT 1
            """,
            new SugarParameter("@id", id));

        return row?.ToDetailDto();
    }

    public async Task<bool> CodeExistsAsync(string code, long? exceptDepartmentId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var sql = "SELECT COUNT(1) FROM sys_dept WHERE code = @code AND deleted_at IS NULL";
        var parameters = new List<SugarParameter> { new("@code", code) };
        if (exceptDepartmentId is not null)
        {
            sql += " AND id <> @exceptDepartmentId";
            parameters.Add(new SugarParameter("@exceptDepartmentId", exceptDepartmentId.Value));
        }

        return Convert.ToInt32(await _db.Ado.GetScalarAsync(sql, parameters), global::System.Globalization.CultureInfo.InvariantCulture) > 0;
    }

    public async Task<bool> ExistsAsync(long id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Convert.ToInt32(await _db.Ado.GetScalarAsync("SELECT COUNT(1) FROM sys_dept WHERE id = @id AND deleted_at IS NULL", new SugarParameter("@id", id)), global::System.Globalization.CultureInfo.InvariantCulture) == 1;
    }

    public async Task<bool> HasChildrenAsync(long id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Convert.ToInt32(await _db.Ado.GetScalarAsync("SELECT COUNT(1) FROM sys_dept WHERE parent_id = @id AND deleted_at IS NULL", new SugarParameter("@id", id)), global::System.Globalization.CultureInfo.InvariantCulture) > 0;
    }

    public async Task<bool> HasUsersAsync(long id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Convert.ToInt32(await _db.Ado.GetScalarAsync("SELECT COUNT(1) FROM sys_user WHERE dept_id = @id AND deleted_at IS NULL", new SugarParameter("@id", id)), global::System.Globalization.CultureInfo.InvariantCulture) > 0;
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

            var parent = await _db.Ado.GetScalarAsync("SELECT parent_id FROM sys_dept WHERE id = @id AND deleted_at IS NULL", new SugarParameter("@id", current));
            if (parent is null || parent == DBNull.Value)
            {
                return false;
            }

            current = Convert.ToInt64(parent, global::System.Globalization.CultureInfo.InvariantCulture);
        }
    }

    public async Task<long> CreateAsync(DepartmentCreateRecord record, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _db.Ado.ExecuteCommandAsync(
            """
            INSERT INTO sys_dept (parent_id, code, name, sort_order, status, created_at, updated_at, deleted_at)
            VALUES (@parentId, @code, @name, @sortOrder, @status, @createdAt, @updatedAt, NULL)
            """,
            new SugarParameter("@parentId", record.ParentId),
            new SugarParameter("@code", record.Code),
            new SugarParameter("@name", record.Name),
            new SugarParameter("@sortOrder", record.SortOrder),
            new SugarParameter("@status", record.Status),
            new SugarParameter("@createdAt", record.Now.UtcDateTime),
            new SugarParameter("@updatedAt", record.Now.UtcDateTime));

        return Convert.ToInt64(await _db.Ado.GetScalarAsync("SELECT LAST_INSERT_ID()"), global::System.Globalization.CultureInfo.InvariantCulture);
    }

    public Task UpdateAsync(DepartmentUpdateRecord record, CancellationToken cancellationToken)
    {
        return ExpectOneAsync(
            """
            UPDATE sys_dept
            SET parent_id = @parentId,
                name = @name,
                sort_order = @sortOrder,
                status = @status,
                updated_at = @updatedAt
            WHERE id = @id
              AND deleted_at IS NULL
            """,
            cancellationToken,
            new SugarParameter("@parentId", record.ParentId),
            new SugarParameter("@name", record.Name),
            new SugarParameter("@sortOrder", record.SortOrder),
            new SugarParameter("@status", record.Status),
            new SugarParameter("@updatedAt", record.Now.UtcDateTime),
            new SugarParameter("@id", record.Id));
    }

    public Task SoftDeleteAsync(long id, DateTimeOffset now, CancellationToken cancellationToken)
    {
        return ExpectOneAsync(
            """
            UPDATE sys_dept
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
            UPDATE sys_dept
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

    public Task RecordAuditAsync(DepartmentAuditRecord record, CancellationToken cancellationToken)
    {
        return ExpectOneAsync(
            """
            INSERT INTO sys_audit_log (user_id, username, module, resource, action, target_id, request_method, request_path, ip_address, user_agent, trace_id, result, detail, created_at)
            VALUES (@userId, @username, 'system', 'department', @action, @targetId, @requestMethod, @requestPath, @ipAddress, @userAgent, @traceId, @result, @detail, @createdAt)
            """,
            cancellationToken,
            new SugarParameter("@userId", record.ActorUserId),
            new SugarParameter("@username", record.ActorUsername),
            new SugarParameter("@action", record.Action),
            new SugarParameter("@targetId", record.TargetDepartmentId.ToString(global::System.Globalization.CultureInfo.InvariantCulture)),
            new SugarParameter("@requestMethod", string.Empty),
            new SugarParameter("@requestPath", "/api/v1/system/depts"),
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

    private sealed class DepartmentRow
    {
        public long Id { get; set; }
        public long? ParentId { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public DepartmentSummaryDto ToSummaryDto() => new(Id, ParentId, Code, Name, SortOrder, Status);
        public DepartmentDetailDto ToDetailDto() => new(Id, ParentId, Code, Name, SortOrder, Status, ToOffset(CreatedAt), ToOffset(UpdatedAt));

        private static DateTimeOffset ToOffset(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    }
}
