using SqlSugar;
using WeCms.Modules.Organization.Positions;
using WeCms.Shared;

namespace WeCms.Modules.Organization.SqlSugar.Repositories;

public sealed class PositionRepository : IPositionRepository
{
    private readonly ISqlSugarClient _db;

    public PositionRepository(ISqlSugarClient db) => _db = db;

    public async Task<PagedResult<PositionSummaryDto>> ListAsync(PositionListCriteria criteria, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var where = "WHERE deleted_at IS NULL";
        var parameters = new List<SugarParameter>();
        if (!string.IsNullOrWhiteSpace(criteria.Keyword))
        {
            where += " AND (code LIKE @keyword OR name LIKE @keyword)";
            parameters.Add(new SugarParameter("@keyword", $"%{criteria.Keyword}%"));
        }

        if (!string.IsNullOrWhiteSpace(criteria.Status))
        {
            where += " AND status = @status";
            parameters.Add(new SugarParameter("@status", criteria.Status));
        }

        var total = Convert.ToInt64(await _db.Ado.GetScalarAsync($"SELECT COUNT(1) FROM sys_position {where}", parameters), global::System.Globalization.CultureInfo.InvariantCulture);
        var offset = (criteria.Page - 1) * criteria.PageSize;
        parameters.Add(new SugarParameter("@offset", offset));
        parameters.Add(new SugarParameter("@pageSize", criteria.PageSize));
        var rows = await _db.Ado.SqlQueryAsync<PositionRow>(
            $"""
            SELECT id AS Id,
                   code AS Code,
                   name AS Name,
                   sort_order AS SortOrder,
                   status AS Status,
                   created_at AS CreatedAt,
                   updated_at AS UpdatedAt
            FROM sys_position
            {where}
            ORDER BY sort_order, id
            LIMIT @pageSize OFFSET @offset
            """,
            parameters);

        return new PagedResult<PositionSummaryDto>(rows.Select(row => row.ToSummaryDto()).ToArray(), criteria.Page, criteria.PageSize, total);
    }

    public async Task<PositionDetailDto?> GetAsync(long id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var row = await _db.Ado.SqlQuerySingleAsync<PositionRow>(
            """
            SELECT id AS Id,
                   code AS Code,
                   name AS Name,
                   sort_order AS SortOrder,
                   status AS Status,
                   created_at AS CreatedAt,
                   updated_at AS UpdatedAt
            FROM sys_position
            WHERE id = @id
              AND deleted_at IS NULL
            LIMIT 1
            """,
            new SugarParameter("@id", id));

        return row?.ToDetailDto();
    }

    public async Task<bool> CodeExistsAsync(string code, long? exceptPositionId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var sql = "SELECT COUNT(1) FROM sys_position WHERE code = @code AND deleted_at IS NULL";
        var parameters = new List<SugarParameter> { new("@code", code) };
        if (exceptPositionId is not null)
        {
            sql += " AND id <> @exceptPositionId";
            parameters.Add(new SugarParameter("@exceptPositionId", exceptPositionId.Value));
        }

        return Convert.ToInt32(await _db.Ado.GetScalarAsync(sql, parameters), global::System.Globalization.CultureInfo.InvariantCulture) > 0;
    }

    public async Task<IReadOnlySet<long>> ExistingIdsAsync(IReadOnlyList<long> ids, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (ids.Count == 0)
        {
            return new HashSet<long>();
        }

        var parameters = ids.Select((id, index) => new SugarParameter($"@id{index}", id)).ToArray();
        var placeholders = string.Join(", ", parameters.Select(parameter => parameter.ParameterName));
        var rows = await _db.Ado.SqlQueryAsync<long>(
            $"""
            SELECT id
            FROM sys_position
            WHERE id IN ({placeholders})
              AND deleted_at IS NULL
            """,
            parameters);

        return rows.ToHashSet();
    }

    public async Task<bool> HasUsersAsync(long id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Convert.ToInt32(await _db.Ado.GetScalarAsync("SELECT COUNT(1) FROM sys_user_position WHERE position_id = @id", new SugarParameter("@id", id)), global::System.Globalization.CultureInfo.InvariantCulture) > 0;
    }

    public async Task<long> CreateAsync(PositionCreateRecord record, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await ExpectOneAsync(
            """
            INSERT INTO sys_position (code, name, sort_order, status, created_at, updated_at, deleted_at)
            VALUES (@code, @name, @sortOrder, @status, @createdAt, @updatedAt, NULL)
            """,
            cancellationToken,
            new SugarParameter("@code", record.Code),
            new SugarParameter("@name", record.Name),
            new SugarParameter("@sortOrder", record.SortOrder),
            new SugarParameter("@status", record.Status),
            new SugarParameter("@createdAt", record.Now.UtcDateTime),
            new SugarParameter("@updatedAt", record.Now.UtcDateTime));

        return Convert.ToInt64(
            await _db.Ado.GetScalarAsync(
                "SELECT id FROM sys_position WHERE code = @code AND deleted_at IS NULL LIMIT 1",
                new SugarParameter("@code", record.Code)),
            global::System.Globalization.CultureInfo.InvariantCulture);
    }

    public Task UpdateAsync(PositionUpdateRecord record, CancellationToken cancellationToken) => ExpectOneAsync(
        """
        UPDATE sys_position
        SET name = @name,
            sort_order = @sortOrder,
            status = @status,
            updated_at = @updatedAt
        WHERE id = @id
          AND deleted_at IS NULL
        """,
        cancellationToken,
        new SugarParameter("@name", record.Name),
        new SugarParameter("@sortOrder", record.SortOrder),
        new SugarParameter("@status", record.Status),
        new SugarParameter("@updatedAt", record.Now.UtcDateTime),
        new SugarParameter("@id", record.Id));

    public Task SoftDeleteAsync(long id, DateTimeOffset now, CancellationToken cancellationToken) => ExpectOneAsync(
        """
        UPDATE sys_position
        SET deleted_at = @deletedAt,
            updated_at = @updatedAt
        WHERE id = @id
          AND deleted_at IS NULL
        """,
        cancellationToken,
        new SugarParameter("@deletedAt", now.UtcDateTime),
        new SugarParameter("@updatedAt", now.UtcDateTime),
        new SugarParameter("@id", id));

    public Task SetStatusAsync(long id, string status, DateTimeOffset now, CancellationToken cancellationToken) => ExpectOneAsync(
        """
        UPDATE sys_position
        SET status = @status,
            updated_at = @updatedAt
        WHERE id = @id
          AND deleted_at IS NULL
        """,
        cancellationToken,
        new SugarParameter("@status", status),
        new SugarParameter("@updatedAt", now.UtcDateTime),
        new SugarParameter("@id", id));

    public Task RecordAuditAsync(PositionAuditRecord record, CancellationToken cancellationToken) => ExpectOneAsync(
        """
        INSERT INTO sys_audit_log (user_id, username, module, resource, action, target_id, request_method, request_path, ip_address, user_agent, trace_id, result, detail, created_at)
        VALUES (@userId, @username, 'system', 'position', @action, @targetId, @requestMethod, @requestPath, @ipAddress, @userAgent, @traceId, @result, @detail, @createdAt)
        """,
        cancellationToken,
        new SugarParameter("@userId", record.ActorUserId),
        new SugarParameter("@username", record.ActorUsername),
        new SugarParameter("@action", record.Action),
        new SugarParameter("@targetId", record.TargetPositionId.ToString(global::System.Globalization.CultureInfo.InvariantCulture)),
        new SugarParameter("@requestMethod", string.Empty),
        new SugarParameter("@requestPath", "/api/v1/system/positions"),
        new SugarParameter("@ipAddress", record.Ip),
        new SugarParameter("@userAgent", record.UserAgent),
        new SugarParameter("@traceId", record.TraceId),
        new SugarParameter("@result", record.Result),
        new SugarParameter("@detail", record.Detail),
        new SugarParameter("@createdAt", record.Now.UtcDateTime));

    private async Task ExpectOneAsync(string sql, CancellationToken cancellationToken, params SugarParameter[] parameters)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var rows = await _db.Ado.ExecuteCommandAsync(sql, parameters);
        if (rows != 1)
        {
            throw new InvalidOperationException($"Expected one affected row, got {rows}.");
        }
    }

    private sealed class PositionRow
    {
        public long Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public PositionSummaryDto ToSummaryDto() => new(Id, Code, Name, SortOrder, Status, ToOffset(CreatedAt));
        public PositionDetailDto ToDetailDto() => new(Id, Code, Name, SortOrder, Status, ToOffset(CreatedAt), ToOffset(UpdatedAt));
        private static DateTimeOffset ToOffset(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    }
}
