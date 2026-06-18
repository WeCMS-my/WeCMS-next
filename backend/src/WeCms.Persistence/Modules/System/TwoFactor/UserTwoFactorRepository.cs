using System.Text.Json;
using SqlSugar;
using WeCms.Modules.System.TwoFactor;

namespace WeCms.Persistence.Modules.System.TwoFactor;

public sealed class UserTwoFactorRepository : IUserTwoFactorRepository
{
    private readonly ISqlSugarClient _db;

    public UserTwoFactorRepository(ISqlSugarClient db)
    {
        _db = db;
    }

    public async Task<UserTwoFactorRecord?> GetByUserIdAsync(long userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var row = await _db.Ado.SqlQuerySingleAsync<UserTwoFactorRow>(
            """
            SELECT id AS Id,
                   user_id AS UserId,
                   enabled AS Enabled,
                   secret_cipher AS SecretCipher,
                   confirmed_at AS ConfirmedAt,
                   last_totp_step AS LastTotpStep,
                   recovery_codes_hash_json AS RecoveryCodesHashJson,
                   recovery_codes_used_count AS RecoveryCodesUsedCount,
                   reset_required AS ResetRequired,
                   created_at AS CreatedAt,
                   updated_at AS UpdatedAt
            FROM sys_user_two_factor
            WHERE user_id = @userId
            LIMIT 1
            """,
            new SugarParameter("@userId", userId));

        return row?.ToRecord();
    }

    public async Task UpsertSetupAsync(UserTwoFactorSetupRecord record, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var rows = await _db.Ado.ExecuteCommandAsync(
            """
            INSERT INTO sys_user_two_factor (
                user_id,
                enabled,
                secret_cipher,
                confirmed_at,
                last_totp_step,
                recovery_codes_hash_json,
                recovery_codes_used_count,
                reset_required,
                created_at,
                updated_at)
            VALUES (
                @userId,
                FALSE,
                @secretCipher,
                NULL,
                NULL,
                @recoveryCodesHashJson,
                0,
                FALSE,
                @now,
                @now)
            ON DUPLICATE KEY UPDATE
                enabled = FALSE,
                secret_cipher = VALUES(secret_cipher),
                confirmed_at = NULL,
                last_totp_step = NULL,
                recovery_codes_hash_json = VALUES(recovery_codes_hash_json),
                recovery_codes_used_count = 0,
                reset_required = FALSE,
                updated_at = VALUES(updated_at)
            """,
            new SugarParameter("@userId", record.UserId),
            new SugarParameter("@secretCipher", record.SecretCipher),
            new SugarParameter("@recoveryCodesHashJson", JsonSerializer.Serialize(record.RecoveryCodeHashes)),
            new SugarParameter("@now", ToDatabaseDateTime(record.Now)));

        if (rows is < 1 or > 2)
        {
            throw new InvalidOperationException($"Expected to upsert one two-factor row, affected {rows}.");
        }
    }

    public async Task EnableAsync(UserTwoFactorEnableRecord record, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var rows = await _db.Ado.ExecuteCommandAsync(
            """
            UPDATE sys_user_two_factor
            SET enabled = TRUE,
                confirmed_at = @now,
                last_totp_step = @lastTotpStep,
                reset_required = FALSE,
                updated_at = @now
            WHERE user_id = @userId
              AND (last_totp_step IS NULL OR last_totp_step <> @lastTotpStep)
            """,
            new SugarParameter("@userId", record.UserId),
            new SugarParameter("@lastTotpStep", record.LastTotpStep),
            new SugarParameter("@now", ToDatabaseDateTime(record.Now)));

        if (rows != 1)
        {
            throw new InvalidOperationException($"Expected to enable one two-factor row, affected {rows}.");
        }
    }

    public async Task UpdateRecoveryCodesAsync(UserTwoFactorRecoveryCodeUpdateRecord record, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var rows = await _db.Ado.ExecuteCommandAsync(
            """
            UPDATE sys_user_two_factor
            SET recovery_codes_hash_json = @recoveryCodesHashJson,
                recovery_codes_used_count = @recoveryCodesUsedCount,
                updated_at = @now
            WHERE user_id = @userId
            """,
            new SugarParameter("@userId", record.UserId),
            new SugarParameter("@recoveryCodesHashJson", JsonSerializer.Serialize(record.RecoveryCodeHashes)),
            new SugarParameter("@recoveryCodesUsedCount", record.RecoveryCodesUsedCount),
            new SugarParameter("@now", ToDatabaseDateTime(record.Now)));

        if (rows != 1)
        {
            throw new InvalidOperationException($"Expected to update one two-factor recovery code row, affected {rows}.");
        }
    }

    public async Task UpdateLastTotpStepAsync(UserTwoFactorTotpStepUpdateRecord record, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var rows = await _db.Ado.ExecuteCommandAsync(
            """
            UPDATE sys_user_two_factor
            SET last_totp_step = @lastTotpStep,
                updated_at = @now
            WHERE user_id = @userId
              AND enabled = TRUE
              AND (last_totp_step IS NULL OR last_totp_step <> @lastTotpStep)
            """,
            new SugarParameter("@userId", record.UserId),
            new SugarParameter("@lastTotpStep", record.LastTotpStep),
            new SugarParameter("@now", ToDatabaseDateTime(record.Now)));

        if (rows != 1)
        {
            throw new InvalidOperationException($"Expected to update one two-factor TOTP step row, affected {rows}.");
        }
    }

    public async Task ClearAsync(long userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var rows = await _db.Ado.ExecuteCommandAsync(
            """
            UPDATE sys_user_two_factor
            SET enabled = FALSE,
                secret_cipher = '',
                confirmed_at = NULL,
                last_totp_step = NULL,
                recovery_codes_hash_json = JSON_ARRAY(),
                recovery_codes_used_count = 0,
                reset_required = TRUE,
                updated_at = @now
            WHERE user_id = @userId
            """,
            new SugarParameter("@userId", userId),
            new SugarParameter("@now", ToDatabaseDateTime(now)));

        if (rows > 1)
        {
            throw new InvalidOperationException($"Expected to clear at most one two-factor row, affected {rows}.");
        }
    }

    private sealed class UserTwoFactorRow
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public bool Enabled { get; set; }
        public string SecretCipher { get; set; } = string.Empty;
        public DateTime? ConfirmedAt { get; set; }
        public long? LastTotpStep { get; set; }
        public string RecoveryCodesHashJson { get; set; } = "[]";
        public int RecoveryCodesUsedCount { get; set; }
        public bool ResetRequired { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public UserTwoFactorRecord ToRecord()
        {
            return new UserTwoFactorRecord(
                Id,
                UserId,
                Enabled,
                SecretCipher,
                ConfirmedAt is null ? null : new DateTimeOffset(DateTime.SpecifyKind(ConfirmedAt.Value, DateTimeKind.Utc)),
                LastTotpStep,
                JsonSerializer.Deserialize<IReadOnlyList<string>>(RecoveryCodesHashJson) ?? [],
                RecoveryCodesUsedCount,
                ResetRequired,
                new DateTimeOffset(DateTime.SpecifyKind(CreatedAt, DateTimeKind.Utc)),
                new DateTimeOffset(DateTime.SpecifyKind(UpdatedAt, DateTimeKind.Utc)));
        }
    }

    private static DateTime ToDatabaseDateTime(DateTimeOffset value)
    {
        return DateTime.SpecifyKind(value.UtcDateTime, DateTimeKind.Unspecified);
    }
}
