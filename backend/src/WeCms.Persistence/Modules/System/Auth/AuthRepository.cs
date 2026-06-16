using SqlSugar;
using WeCms.Modules.System.Auth;

namespace WeCms.Persistence.Modules.System.Auth;

public sealed class AuthRepository : IAuthRepository
{
    private readonly ISqlSugarClient _db;

    public AuthRepository(ISqlSugarClient db)
    {
        _db = db;
    }

    public Task<AuthUserRecord?> FindUserByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var record = _db.Ado.SqlQuerySingle<AuthUserRow>(
            """
            SELECT id AS Id,
                   username AS Username,
                   display_name AS DisplayName,
                   password_hash AS PasswordHash,
                   status AS Status,
                   is_super_admin AS IsSuperAdmin
            FROM sys_user
            WHERE username = @username
            LIMIT 1
            """,
            new SugarParameter("@username", username));

        return Task.FromResult(record?.ToRecord());
    }

    public Task<AuthUserRecord?> FindUserByIdAsync(long userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var record = _db.Ado.SqlQuerySingle<AuthUserRow>(
            """
            SELECT id AS Id,
                   username AS Username,
                   display_name AS DisplayName,
                   password_hash AS PasswordHash,
                   status AS Status,
                   is_super_admin AS IsSuperAdmin
            FROM sys_user
            WHERE id = @userId
            LIMIT 1
            """,
            new SugarParameter("@userId", userId));

        return Task.FromResult(record?.ToRecord());
    }

    public Task<RefreshTokenRecord?> FindRefreshTokenByHashAsync(string tokenHash, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var table = _db.Ado.GetDataTable(
            """
            SELECT rt.id,
                   rt.user_id,
                   u.username,
                   u.display_name,
                   u.status,
                   u.is_super_admin,
                   rt.token_hash,
                   rt.family_id,
                   rt.expires_at,
                   rt.revoked_at
            FROM sys_refresh_token rt
            INNER JOIN sys_user u ON u.id = rt.user_id
            WHERE rt.token_hash = @tokenHash
            LIMIT 1
            """,
            new SugarParameter("@tokenHash", tokenHash));

        if (table.Rows.Count == 0)
        {
            return Task.FromResult<RefreshTokenRecord?>(null);
        }

        var row = table.Rows[0];
        var revokedAt = row["revoked_at"] == DBNull.Value
            ? (DateTimeOffset?)null
            : new DateTimeOffset(DateTime.SpecifyKind((DateTime)row["revoked_at"], DateTimeKind.Utc));

        return Task.FromResult<RefreshTokenRecord?>(new RefreshTokenRecord(
            Convert.ToInt64(row["id"], global::System.Globalization.CultureInfo.InvariantCulture),
            Convert.ToInt64(row["user_id"], global::System.Globalization.CultureInfo.InvariantCulture),
            Convert.ToString(row["username"], global::System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
            Convert.ToString(row["display_name"], global::System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
            Convert.ToString(row["status"], global::System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
            Convert.ToBoolean(row["is_super_admin"], global::System.Globalization.CultureInfo.InvariantCulture),
            Convert.ToString(row["token_hash"], global::System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
            Convert.ToString(row["family_id"], global::System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
            new DateTimeOffset(DateTime.SpecifyKind((DateTime)row["expires_at"], DateTimeKind.Utc)),
            revokedAt));
    }

    public Task<IReadOnlyList<string>> ListRoleCodesAsync(long userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var rows = _db.Ado.SqlQuery<string>(
            """
            SELECT r.code
            FROM sys_role r
            INNER JOIN sys_user_role ur ON ur.role_id = r.id
            WHERE ur.user_id = @userId
              AND r.status = 'enabled'
            ORDER BY r.code
            """,
            new SugarParameter("@userId", userId));

        return Task.FromResult<IReadOnlyList<string>>(rows);
    }

    public Task<IReadOnlyList<string>> ListPermissionCodesAsync(long userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var rows = _db.Ado.SqlQuery<string>(
            """
            SELECT DISTINCT p.code
            FROM sys_permission p
            INNER JOIN sys_role_permission rp ON rp.permission_id = p.id
            INNER JOIN sys_user_role ur ON ur.role_id = rp.role_id
            INNER JOIN sys_role r ON r.id = ur.role_id
            WHERE ur.user_id = @userId
              AND r.status = 'enabled'
            ORDER BY p.code
            """,
            new SugarParameter("@userId", userId));

        return Task.FromResult<IReadOnlyList<string>>(rows);
    }

    public Task RecordFailedLoginAsync(FailedLoginRecord record, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var insertedRows = _db.Ado.ExecuteCommand(
            """
            INSERT INTO sys_login_log (username, user_id, ip, user_agent, result, reason, created_at)
            VALUES (@username, NULL, @ip, @userAgent, 'failed', @reason, @createdAt)
            """,
            new SugarParameter("@username", record.Username),
            new SugarParameter("@ip", record.Ip),
            new SugarParameter("@userAgent", record.UserAgent),
            new SugarParameter("@reason", record.Reason),
            new SugarParameter("@createdAt", record.CreatedAt.UtcDateTime));

        if (insertedRows != 1)
        {
            throw new InvalidOperationException($"Expected to insert one failed login row, inserted {insertedRows}.");
        }

        return Task.CompletedTask;
    }

    public Task RecordSecurityEventAsync(SecurityEventRecord record, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var insertedRows = _db.Ado.ExecuteCommand(
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

        return Task.CompletedTask;
    }

    public Task CompleteSuccessfulLoginAsync(SuccessfulLoginRecord record, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var updatedRows = _db.Ado.ExecuteCommand(
            """
            UPDATE sys_user
            SET last_login_at = @lastLoginAt,
                last_login_ip = @lastLoginIp,
                updated_at = @updatedAt
            WHERE id = @userId
            """,
            new SugarParameter("@lastLoginAt", record.UpdatedAt.UtcDateTime),
            new SugarParameter("@lastLoginIp", record.Ip),
            new SugarParameter("@updatedAt", record.UpdatedAt.UtcDateTime),
            new SugarParameter("@userId", record.UserId));

        if (updatedRows != 1)
        {
            throw new InvalidOperationException($"Expected to update one user login row, updated {updatedRows}.");
        }

        var insertedRows = _db.Ado.ExecuteCommand(
            """
            INSERT INTO sys_refresh_token (user_id, token_hash, family_id, expires_at, revoked_at, replaced_by_token_hash, created_at)
            VALUES (@userId, @tokenHash, @familyId, @expiresAt, NULL, NULL, @createdAt)
            """,
            new SugarParameter("@userId", record.UserId),
            new SugarParameter("@tokenHash", record.RefreshTokenHash),
            new SugarParameter("@familyId", record.RefreshTokenFamilyId),
            new SugarParameter("@expiresAt", record.RefreshTokenExpiresAt.UtcDateTime),
            new SugarParameter("@createdAt", record.UpdatedAt.UtcDateTime));

        if (insertedRows != 1)
        {
            throw new InvalidOperationException($"Expected to insert one refresh token row, inserted {insertedRows}.");
        }

        return Task.CompletedTask;
    }

    public Task CompleteRefreshRotationAsync(RefreshRotationRecord record, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var revokedRows = _db.Ado.ExecuteCommand(
            """
            UPDATE sys_refresh_token
            SET revoked_at = @revokedAt,
                replaced_by_token_hash = @replacedByTokenHash
            WHERE id = @oldRefreshTokenId
              AND revoked_at IS NULL
            """,
            new SugarParameter("@revokedAt", record.RotatedAt.UtcDateTime),
            new SugarParameter("@replacedByTokenHash", record.NewRefreshTokenHash),
            new SugarParameter("@oldRefreshTokenId", record.OldRefreshTokenId));

        if (revokedRows != 1)
        {
            throw new RefreshTokenAlreadyRevokedException(record.FamilyId);
        }

        var insertedRows = _db.Ado.ExecuteCommand(
            """
            INSERT INTO sys_refresh_token (user_id, token_hash, family_id, expires_at, revoked_at, replaced_by_token_hash, created_at)
            VALUES (@userId, @tokenHash, @familyId, @expiresAt, NULL, NULL, @createdAt)
            """,
            new SugarParameter("@userId", record.UserId),
            new SugarParameter("@tokenHash", record.NewRefreshTokenHash),
            new SugarParameter("@familyId", record.FamilyId),
            new SugarParameter("@expiresAt", record.NewRefreshTokenExpiresAt.UtcDateTime),
            new SugarParameter("@createdAt", record.RotatedAt.UtcDateTime));

        if (insertedRows != 1)
        {
            throw new InvalidOperationException($"Expected to insert one rotated refresh token row, inserted {insertedRows}.");
        }

        return Task.CompletedTask;
    }

    public Task RevokeRefreshTokenFamilyAsync(string familyId, DateTimeOffset revokedAt, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var revokedRows = _db.Ado.ExecuteCommand(
            """
            UPDATE sys_refresh_token
            SET revoked_at = @revokedAt
            WHERE family_id = @familyId
              AND revoked_at IS NULL
            """,
            new SugarParameter("@revokedAt", revokedAt.UtcDateTime),
            new SugarParameter("@familyId", familyId));

        return Task.CompletedTask;
    }

    private sealed class AuthUserRow
    {
        public long Id { get; set; }

        public string Username { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public bool IsSuperAdmin { get; set; }

        public AuthUserRecord ToRecord()
        {
            return new AuthUserRecord(Id, Username, DisplayName, PasswordHash, Status, IsSuperAdmin);
        }
    }

}
