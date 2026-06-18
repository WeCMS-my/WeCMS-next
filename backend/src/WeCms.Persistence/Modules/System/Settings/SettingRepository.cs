using SqlSugar;
using WeCms.Modules.System.Settings;
using WeCms.Shared;
using WeCms.Shared.Security;

namespace WeCms.Persistence.Modules.System.Settings;

public sealed class SettingRepository : ISettingRepository
{
    private readonly ISqlSugarClient _db;
    private readonly ISecurityEventClassifier _securityEventClassifier;

    public SettingRepository(ISqlSugarClient db, ISecurityEventClassifier securityEventClassifier)
    {
        _db = db;
        _securityEventClassifier = securityEventClassifier;
    }

    public async Task<PagedResult<SettingSummaryDto>> ListAsync(SettingListCriteria criteria, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var where = "WHERE 1 = 1";
        var parameters = new List<SugarParameter>();
        if (!string.IsNullOrWhiteSpace(criteria.Keyword))
        {
            where += " AND (`key` LIKE @keyword OR name LIKE @keyword)";
            parameters.Add(new SugarParameter("@keyword", $"%{criteria.Keyword}%"));
        }

        if (!string.IsNullOrWhiteSpace(criteria.GroupCode))
        {
            where += " AND group_code = @groupCode";
            parameters.Add(new SugarParameter("@groupCode", criteria.GroupCode));
        }

        var total = Convert.ToInt64(await _db.Ado.GetScalarAsync($"SELECT COUNT(1) FROM sys_setting {where}", parameters), global::System.Globalization.CultureInfo.InvariantCulture);
        var offset = (criteria.Page - 1) * criteria.PageSize;
        parameters.Add(new SugarParameter("@offset", offset));
        parameters.Add(new SugarParameter("@pageSize", criteria.PageSize));
        var rows = await _db.Ado.SqlQueryAsync<SettingRow>(
            $"""
            SELECT `key` AS `Key`,
                   `value` AS Value,
                   value_type AS ValueType,
                   group_code AS GroupCode,
                   name AS Name,
                   description AS Description,
                   is_sensitive AS IsSensitive,
                   is_system AS IsSystem,
                   updated_at AS UpdatedAt,
                   updated_by AS UpdatedBy
            FROM sys_setting
            {where}
            ORDER BY group_code, `key`
            LIMIT @pageSize OFFSET @offset
            """,
            parameters);

        return new PagedResult<SettingSummaryDto>(rows.Select(row => row.ToSummaryDto()).ToArray(), criteria.Page, criteria.PageSize, total);
    }

    public async Task<SettingDetailDto?> GetAsync(string key, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var row = await _db.Ado.SqlQuerySingleAsync<SettingRow>(
            """
            SELECT `key` AS `Key`,
                   `value` AS Value,
                   value_type AS ValueType,
                   group_code AS GroupCode,
                   name AS Name,
                   description AS Description,
                   is_sensitive AS IsSensitive,
                   is_system AS IsSystem,
                   updated_at AS UpdatedAt,
                   updated_by AS UpdatedBy
            FROM sys_setting
            WHERE `key` = @key
            LIMIT 1
            """,
            new SugarParameter("@key", key));

        return row?.ToDetailDto();
    }

    public Task UpdateAsync(SettingUpdateRecord record, CancellationToken cancellationToken) => ExpectOneAsync(
        """
        UPDATE sys_setting
        SET `value` = @value,
            updated_at = @updatedAt,
            updated_by = @updatedBy
        WHERE `key` = @key
        """,
        cancellationToken,
        new SugarParameter("@value", record.Value),
        new SugarParameter("@updatedAt", record.Now.UtcDateTime),
        new SugarParameter("@updatedBy", record.UpdatedBy),
        new SugarParameter("@key", record.Key));

    public Task RecordAuditAsync(SettingAuditRecord record, CancellationToken cancellationToken) => ExpectOneAsync(
        """
        INSERT INTO sys_audit_log (user_id, username, module, resource, action, target_id, request_method, request_path, ip_address, user_agent, trace_id, result, detail, created_at)
        VALUES (@userId, @username, 'system', 'setting', @action, @targetId, @requestMethod, @requestPath, @ipAddress, @userAgent, @traceId, @result, @detail, @createdAt)
        """,
        cancellationToken,
        new SugarParameter("@userId", record.ActorUserId),
        new SugarParameter("@username", record.ActorUsername),
        new SugarParameter("@action", record.Action),
        new SugarParameter("@targetId", record.TargetKey),
        new SugarParameter("@requestMethod", string.Empty),
        new SugarParameter("@requestPath", "/api/v1/system/settings"),
        new SugarParameter("@ipAddress", record.Ip),
        new SugarParameter("@userAgent", record.UserAgent),
        new SugarParameter("@traceId", record.TraceId),
        new SugarParameter("@result", record.Result),
        new SugarParameter("@detail", record.Detail),
        new SugarParameter("@createdAt", record.Now.UtcDateTime));

    public Task RecordSecurityEventAsync(SettingSecurityEventRecord record, CancellationToken cancellationToken)
    {
        var classification = _securityEventClassifier.Classify(record.EventType, record.TraceId);
        return ExpectOneAsync(
            """
            INSERT INTO sys_security_event (event_type, user_id, username, ip, severity, source, message, trace_id, created_at)
            VALUES (@eventType, @userId, @username, @ip, @severity, @source, @message, @traceId, @createdAt)
            """,
            cancellationToken,
            new SugarParameter("@eventType", classification.EventType),
            new SugarParameter("@userId", record.UserId),
            new SugarParameter("@username", record.Username),
            new SugarParameter("@ip", record.Ip),
            new SugarParameter("@severity", classification.Severity),
            new SugarParameter("@source", classification.Source),
            new SugarParameter("@message", record.Message),
            new SugarParameter("@traceId", classification.TraceId),
            new SugarParameter("@createdAt", record.CreatedAt.UtcDateTime));
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

    private sealed class SettingRow
    {
        public string Key { get; set; } = string.Empty;
        public string? Value { get; set; }
        public string ValueType { get; set; } = string.Empty;
        public string GroupCode { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public bool IsSensitive { get; set; }
        public bool IsSystem { get; set; }
        public DateTime UpdatedAt { get; set; }
        public long? UpdatedBy { get; set; }

        public SettingSummaryDto ToSummaryDto() => new(Key, Value, ValueType, GroupCode, Name, Description, IsSensitive, IsSystem, ToOffset(UpdatedAt), UpdatedBy);
        public SettingDetailDto ToDetailDto() => new(Key, Value, ValueType, GroupCode, Name, Description, IsSensitive, IsSystem, ToOffset(UpdatedAt), UpdatedBy);
        private static DateTimeOffset ToOffset(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    }
}
