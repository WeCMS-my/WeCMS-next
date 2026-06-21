using SqlSugar;

namespace WeCms.Modules.Identity.SqlSugar.Repositories;

public sealed class AuthChallengeRepository : IAuthChallengeRepository
{
    private readonly ISqlSugarClient _db;

    public AuthChallengeRepository(ISqlSugarClient db)
    {
        _db = db;
    }

    public async Task CreateAsync(CreateAuthChallengeRecord record, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var rows = await _db.Ado.ExecuteCommandAsync(
            """
            INSERT INTO sys_auth_challenge (
                challenge_id,
                user_id,
                challenge_type,
                status,
                failed_attempts,
                expires_at,
                consumed_at,
                ip,
                user_agent,
                trace_id,
                created_at,
                updated_at)
            VALUES (
                @challengeId,
                @userId,
                @challengeType,
                'pending',
                0,
                @expiresAt,
                NULL,
                @ip,
                @userAgent,
                @traceId,
                @now,
                @now)
            """,
            new SugarParameter("@challengeId", record.ChallengeId),
            new SugarParameter("@userId", record.UserId),
            new SugarParameter("@challengeType", record.ChallengeType),
            new SugarParameter("@expiresAt", ToDatabaseDateTime(record.ExpiresAt)),
            new SugarParameter("@ip", record.Ip),
            new SugarParameter("@userAgent", record.UserAgent),
            new SugarParameter("@traceId", record.TraceId),
            new SugarParameter("@now", ToDatabaseDateTime(record.Now)));

        if (rows != 1)
        {
            throw new InvalidOperationException($"Expected to insert one auth challenge row, inserted {rows}.");
        }
    }

    public async Task<AuthChallengeRecord?> FindByChallengeIdAsync(string challengeId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var row = await _db.Ado.SqlQuerySingleAsync<AuthChallengeRow>(
            """
            SELECT id AS Id,
                   challenge_id AS ChallengeId,
                   user_id AS UserId,
                   challenge_type AS ChallengeType,
                   status AS Status,
                   failed_attempts AS FailedAttempts,
                   expires_at AS ExpiresAt,
                   consumed_at AS ConsumedAt,
                   ip AS Ip,
                   user_agent AS UserAgent,
                   trace_id AS TraceId,
                   created_at AS CreatedAt
            FROM sys_auth_challenge
            WHERE challenge_id = @challengeId
            LIMIT 1
            """,
            new SugarParameter("@challengeId", challengeId));

        return row?.ToRecord();
    }

    public async Task<int> IncrementFailedAttemptsAsync(long id, DateTimeOffset now, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var rows = await _db.Ado.ExecuteCommandAsync(
            """
            UPDATE sys_auth_challenge
            SET failed_attempts = failed_attempts + 1,
                updated_at = @now
            WHERE id = @id
              AND status = 'pending'
            """,
            new SugarParameter("@id", id),
            new SugarParameter("@now", ToDatabaseDateTime(now)));

        if (rows != 1)
        {
            throw new InvalidOperationException($"Expected to update one auth challenge failure row, updated {rows}.");
        }

        var value = await _db.Ado.GetScalarAsync(
            "SELECT failed_attempts FROM sys_auth_challenge WHERE id = @id",
            new SugarParameter("@id", id));
        return Convert.ToInt32(value, global::System.Globalization.CultureInfo.InvariantCulture);
    }

    public async Task MarkFailedAsync(long id, DateTimeOffset now, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var rows = await _db.Ado.ExecuteCommandAsync(
            """
            UPDATE sys_auth_challenge
            SET status = 'failed',
                updated_at = @now
            WHERE id = @id
              AND status = 'pending'
            """,
            new SugarParameter("@id", id),
            new SugarParameter("@now", ToDatabaseDateTime(now)));

        if (rows > 1)
        {
            throw new InvalidOperationException($"Expected to fail at most one auth challenge row, updated {rows}.");
        }
    }

    public async Task<bool> ConsumeAsync(long id, DateTimeOffset now, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var rows = await _db.Ado.ExecuteCommandAsync(
            """
            UPDATE sys_auth_challenge
            SET status = 'consumed',
                consumed_at = @now,
                updated_at = @now
            WHERE id = @id
              AND status = 'pending'
              AND expires_at > @now
            """,
            new SugarParameter("@id", id),
            new SugarParameter("@now", ToDatabaseDateTime(now)));

        return rows == 1;
    }

    private sealed class AuthChallengeRow
    {
        public long Id { get; set; }
        public string ChallengeId { get; set; } = string.Empty;
        public long UserId { get; set; }
        public string ChallengeType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int FailedAttempts { get; set; }
        public DateTime ExpiresAt { get; set; }
        public DateTime? ConsumedAt { get; set; }
        public string Ip { get; set; } = string.Empty;
        public string UserAgent { get; set; } = string.Empty;
        public string TraceId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }

        public AuthChallengeRecord ToRecord()
        {
            return new AuthChallengeRecord(
                Id,
                ChallengeId,
                UserId,
                ChallengeType,
                Status,
                FailedAttempts,
                ToOffset(ExpiresAt),
                ConsumedAt is null ? null : ToOffset(ConsumedAt.Value),
                Ip,
                UserAgent,
                TraceId,
                ToOffset(CreatedAt));
        }
    }

    private static DateTimeOffset ToOffset(DateTime value)
    {
        return new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    }

    private static DateTime ToDatabaseDateTime(DateTimeOffset value)
    {
        return DateTime.SpecifyKind(value.UtcDateTime, DateTimeKind.Unspecified);
    }
}
