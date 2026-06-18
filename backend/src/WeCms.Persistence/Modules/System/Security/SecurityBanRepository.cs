using SqlSugar;
using WeCms.Modules.System.Security;
using WeCms.Shared;

namespace WeCms.Persistence.Modules.System.Security;

public sealed class SecurityBanRepository : ISecurityBanRepository
{
    private readonly ISqlSugarClient _db;

    public SecurityBanRepository(ISqlSugarClient db)
    {
        _db = db;
    }

    public async Task<SecurityStatusDto> GetStatusAsync(
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var row = await _db.Ado.SqlQuerySingleAsync<SecurityStatusRow>(
            """
            SELECT COUNT(1) AS ActiveBans,
                   SUM(CASE WHEN ban_type = 'ip' THEN 1 ELSE 0 END) AS ActiveIpBans,
                   SUM(CASE WHEN ban_type = 'user' THEN 1 ELSE 0 END) AS ActiveUserBans,
                   SUM(CASE WHEN severity = 'critical' THEN 1 ELSE 0 END) AS CriticalActiveBans
            FROM sys_security_ban
            WHERE revoked_at IS NULL
              AND (expires_at IS NULL OR expires_at > @now)
            """,
            new SugarParameter("@now", now.UtcDateTime));

        return new SecurityStatusDto(
            row?.ActiveBans ?? 0,
            row?.ActiveIpBans ?? 0,
            row?.ActiveUserBans ?? 0,
            row?.CriticalActiveBans ?? 0,
            now);
    }

    public async Task<PagedResult<SecurityBanSummaryDto>> ListAsync(
        SecurityBanListCriteria criteria,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var parameters = new List<SugarParameter>();
        var where = BuildWhere(criteria, parameters);
        var total = Convert.ToInt64(await _db.Ado.GetScalarAsync($"SELECT COUNT(1) FROM sys_security_ban {where}", parameters), global::System.Globalization.CultureInfo.InvariantCulture);
        parameters.Add(new SugarParameter("@offset", (criteria.Page - 1) * criteria.PageSize));
        parameters.Add(new SugarParameter("@pageSize", criteria.PageSize));

        var rows = await _db.Ado.SqlQueryAsync<SecurityBanListRow>(
            $"""
            SELECT id AS Id,
                   ban_type AS BanType,
                   target AS Target,
                   reason AS Reason,
                   severity AS Severity,
                   source AS Source,
                   expires_at AS ExpiresAt,
                   revoked_at AS RevokedAt,
                   created_at AS CreatedAt,
                   updated_at AS UpdatedAt
            FROM sys_security_ban
            {where}
            ORDER BY created_at DESC, id DESC
            LIMIT @pageSize OFFSET @offset
            """,
            parameters);

        return new PagedResult<SecurityBanSummaryDto>(
            rows.Select(static row => row.ToSummary()).ToArray(),
            criteria.Page,
            criteria.PageSize,
            total);
    }

    public async Task<SecurityBanDetailDto?> GetAsync(
        long id,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var row = await _db.Ado.SqlQuerySingleAsync<SecurityBanDetailRow>(
            """
            SELECT b.id AS Id,
                   b.ban_type AS BanType,
                   b.target AS Target,
                   b.reason AS Reason,
                   b.severity AS Severity,
                   b.source AS Source,
                   b.expires_at AS ExpiresAt,
                   b.revoked_by AS RevokedBy,
                   b.revoked_at AS RevokedAt,
                   b.revoke_reason AS RevokeReason,
                   b.created_at AS CreatedAt,
                   b.updated_at AS UpdatedAt,
                   NULL AS CreatedBy,
                   NULL AS CreatedByUsername
            FROM sys_security_ban b
            WHERE b.id = @id
            LIMIT 1
            """,
            new SugarParameter("@id", id));

        return row?.ToDetail();
    }

    public async Task RevokeAsync(
        SecurityBanRevokeRecord record,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var affectedRows = await _db.Ado.ExecuteCommandAsync(
            """
            UPDATE sys_security_ban
            SET revoked_by = @revokedBy,
                revoked_at = @revokedAt,
                revoke_reason = @revokeReason,
                updated_at = @revokedAt
            WHERE id = @id
              AND revoked_at IS NULL
            """,
            new SugarParameter("@id", record.Id),
            new SugarParameter("@revokedBy", record.RevokedBy),
            new SugarParameter("@revokedAt", record.Now.UtcDateTime),
            new SugarParameter("@revokeReason", record.RevokeReason));

        if (affectedRows != 1)
        {
            throw new InvalidOperationException($"Expected to revoke one security ban row, affected {affectedRows}.");
        }
    }

    public async Task<bool> IsSuperAdminAsync(
        long userId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _db.Ado.GetScalarAsync(
            """
            SELECT is_super_admin
            FROM sys_user
            WHERE id = @userId
              AND deleted_at IS NULL
            LIMIT 1
            """,
            new SugarParameter("@userId", userId));

        return result is not null && Convert.ToBoolean(result, global::System.Globalization.CultureInfo.InvariantCulture);
    }

    public async Task RecordAuditAsync(
        SecurityBanAuditRecord record,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var insertedRows = await _db.Ado.ExecuteCommandAsync(
            """
            INSERT INTO sys_audit_log (user_id, username, module, resource, action, target_id, request_method, request_path, ip_address, user_agent, trace_id, result, detail, created_at)
            VALUES (@userId, @username, 'system', 'security-ban', @action, @targetId, 'POST', @requestPath, @ip, @userAgent, @traceId, @result, @detail, @createdAt)
            """,
            new SugarParameter("@userId", record.ActorUserId),
            new SugarParameter("@username", record.ActorUsername),
            new SugarParameter("@action", record.Action),
            new SugarParameter("@targetId", record.TargetBanId.ToString(global::System.Globalization.CultureInfo.InvariantCulture)),
            new SugarParameter("@requestPath", "/api/v1/system/security/bans"),
            new SugarParameter("@ip", record.Ip),
            new SugarParameter("@userAgent", record.UserAgent),
            new SugarParameter("@traceId", record.TraceId),
            new SugarParameter("@result", record.Result),
            new SugarParameter("@detail", record.Detail),
            new SugarParameter("@createdAt", record.Now.UtcDateTime));

        if (insertedRows != 1)
        {
            throw new InvalidOperationException($"Expected to insert one audit log row, inserted {insertedRows}.");
        }
    }

    public async Task<SecurityBanRecord?> FindActiveAsync(
        string banType,
        string target,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var row = await _db.Ado.SqlQuerySingleAsync<SecurityBanRow>(
            """
            SELECT id AS Id,
                   ban_type AS BanType,
                   target AS Target,
                   reason AS Reason,
                   severity AS Severity,
                   source AS Source,
                   expires_at AS ExpiresAt,
                   revoked_at AS RevokedAt
            FROM sys_security_ban
            WHERE ban_type = @banType
              AND target = @target
              AND revoked_at IS NULL
              AND (expires_at IS NULL OR expires_at > @now)
            ORDER BY created_at DESC, id DESC
            LIMIT 1
            """,
            new SugarParameter("@banType", banType),
            new SugarParameter("@target", target),
            new SugarParameter("@now", now.UtcDateTime));

        return row?.ToRecord();
    }

    public async Task<long> CreateAsync(
        CreateSecurityBanRecord record,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var insertedRows = await _db.Ado.ExecuteCommandAsync(
            """
            INSERT INTO sys_security_ban (ban_type, target, reason, severity, source, expires_at, revoked_at, revoked_by, revoke_reason, created_at, updated_at)
            VALUES (@banType, @target, @reason, @severity, @source, @expiresAt, NULL, NULL, NULL, @createdAt, @createdAt)
            """,
            new SugarParameter("@banType", record.BanType),
            new SugarParameter("@target", record.Target),
            new SugarParameter("@reason", record.Reason),
            new SugarParameter("@severity", record.Severity),
            new SugarParameter("@source", record.Source),
            new SugarParameter("@expiresAt", record.ExpiresAt?.UtcDateTime),
            new SugarParameter("@createdAt", record.CreatedAt.UtcDateTime));

        if (insertedRows != 1)
        {
            throw new InvalidOperationException($"Expected to insert one security ban row, inserted {insertedRows}.");
        }

        return Convert.ToInt64(await _db.Ado.GetScalarAsync("SELECT LAST_INSERT_ID()"), global::System.Globalization.CultureInfo.InvariantCulture);
    }

    public async Task RecordSecurityEventAsync(
        SecurityBanSecurityEventRecord record,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var insertedRows = await _db.Ado.ExecuteCommandAsync(
            """
            INSERT INTO sys_security_event (event_type, user_id, username, ip, severity, message, created_at)
            VALUES (@eventType, @userId, @username, @ip, @severity, @message, @createdAt)
            """,
            new SugarParameter("@eventType", record.EventType),
            new SugarParameter("@userId", record.UserId),
            new SugarParameter("@username", record.Username),
            new SugarParameter("@ip", record.Ip),
            new SugarParameter("@severity", record.Severity),
            new SugarParameter("@message", record.Message),
            new SugarParameter("@createdAt", record.CreatedAt.UtcDateTime));

        if (insertedRows != 1)
        {
            throw new InvalidOperationException($"Expected to insert one security event row, inserted {insertedRows}.");
        }
    }

    private sealed class SecurityBanRow
    {
        public long Id { get; set; }
        public string BanType { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public DateTime? ExpiresAt { get; set; }
        public DateTime? RevokedAt { get; set; }

        public SecurityBanRecord ToRecord()
        {
            return new SecurityBanRecord(
                Id,
                BanType,
                Target,
                Reason,
                Severity,
                Source,
                ToOffset(ExpiresAt),
                ToOffset(RevokedAt));
        }

    }

    private static DateTimeOffset? ToOffset(DateTime? value)
    {
        return value is null ? null : new DateTimeOffset(DateTime.SpecifyKind(value.Value, DateTimeKind.Utc));
    }

    private static DateTimeOffset ToRequiredOffset(DateTime value)
    {
        return new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    }

    private static string BuildWhere(SecurityBanListCriteria criteria, List<SugarParameter> parameters)
    {
        var clauses = new List<string>();

        if (criteria.ActiveOnly)
        {
            clauses.Add("revoked_at IS NULL");
            clauses.Add("(expires_at IS NULL OR expires_at > @now)");
            parameters.Add(new SugarParameter("@now", criteria.Now.UtcDateTime));
        }

        if (criteria.BanType is not null)
        {
            clauses.Add("ban_type = @banType");
            parameters.Add(new SugarParameter("@banType", criteria.BanType));
        }

        if (criteria.Target is not null)
        {
            clauses.Add("target LIKE @target");
            parameters.Add(new SugarParameter("@target", $"%{criteria.Target}%"));
        }

        if (criteria.Severity is not null)
        {
            clauses.Add("severity = @severity");
            parameters.Add(new SugarParameter("@severity", criteria.Severity));
        }

        if (criteria.Source is not null)
        {
            clauses.Add("source = @source");
            parameters.Add(new SugarParameter("@source", criteria.Source));
        }

        return clauses.Count == 0 ? string.Empty : $"WHERE {string.Join(" AND ", clauses)}";
    }

    private sealed class SecurityStatusRow
    {
        public long ActiveBans { get; set; }
        public long ActiveIpBans { get; set; }
        public long ActiveUserBans { get; set; }
        public long CriticalActiveBans { get; set; }
    }

    private sealed class SecurityBanListRow
    {
        public long Id { get; set; }
        public string BanType { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public DateTime? ExpiresAt { get; set; }
        public DateTime? RevokedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public SecurityBanSummaryDto ToSummary()
        {
            return new SecurityBanSummaryDto(
                Id,
                BanType,
                Target,
                Reason,
                Severity,
                Source,
                ToOffset(ExpiresAt),
                ToOffset(RevokedAt),
                ToRequiredOffset(CreatedAt),
                ToRequiredOffset(UpdatedAt));
        }
    }

    private sealed class SecurityBanDetailRow
    {
        public long Id { get; set; }
        public string BanType { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
        public DateTime? ExpiresAt { get; set; }
        public long? RevokedBy { get; set; }
        public DateTime? RevokedAt { get; set; }
        public string? RevokeReason { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public long? CreatedBy { get; set; }
        public string? CreatedByUsername { get; set; }

        public SecurityBanDetailDto ToDetail()
        {
            return new SecurityBanDetailDto(
                Id,
                BanType,
                Target,
                Reason,
                Severity,
                Source,
                ToOffset(ExpiresAt),
                RevokedBy,
                ToOffset(RevokedAt),
                RevokeReason,
                ToRequiredOffset(CreatedAt),
                ToRequiredOffset(UpdatedAt),
                CreatedBy,
                CreatedByUsername);
        }
    }
}
