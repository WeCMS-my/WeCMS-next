using SqlSugar;
using WeCms.Modules.System.Auth;
using WeCms.Shared.Security;

namespace WeCms.Persistence.Modules.System.Auth;

public sealed class LoginFailureCounterRepository : ILoginFailureCounterRepository
{
    private readonly ISqlSugarClient _db;
    private readonly ISecurityEventClassifier _securityEventClassifier;

    public LoginFailureCounterRepository(ISqlSugarClient db, ISecurityEventClassifier securityEventClassifier)
    {
        _db = db;
        _securityEventClassifier = securityEventClassifier;
    }

    public async Task<LoginFailureCounterRecord> IncrementAsync(
        LoginFailureCounterIncrement record,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var existing = await GetAsync(record.Scope, record.Target, cancellationToken);
        if (existing is null)
        {
            await InsertAsync(record, cancellationToken);
            return new LoginFailureCounterRecord(record.Scope, record.Target, 1);
        }

        await UpdateAsync(record, cancellationToken);
        var updated = await GetAsync(record.Scope, record.Target, cancellationToken)
            ?? throw new InvalidOperationException("Login failure counter disappeared after update.");
        return new LoginFailureCounterRecord(record.Scope, record.Target, updated.FailureCount);
    }

    public async Task ResetAsync(
        string scope,
        string target,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await _db.Ado.ExecuteCommandAsync(
            """
            DELETE FROM sys_login_failure_counter
            WHERE scope = @scope
              AND target = @target
            """,
            new SugarParameter("@scope", scope),
            new SugarParameter("@target", target));
    }

    public async Task RecordSecurityEventAsync(
        SecurityEventRecord record,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var classification = _securityEventClassifier.Classify(record.EventType, record.TraceId);

        var insertedRows = await _db.Ado.ExecuteCommandAsync(
            """
            INSERT INTO sys_security_event (event_type, user_id, username, ip, severity, source, message, trace_id, created_at)
            VALUES (@eventType, @userId, @username, @ip, @severity, @source, @message, @traceId, @createdAt)
            """,
            new SugarParameter("@eventType", classification.EventType),
            new SugarParameter("@userId", record.UserId),
            new SugarParameter("@username", record.Username),
            new SugarParameter("@ip", record.Ip),
            new SugarParameter("@severity", classification.Severity),
            new SugarParameter("@source", classification.Source),
            new SugarParameter("@message", record.Message),
            new SugarParameter("@traceId", classification.TraceId),
            new SugarParameter("@createdAt", record.CreatedAt.UtcDateTime));

        if (insertedRows != 1)
        {
            throw new InvalidOperationException($"Expected to insert one security event row, inserted {insertedRows}.");
        }
    }

    private async Task<LoginFailureCounterRow?> GetAsync(
        string scope,
        string target,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        return await _db.Ado.SqlQuerySingleAsync<LoginFailureCounterRow>(
            """
            SELECT scope AS Scope,
                   target AS Target,
                   failure_count AS FailureCount,
                   window_started_at AS WindowStartedAt
            FROM sys_login_failure_counter
            WHERE scope = @scope
              AND target = @target
            LIMIT 1
            """,
            new SugarParameter("@scope", scope),
            new SugarParameter("@target", target));
    }

    private async Task InsertAsync(
        LoginFailureCounterIncrement record,
        CancellationToken cancellationToken)
    {
        var insertedRows = await _db.Ado.ExecuteCommandAsync(
            """
            INSERT INTO sys_login_failure_counter (scope, target, failure_count, window_started_at, last_failed_at, updated_at)
            VALUES (@scope, @target, 1, @windowStartedAt, @lastFailedAt, @lastFailedAt)
            """,
            new SugarParameter("@scope", record.Scope),
            new SugarParameter("@target", record.Target),
            new SugarParameter("@windowStartedAt", ToDatabaseDateTime(record.WindowStartedAt)),
            new SugarParameter("@lastFailedAt", ToDatabaseDateTime(record.LastFailedAt)));

        if (insertedRows != 1)
        {
            throw new InvalidOperationException($"Expected to insert one login failure counter row, inserted {insertedRows}.");
        }
    }

    private async Task UpdateAsync(
        LoginFailureCounterIncrement record,
        CancellationToken cancellationToken)
    {
        var windowBoundary = record.LastFailedAt.Subtract(record.Window);
        var updatedRows = await _db.Ado.ExecuteCommandAsync(
            """
            UPDATE sys_login_failure_counter
            SET failure_count = CASE WHEN window_started_at <= @windowBoundary THEN 1 ELSE failure_count + 1 END,
                window_started_at = CASE WHEN window_started_at <= @windowBoundary THEN @lastFailedAt ELSE window_started_at END,
                last_failed_at = @lastFailedAt,
                updated_at = @lastFailedAt
            WHERE scope = @scope
              AND target = @target
            """,
            new SugarParameter("@windowBoundary", ToDatabaseDateTime(windowBoundary)),
            new SugarParameter("@lastFailedAt", ToDatabaseDateTime(record.LastFailedAt)),
            new SugarParameter("@scope", record.Scope),
            new SugarParameter("@target", record.Target));

        if (updatedRows != 1)
        {
            throw new InvalidOperationException($"Expected to update one login failure counter row, updated {updatedRows}.");
        }
    }

    private sealed class LoginFailureCounterRow
    {
        public string Scope { get; set; } = string.Empty;
        public string Target { get; set; } = string.Empty;
        public int FailureCount { get; set; }
        public DateTime WindowStartedAt { get; set; }
    }

    private static DateTime ToDatabaseDateTime(DateTimeOffset value)
    {
        return DateTime.SpecifyKind(value.UtcDateTime, DateTimeKind.Unspecified);
    }
}
