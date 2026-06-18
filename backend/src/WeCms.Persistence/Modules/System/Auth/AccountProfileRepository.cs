using SqlSugar;
using WeCms.Modules.System.Auth;

namespace WeCms.Persistence.Modules.System.Auth;

public sealed class AccountProfileRepository : IAccountProfileRepository
{
    private readonly ISqlSugarClient _db;

    public AccountProfileRepository(ISqlSugarClient db)
    {
        _db = db;
    }

    public async Task<AccountProfileRecord?> GetAsync(long userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var row = await _db.Ado.SqlQuerySingleAsync<AccountProfileRow>(
            """
            SELECT id AS Id,
                   username AS Username,
                   display_name AS DisplayName,
                   password_hash AS PasswordHash,
                   email AS Email,
                   phone AS Phone,
                   avatar_object_key AS AvatarObjectKey,
                   avatar_mime_type AS AvatarMimeType,
                   avatar_file_ext AS AvatarFileExt,
                   must_change_password AS MustChangePassword,
                   last_login_at AS LastLoginAt,
                   last_login_ip AS LastLoginIp
            FROM sys_user
            WHERE id = @userId
              AND deleted_at IS NULL
              AND status = 'enabled'
            LIMIT 1
            """,
            new SugarParameter("@userId", userId));
        return row?.ToRecord();
    }

    public async Task<bool> EmailExistsAsync(string email, long exceptUserId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var count = await _db.Ado.GetIntAsync(
            """
            SELECT COUNT(1)
            FROM sys_user
            WHERE email = @email
              AND id <> @exceptUserId
              AND deleted_at IS NULL
            """,
            new SugarParameter("@email", email),
            new SugarParameter("@exceptUserId", exceptUserId));
        return count > 0;
    }

    public async Task<bool> PhoneExistsAsync(string phone, long exceptUserId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var count = await _db.Ado.GetIntAsync(
            """
            SELECT COUNT(1)
            FROM sys_user
            WHERE phone = @phone
              AND id <> @exceptUserId
              AND deleted_at IS NULL
            """,
            new SugarParameter("@phone", phone),
            new SugarParameter("@exceptUserId", exceptUserId));
        return count > 0;
    }

    public Task UpdateProfileAsync(AccountProfileUpdateRecord record, CancellationToken cancellationToken)
    {
        return ExpectOneAsync(
            """
            UPDATE sys_user
            SET display_name = @displayName,
                email = @email,
                phone = @phone,
                updated_at = @updatedAt
            WHERE id = @userId
              AND deleted_at IS NULL
            """,
            cancellationToken,
            new SugarParameter("@displayName", record.DisplayName),
            new SugarParameter("@email", record.Email),
            new SugarParameter("@phone", record.Phone),
            new SugarParameter("@updatedAt", ToDatabaseDateTime(record.Now)),
            new SugarParameter("@userId", record.UserId));
    }

    public Task UpdatePasswordAsync(AccountPasswordUpdateRecord record, CancellationToken cancellationToken)
    {
        return ExpectOneAsync(
            """
            UPDATE sys_user
            SET password_hash = @passwordHash,
                security_stamp = @securityStamp,
                must_change_password = FALSE,
                updated_at = @updatedAt
            WHERE id = @userId
              AND deleted_at IS NULL
            """,
            cancellationToken,
            new SugarParameter("@passwordHash", record.PasswordHash),
            new SugarParameter("@securityStamp", Guid.NewGuid().ToString("N")),
            new SugarParameter("@updatedAt", ToDatabaseDateTime(record.Now)),
            new SugarParameter("@userId", record.UserId));
    }

    public Task RevokeRefreshTokensAsync(long userId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _db.Ado.ExecuteCommandAsync(
            """
            UPDATE sys_refresh_token
            SET revoked_at = @revokedAt
            WHERE user_id = @userId
              AND revoked_at IS NULL
            """,
            new SugarParameter("@revokedAt", ToDatabaseDateTime(now)),
            new SugarParameter("@userId", userId));
    }

    public Task UpdateAvatarAsync(AccountAvatarUpdateRecord record, CancellationToken cancellationToken)
    {
        return ExpectOneAsync(
            """
            UPDATE sys_user
            SET avatar_object_key = @objectKey,
                avatar_mime_type = @mimeType,
                avatar_file_ext = @fileExt,
                avatar_updated_at = @avatarUpdatedAt,
                updated_at = @updatedAt
            WHERE id = @userId
              AND deleted_at IS NULL
            """,
            cancellationToken,
            new SugarParameter("@objectKey", record.ObjectKey),
            new SugarParameter("@mimeType", record.MimeType),
            new SugarParameter("@fileExt", record.FileExt),
            new SugarParameter("@avatarUpdatedAt", ToDatabaseDateTime(record.Now)),
            new SugarParameter("@updatedAt", ToDatabaseDateTime(record.Now)),
            new SugarParameter("@userId", record.UserId));
    }

    public Task RecordAuditAsync(AccountAuditRecord record, CancellationToken cancellationToken)
    {
        return ExpectOneAsync(
            """
            INSERT INTO sys_audit_log (user_id, username, module, resource, action, target_id, request_method, request_path, ip_address, user_agent, trace_id, result, detail, created_at)
            VALUES (@userId, @username, 'system', 'account', @action, @targetId, '', '/api/v1/account', @ip, @userAgent, @traceId, @result, @detail, @createdAt)
            """,
            cancellationToken,
            new SugarParameter("@userId", record.UserId),
            new SugarParameter("@username", record.Username),
            new SugarParameter("@action", record.Action),
            new SugarParameter("@targetId", record.UserId.ToString(global::System.Globalization.CultureInfo.InvariantCulture)),
            new SugarParameter("@ip", record.Ip),
            new SugarParameter("@userAgent", record.UserAgent),
            new SugarParameter("@traceId", record.TraceId),
            new SugarParameter("@result", record.Result),
            new SugarParameter("@detail", record.Detail),
            new SugarParameter("@createdAt", ToDatabaseDateTime(record.CreatedAt)));
    }

    public Task RecordSecurityEventAsync(AccountSecurityEventRecord record, CancellationToken cancellationToken)
    {
        return ExpectOneAsync(
            """
            INSERT INTO sys_security_event (event_type, user_id, username, ip, severity, message, created_at)
            VALUES (@eventType, @userId, @username, @ip, @severity, @message, @createdAt)
            """,
            cancellationToken,
            new SugarParameter("@eventType", record.EventType),
            new SugarParameter("@userId", record.UserId),
            new SugarParameter("@username", record.Username),
            new SugarParameter("@ip", record.Ip),
            new SugarParameter("@severity", record.Severity),
            new SugarParameter("@message", record.Message),
            new SugarParameter("@createdAt", ToDatabaseDateTime(record.CreatedAt)));
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

    private sealed class AccountProfileRow
    {
        public long Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? AvatarObjectKey { get; set; }
        public string? AvatarMimeType { get; set; }
        public string? AvatarFileExt { get; set; }
        public bool MustChangePassword { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public string? LastLoginIp { get; set; }

        public AccountProfileRecord ToRecord()
        {
            return new AccountProfileRecord(
                Id,
                Username,
                DisplayName,
                PasswordHash,
                Email,
                Phone,
                AvatarObjectKey,
                AvatarMimeType,
                AvatarFileExt,
                MustChangePassword,
                LastLoginAt is null ? null : new DateTimeOffset(DateTime.SpecifyKind(LastLoginAt.Value, DateTimeKind.Utc)),
                LastLoginIp);
        }
    }

    private static DateTime ToDatabaseDateTime(DateTimeOffset value)
    {
        return DateTime.SpecifyKind(value.UtcDateTime, DateTimeKind.Unspecified);
    }
}
