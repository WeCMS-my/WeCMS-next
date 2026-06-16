using SqlSugar;
using WeCms.Modules.System.Dicts;
using WeCms.Shared;

namespace WeCms.Persistence.Modules.System.Dicts;

public sealed class DictRepository : IDictRepository
{
    private readonly ISqlSugarClient _db;

    public DictRepository(ISqlSugarClient db) => _db = db;

    public async Task<PagedResult<DictTypeSummaryDto>> ListTypesAsync(DictTypeListCriteria criteria, CancellationToken cancellationToken)
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

        var total = Convert.ToInt64(await _db.Ado.GetScalarAsync($"SELECT COUNT(1) FROM sys_dict_type {where}", parameters), global::System.Globalization.CultureInfo.InvariantCulture);
        var offset = (criteria.Page - 1) * criteria.PageSize;
        parameters.Add(new SugarParameter("@offset", offset));
        parameters.Add(new SugarParameter("@pageSize", criteria.PageSize));
        var rows = await _db.Ado.SqlQueryAsync<DictTypeRow>(
            $"""
            SELECT id AS Id,
                   code AS Code,
                   name AS Name,
                   description AS Description,
                   is_system AS IsSystem,
                   status AS Status,
                   sort_order AS SortOrder,
                   created_at AS CreatedAt,
                   updated_at AS UpdatedAt
            FROM sys_dict_type
            {where}
            ORDER BY sort_order, id
            LIMIT @pageSize OFFSET @offset
            """,
            parameters);

        return new PagedResult<DictTypeSummaryDto>(rows.Select(row => row.ToSummaryDto()).ToArray(), criteria.Page, criteria.PageSize, total);
    }

    public async Task<DictTypeDetailDto?> GetTypeAsync(long id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return (await QueryTypeAsync("id = @id", new SugarParameter("@id", id)))?.ToDetailDto();
    }

    public async Task<DictTypeDetailDto?> GetTypeByCodeAsync(string code, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return (await QueryTypeAsync("code = @code", new SugarParameter("@code", code)))?.ToDetailDto();
    }

    public async Task<bool> TypeCodeExistsAsync(string code, long? exceptTypeId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var sql = "SELECT COUNT(1) FROM sys_dict_type WHERE code = @code AND deleted_at IS NULL";
        var parameters = new List<SugarParameter> { new("@code", code) };
        if (exceptTypeId is not null)
        {
            sql += " AND id <> @exceptTypeId";
            parameters.Add(new SugarParameter("@exceptTypeId", exceptTypeId.Value));
        }

        return Convert.ToInt32(await _db.Ado.GetScalarAsync(sql, parameters), global::System.Globalization.CultureInfo.InvariantCulture) > 0;
    }

    public async Task<bool> TypeHasValuesAsync(long id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Convert.ToInt32(await _db.Ado.GetScalarAsync("SELECT COUNT(1) FROM sys_dict_value WHERE type_id = @id AND deleted_at IS NULL", new SugarParameter("@id", id)), global::System.Globalization.CultureInfo.InvariantCulture) > 0;
    }

    public async Task<long> CreateTypeAsync(DictTypeCreateRecord record, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _db.Ado.ExecuteCommandAsync(
            """
            INSERT INTO sys_dict_type (code, name, description, is_system, status, sort_order, created_at, updated_at, deleted_at)
            VALUES (@code, @name, @description, FALSE, @status, @sortOrder, @createdAt, @updatedAt, NULL)
            """,
            new SugarParameter("@code", record.Code),
            new SugarParameter("@name", record.Name),
            new SugarParameter("@description", record.Description),
            new SugarParameter("@status", record.Status),
            new SugarParameter("@sortOrder", record.SortOrder),
            new SugarParameter("@createdAt", record.Now.UtcDateTime),
            new SugarParameter("@updatedAt", record.Now.UtcDateTime));

        return Convert.ToInt64(await _db.Ado.GetScalarAsync("SELECT LAST_INSERT_ID()"), global::System.Globalization.CultureInfo.InvariantCulture);
    }

    public Task UpdateTypeAsync(DictTypeUpdateRecord record, CancellationToken cancellationToken) => ExpectOneAsync(
        """
        UPDATE sys_dict_type
        SET name = @name,
            description = @description,
            sort_order = @sortOrder,
            status = @status,
            updated_at = @updatedAt
        WHERE id = @id
          AND deleted_at IS NULL
        """,
        cancellationToken,
        new SugarParameter("@name", record.Name),
        new SugarParameter("@description", record.Description),
        new SugarParameter("@sortOrder", record.SortOrder),
        new SugarParameter("@status", record.Status),
        new SugarParameter("@updatedAt", record.Now.UtcDateTime),
        new SugarParameter("@id", record.Id));

    public Task SoftDeleteTypeAsync(long id, DateTimeOffset now, CancellationToken cancellationToken) => ExpectOneAsync(
        """
        UPDATE sys_dict_type
        SET deleted_at = @deletedAt,
            updated_at = @updatedAt
        WHERE id = @id
          AND deleted_at IS NULL
        """,
        cancellationToken,
        new SugarParameter("@deletedAt", now.UtcDateTime),
        new SugarParameter("@updatedAt", now.UtcDateTime),
        new SugarParameter("@id", id));

    public async Task<IReadOnlyList<DictValueDto>> ListValuesAsync(string typeCode, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var rows = await _db.Ado.SqlQueryAsync<DictValueRow>(
            """
            SELECT v.id AS Id,
                   v.type_id AS TypeId,
                   t.code AS TypeCode,
                   v.label AS Label,
                   v.value AS Value,
                   v.description AS Description,
                   v.sort_order AS SortOrder,
                   v.is_default AS IsDefault,
                   v.status AS Status
            FROM sys_dict_value v
            INNER JOIN sys_dict_type t ON t.id = v.type_id
            WHERE t.code = @typeCode
              AND t.deleted_at IS NULL
              AND v.deleted_at IS NULL
            ORDER BY v.sort_order, v.id
            """,
            new SugarParameter("@typeCode", typeCode));

        return rows.Select(row => row.ToDto()).ToArray();
    }

    public async Task<DictValueDto?> GetValueAsync(long id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var row = await _db.Ado.SqlQuerySingleAsync<DictValueRow>(
            """
            SELECT v.id AS Id,
                   v.type_id AS TypeId,
                   t.code AS TypeCode,
                   v.label AS Label,
                   v.value AS Value,
                   v.description AS Description,
                   v.sort_order AS SortOrder,
                   v.is_default AS IsDefault,
                   v.status AS Status
            FROM sys_dict_value v
            INNER JOIN sys_dict_type t ON t.id = v.type_id
            WHERE v.id = @id
              AND t.deleted_at IS NULL
              AND v.deleted_at IS NULL
            LIMIT 1
            """,
            new SugarParameter("@id", id));

        return row?.ToDto();
    }

    public async Task<bool> ValueExistsAsync(long typeId, string value, long? exceptValueId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var sql = "SELECT COUNT(1) FROM sys_dict_value WHERE type_id = @typeId AND value = @value AND deleted_at IS NULL";
        var parameters = new List<SugarParameter> { new("@typeId", typeId), new("@value", value) };
        if (exceptValueId is not null)
        {
            sql += " AND id <> @exceptValueId";
            parameters.Add(new SugarParameter("@exceptValueId", exceptValueId.Value));
        }

        return Convert.ToInt32(await _db.Ado.GetScalarAsync(sql, parameters), global::System.Globalization.CultureInfo.InvariantCulture) > 0;
    }

    public async Task<long> CreateValueAsync(DictValueCreateRecord record, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _db.Ado.ExecuteCommandAsync(
            """
            INSERT INTO sys_dict_value (type_id, label, value, description, sort_order, status, is_default, created_at, updated_at, deleted_at)
            VALUES (@typeId, @label, @value, @description, @sortOrder, @status, @isDefault, @createdAt, @updatedAt, NULL)
            """,
            new SugarParameter("@typeId", record.TypeId),
            new SugarParameter("@label", record.Label),
            new SugarParameter("@value", record.Value),
            new SugarParameter("@description", record.Description),
            new SugarParameter("@sortOrder", record.SortOrder),
            new SugarParameter("@status", record.Status),
            new SugarParameter("@isDefault", record.IsDefault),
            new SugarParameter("@createdAt", record.Now.UtcDateTime),
            new SugarParameter("@updatedAt", record.Now.UtcDateTime));

        return Convert.ToInt64(await _db.Ado.GetScalarAsync("SELECT LAST_INSERT_ID()"), global::System.Globalization.CultureInfo.InvariantCulture);
    }

    public Task UpdateValueAsync(DictValueUpdateRecord record, CancellationToken cancellationToken) => ExpectOneAsync(
        """
        UPDATE sys_dict_value
        SET label = @label,
            value = @value,
            description = @description,
            sort_order = @sortOrder,
            status = @status,
            is_default = @isDefault,
            updated_at = @updatedAt
        WHERE id = @id
          AND deleted_at IS NULL
        """,
        cancellationToken,
        new SugarParameter("@label", record.Label),
        new SugarParameter("@value", record.Value),
        new SugarParameter("@description", record.Description),
        new SugarParameter("@sortOrder", record.SortOrder),
        new SugarParameter("@status", record.Status),
        new SugarParameter("@isDefault", record.IsDefault),
        new SugarParameter("@updatedAt", record.Now.UtcDateTime),
        new SugarParameter("@id", record.Id));

    public Task SoftDeleteValueAsync(long id, DateTimeOffset now, CancellationToken cancellationToken) => ExpectOneAsync(
        """
        UPDATE sys_dict_value
        SET deleted_at = @deletedAt,
            updated_at = @updatedAt
        WHERE id = @id
          AND deleted_at IS NULL
        """,
        cancellationToken,
        new SugarParameter("@deletedAt", now.UtcDateTime),
        new SugarParameter("@updatedAt", now.UtcDateTime),
        new SugarParameter("@id", id));

    public Task RecordAuditAsync(DictAuditRecord record, CancellationToken cancellationToken) => ExpectOneAsync(
        """
        INSERT INTO sys_audit_log (user_id, username, module, resource, action, target_id, request_method, request_path, ip_address, user_agent, trace_id, result, detail, created_at)
        VALUES (@userId, @username, 'system', @resource, @action, @targetId, @requestMethod, @requestPath, @ipAddress, @userAgent, @traceId, @result, @detail, @createdAt)
        """,
        cancellationToken,
        new SugarParameter("@userId", record.ActorUserId),
        new SugarParameter("@username", record.ActorUsername),
        new SugarParameter("@resource", record.Resource),
        new SugarParameter("@action", record.Action),
        new SugarParameter("@targetId", record.TargetId.ToString(global::System.Globalization.CultureInfo.InvariantCulture)),
        new SugarParameter("@requestMethod", string.Empty),
        new SugarParameter("@requestPath", "/api/v1/system/dicts"),
        new SugarParameter("@ipAddress", record.Ip),
        new SugarParameter("@userAgent", record.UserAgent),
        new SugarParameter("@traceId", record.TraceId),
        new SugarParameter("@result", record.Result),
        new SugarParameter("@detail", record.Detail),
        new SugarParameter("@createdAt", record.Now.UtcDateTime));

    private async Task<DictTypeRow?> QueryTypeAsync(string predicate, SugarParameter parameter)
    {
        return await _db.Ado.SqlQuerySingleAsync<DictTypeRow>(
            $"""
            SELECT id AS Id,
                   code AS Code,
                   name AS Name,
                   description AS Description,
                   is_system AS IsSystem,
                   status AS Status,
                   sort_order AS SortOrder,
                   created_at AS CreatedAt,
                   updated_at AS UpdatedAt
            FROM sys_dict_type
            WHERE {predicate}
              AND deleted_at IS NULL
            LIMIT 1
            """,
            parameter);
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

    private sealed class DictTypeRow
    {
        public long Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsSystem { get; set; }
        public string Status { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public DictTypeSummaryDto ToSummaryDto() => new(Id, Code, Name, Description, IsSystem, Status, SortOrder, ToOffset(CreatedAt));
        public DictTypeDetailDto ToDetailDto() => new(Id, Code, Name, Description, IsSystem, Status, SortOrder, ToOffset(CreatedAt), ToOffset(UpdatedAt));
    }

    private sealed class DictValueRow
    {
        public long Id { get; set; }
        public long TypeId { get; set; }
        public string TypeCode { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int SortOrder { get; set; }
        public bool IsDefault { get; set; }
        public string Status { get; set; } = string.Empty;

        public DictValueDto ToDto() => new(Id, TypeId, TypeCode, Label, Value, Description, SortOrder, IsDefault, Status);
    }

    private static DateTimeOffset ToOffset(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
}
