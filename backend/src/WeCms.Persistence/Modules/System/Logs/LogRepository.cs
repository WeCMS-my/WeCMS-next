using SqlSugar;
using WeCms.Modules.System.Logs;
using WeCms.Shared;

namespace WeCms.Persistence.Modules.System.Logs;

public sealed class LogRepository : ILogRepository
{
    private readonly ISqlSugarClient _db;

    public LogRepository(ISqlSugarClient db) => _db = db;

    public async Task<PagedResult<LoginLogSummaryDto>> ListLoginLogsAsync(LoginLogListCriteria criteria, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var where = "WHERE 1 = 1";
        var parameters = new List<SugarParameter>();
        if (!string.IsNullOrWhiteSpace(criteria.Username))
        {
            where += " AND username LIKE @username";
            parameters.Add(new SugarParameter("@username", $"%{criteria.Username}%"));
        }

        if (!string.IsNullOrWhiteSpace(criteria.Ip))
        {
            where += " AND ip = @ip";
            parameters.Add(new SugarParameter("@ip", criteria.Ip));
        }

        if (!string.IsNullOrWhiteSpace(criteria.Result))
        {
            where += " AND result = @result";
            parameters.Add(new SugarParameter("@result", criteria.Result));
        }

        if (criteria.From is not null)
        {
            where += " AND created_at >= @from";
            parameters.Add(new SugarParameter("@from", criteria.From.Value.UtcDateTime));
        }

        if (criteria.To is not null)
        {
            where += " AND created_at <= @to";
            parameters.Add(new SugarParameter("@to", criteria.To.Value.UtcDateTime));
        }

        var total = Convert.ToInt64(await _db.Ado.GetScalarAsync($"SELECT COUNT(1) FROM sys_login_log {where}", parameters), global::System.Globalization.CultureInfo.InvariantCulture);
        var offset = (criteria.Page - 1) * criteria.PageSize;
        parameters.Add(new SugarParameter("@offset", offset));
        parameters.Add(new SugarParameter("@pageSize", criteria.PageSize));
        var rows = await _db.Ado.SqlQueryAsync<LoginLogRow>(
            $"""
            SELECT id AS Id,
                   username AS Username,
                   user_id AS UserId,
                   ip AS Ip,
                   user_agent AS UserAgent,
                   result AS Result,
                   reason AS Reason,
                   created_at AS CreatedAt
            FROM sys_login_log
            {where}
            ORDER BY created_at DESC, id DESC
            LIMIT @pageSize OFFSET @offset
            """,
            parameters);

        return new PagedResult<LoginLogSummaryDto>(rows.Select(row => row.ToSummaryDto()).ToArray(), criteria.Page, criteria.PageSize, total);
    }

    public async Task<LoginLogDetailDto?> GetLoginLogAsync(long id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var row = await _db.Ado.SqlQuerySingleAsync<LoginLogRow>(
            """
            SELECT id AS Id,
                   username AS Username,
                   user_id AS UserId,
                   ip AS Ip,
                   user_agent AS UserAgent,
                   result AS Result,
                   reason AS Reason,
                   created_at AS CreatedAt
            FROM sys_login_log
            WHERE id = @id
            LIMIT 1
            """,
            new SugarParameter("@id", id));

        return row?.ToDetailDto();
    }

    public async Task<PagedResult<AuditLogSummaryDto>> ListAuditLogsAsync(AuditLogListCriteria criteria, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var where = "WHERE 1 = 1";
        var parameters = new List<SugarParameter>();
        if (!string.IsNullOrWhiteSpace(criteria.User))
        {
            where += " AND (username LIKE @user OR CAST(user_id AS CHAR) = @userExact)";
            parameters.Add(new SugarParameter("@user", $"%{criteria.User}%"));
            parameters.Add(new SugarParameter("@userExact", criteria.User));
        }

        if (!string.IsNullOrWhiteSpace(criteria.Module))
        {
            where += " AND module = @module";
            parameters.Add(new SugarParameter("@module", criteria.Module));
        }

        if (!string.IsNullOrWhiteSpace(criteria.Resource))
        {
            where += " AND resource = @resource";
            parameters.Add(new SugarParameter("@resource", criteria.Resource));
        }

        if (!string.IsNullOrWhiteSpace(criteria.Action))
        {
            where += " AND action = @action";
            parameters.Add(new SugarParameter("@action", criteria.Action));
        }

        if (!string.IsNullOrWhiteSpace(criteria.Result))
        {
            where += " AND result = @result";
            parameters.Add(new SugarParameter("@result", criteria.Result));
        }

        if (criteria.From is not null)
        {
            where += " AND created_at >= @from";
            parameters.Add(new SugarParameter("@from", criteria.From.Value.UtcDateTime));
        }

        if (criteria.To is not null)
        {
            where += " AND created_at <= @to";
            parameters.Add(new SugarParameter("@to", criteria.To.Value.UtcDateTime));
        }

        var total = Convert.ToInt64(await _db.Ado.GetScalarAsync($"SELECT COUNT(1) FROM sys_audit_log {where}", parameters), global::System.Globalization.CultureInfo.InvariantCulture);
        var offset = (criteria.Page - 1) * criteria.PageSize;
        parameters.Add(new SugarParameter("@offset", offset));
        parameters.Add(new SugarParameter("@pageSize", criteria.PageSize));
        var rows = await _db.Ado.SqlQueryAsync<AuditLogRow>(
            $"""
            SELECT id AS Id,
                   user_id AS UserId,
                   username AS Username,
                   module AS Module,
                   resource AS Resource,
                   action AS Action,
                   target_id AS TargetId,
                   request_method AS RequestMethod,
                   request_path AS RequestPath,
                   ip_address AS IpAddress,
                   user_agent AS UserAgent,
                   trace_id AS TraceId,
                   result AS Result,
                   detail AS Detail,
                   created_at AS CreatedAt
            FROM sys_audit_log
            {where}
            ORDER BY created_at DESC, id DESC
            LIMIT @pageSize OFFSET @offset
            """,
            parameters);

        return new PagedResult<AuditLogSummaryDto>(rows.Select(row => row.ToSummaryDto()).ToArray(), criteria.Page, criteria.PageSize, total);
    }

    public async Task<AuditLogDetailDto?> GetAuditLogAsync(long id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var row = await _db.Ado.SqlQuerySingleAsync<AuditLogRow>(
            """
            SELECT id AS Id,
                   user_id AS UserId,
                   username AS Username,
                   module AS Module,
                   resource AS Resource,
                   action AS Action,
                   target_id AS TargetId,
                   request_method AS RequestMethod,
                   request_path AS RequestPath,
                   ip_address AS IpAddress,
                   user_agent AS UserAgent,
                   trace_id AS TraceId,
                   result AS Result,
                   detail AS Detail,
                   created_at AS CreatedAt
            FROM sys_audit_log
            WHERE id = @id
            LIMIT 1
            """,
            new SugarParameter("@id", id));

        return row?.ToDetailDto();
    }

    public async Task<PagedResult<SecurityEventSummaryDto>> ListSecurityEventsAsync(SecurityEventListCriteria criteria, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var where = "WHERE 1 = 1";
        var parameters = new List<SugarParameter>();
        if (!string.IsNullOrWhiteSpace(criteria.EventType))
        {
            where += " AND event_type = @eventType";
            parameters.Add(new SugarParameter("@eventType", criteria.EventType));
        }

        if (!string.IsNullOrWhiteSpace(criteria.Severity))
        {
            where += " AND severity = @severity";
            parameters.Add(new SugarParameter("@severity", criteria.Severity));
        }

        if (!string.IsNullOrWhiteSpace(criteria.User))
        {
            where += " AND (username LIKE @user OR CAST(user_id AS CHAR) = @userExact)";
            parameters.Add(new SugarParameter("@user", $"%{criteria.User}%"));
            parameters.Add(new SugarParameter("@userExact", criteria.User));
        }

        if (!string.IsNullOrWhiteSpace(criteria.Ip))
        {
            where += " AND ip = @ip";
            parameters.Add(new SugarParameter("@ip", criteria.Ip));
        }

        if (criteria.From is not null)
        {
            where += " AND created_at >= @from";
            parameters.Add(new SugarParameter("@from", criteria.From.Value.UtcDateTime));
        }

        if (criteria.To is not null)
        {
            where += " AND created_at <= @to";
            parameters.Add(new SugarParameter("@to", criteria.To.Value.UtcDateTime));
        }

        var total = Convert.ToInt64(await _db.Ado.GetScalarAsync($"SELECT COUNT(1) FROM sys_security_event {where}", parameters), global::System.Globalization.CultureInfo.InvariantCulture);
        var offset = (criteria.Page - 1) * criteria.PageSize;
        parameters.Add(new SugarParameter("@offset", offset));
        parameters.Add(new SugarParameter("@pageSize", criteria.PageSize));
        var rows = await _db.Ado.SqlQueryAsync<SecurityEventRow>(
            $"""
            SELECT id AS Id,
                   event_type AS EventType,
                   user_id AS UserId,
                   username AS Username,
                   ip AS Ip,
                   severity AS Severity,
                   message AS Message,
                   created_at AS CreatedAt
            FROM sys_security_event
            {where}
            ORDER BY created_at DESC, id DESC
            LIMIT @pageSize OFFSET @offset
            """,
            parameters);

        return new PagedResult<SecurityEventSummaryDto>(rows.Select(row => row.ToSummaryDto()).ToArray(), criteria.Page, criteria.PageSize, total);
    }

    public async Task<SecurityEventDetailDto?> GetSecurityEventAsync(long id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var row = await _db.Ado.SqlQuerySingleAsync<SecurityEventRow>(
            """
            SELECT id AS Id,
                   event_type AS EventType,
                   user_id AS UserId,
                   username AS Username,
                   ip AS Ip,
                   severity AS Severity,
                   message AS Message,
                   created_at AS CreatedAt
            FROM sys_security_event
            WHERE id = @id
            LIMIT 1
            """,
            new SugarParameter("@id", id));

        return row?.ToDetailDto();
    }

    private sealed class LoginLogRow
    {
        public long Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public long? UserId { get; set; }
        public string? Ip { get; set; }
        public string? UserAgent { get; set; }
        public string Result { get; set; } = string.Empty;
        public string? Reason { get; set; }
        public DateTime CreatedAt { get; set; }

        public LoginLogSummaryDto ToSummaryDto() => new(Id, Username, UserId, Ip, Result, Reason, ToOffset(CreatedAt));
        public LoginLogDetailDto ToDetailDto() => new(Id, Username, UserId, Ip, UserAgent, Result, Reason, ToOffset(CreatedAt));
        private static DateTimeOffset ToOffset(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    }

    private sealed class AuditLogRow
    {
        public long Id { get; set; }
        public long? UserId { get; set; }
        public string? Username { get; set; }
        public string Module { get; set; } = string.Empty;
        public string Resource { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public string? TargetId { get; set; }
        public string RequestMethod { get; set; } = string.Empty;
        public string RequestPath { get; set; } = string.Empty;
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
        public string? TraceId { get; set; }
        public string Result { get; set; } = string.Empty;
        public string Detail { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public AuditLogSummaryDto ToSummaryDto() => new(Id, UserId, Username, Module, Resource, Action, TargetId, Result, ToOffset(CreatedAt));
        public AuditLogDetailDto ToDetailDto() => new(Id, UserId, Username, Module, Resource, Action, TargetId, RequestMethod, RequestPath, IpAddress, UserAgent, TraceId, Result, Detail, ToOffset(CreatedAt));
        private static DateTimeOffset ToOffset(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    }

    private sealed class SecurityEventRow
    {
        public long Id { get; set; }
        public string EventType { get; set; } = string.Empty;
        public long? UserId { get; set; }
        public string? Username { get; set; }
        public string? Ip { get; set; }
        public string Severity { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public SecurityEventSummaryDto ToSummaryDto() => new(Id, EventType, UserId, Username, Ip, Severity, Message, ToOffset(CreatedAt));
        public SecurityEventDetailDto ToDetailDto() => new(Id, EventType, UserId, Username, Ip, Severity, Message, ToOffset(CreatedAt));
        private static DateTimeOffset ToOffset(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    }
}
