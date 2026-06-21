using SqlSugar;
using WeCms.Modules.Security.Events;
using WeCms.Shared;

namespace WeCms.Modules.Security.SqlSugar.Repositories;

public sealed class SecurityEventRepository : ISecurityEventRepository
{
    private readonly ISqlSugarClient _db;

    public SecurityEventRepository(ISqlSugarClient db) => _db = db;

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
                   source AS Source,
                   trace_id AS TraceId,
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
                   source AS Source,
                   trace_id AS TraceId,
                   message AS Message,
                   created_at AS CreatedAt
            FROM sys_security_event
            WHERE id = @id
            LIMIT 1
            """,
            new SugarParameter("@id", id));

        return row?.ToDetailDto();
    }

    private sealed class SecurityEventRow
    {
        public long Id { get; set; }
        public string EventType { get; set; } = string.Empty;
        public long? UserId { get; set; }
        public string? Username { get; set; }
        public string? Ip { get; set; }
        public string Severity { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public string TraceId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public SecurityEventSummaryDto ToSummaryDto() => new(Id, EventType, UserId, Username, Ip, Severity, Source, TraceId, Message, ToOffset(CreatedAt));
        public SecurityEventDetailDto ToDetailDto() => new(Id, EventType, UserId, Username, Ip, Severity, Source, TraceId, Message, ToOffset(CreatedAt));
        private static DateTimeOffset ToOffset(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    }
}
