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

    public async Task<AuthUserRecord?> FindUserByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var record = await _db.Ado.SqlQuerySingleAsync<AuthUserRow>(
            """
            SELECT id AS Id,
                   username AS Username,
                   display_name AS DisplayName,
                   password_hash AS PasswordHash,
                   status AS Status,
                   is_super_admin AS IsSuperAdmin,
                   must_change_password AS MustChangePassword,
                   security_stamp AS SecurityStamp
            FROM sys_user
            WHERE username = @username
              AND deleted_at IS NULL
            LIMIT 1
            """,
            new SugarParameter("@username", username));

        return record?.ToRecord();
    }

    public async Task<AuthUserRecord?> FindUserByIdAsync(long userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var record = await _db.Ado.SqlQuerySingleAsync<AuthUserRow>(
            """
            SELECT id AS Id,
                   username AS Username,
                   display_name AS DisplayName,
                   password_hash AS PasswordHash,
                   status AS Status,
                   is_super_admin AS IsSuperAdmin,
                   must_change_password AS MustChangePassword,
                   security_stamp AS SecurityStamp
            FROM sys_user
            WHERE id = @userId
              AND deleted_at IS NULL
            LIMIT 1
            """,
            new SugarParameter("@userId", userId));

        return record?.ToRecord();
    }

    public async Task<RefreshTokenRecord?> FindRefreshTokenByHashAsync(string tokenHash, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var table = await _db.Ado.GetDataTableAsync(
            """
            SELECT rt.id,
                   rt.user_id,
                   u.username,
                   u.display_name,
                   u.status,
                   u.is_super_admin,
                   u.must_change_password AS MustChangePassword,
                   rt.token_hash,
                   rt.family_id,
                   u.security_stamp AS SecurityStamp,
                   rt.expires_at,
                   rt.revoked_at,
                   rt.replaced_by_token_hash
            FROM sys_refresh_token rt
            INNER JOIN sys_user u ON u.id = rt.user_id
            WHERE rt.token_hash = @tokenHash
              AND u.deleted_at IS NULL
            LIMIT 1
            """,
            new SugarParameter("@tokenHash", tokenHash));

        if (table.Rows.Count == 0)
        {
            return null;
        }

        var row = table.Rows[0];
        var revokedAt = row["revoked_at"] == DBNull.Value
            ? (DateTimeOffset?)null
            : new DateTimeOffset(DateTime.SpecifyKind((DateTime)row["revoked_at"], DateTimeKind.Utc));

        return new RefreshTokenRecord(
            Convert.ToInt64(row["id"], global::System.Globalization.CultureInfo.InvariantCulture),
            Convert.ToInt64(row["user_id"], global::System.Globalization.CultureInfo.InvariantCulture),
            Convert.ToString(row["username"], global::System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
            Convert.ToString(row["display_name"], global::System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
            Convert.ToString(row["status"], global::System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
            Convert.ToBoolean(row["is_super_admin"], global::System.Globalization.CultureInfo.InvariantCulture),
            Convert.ToString(row["token_hash"], global::System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
            Convert.ToString(row["family_id"], global::System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty,
            new DateTimeOffset(DateTime.SpecifyKind((DateTime)row["expires_at"], DateTimeKind.Utc)),
            revokedAt,
            row["replaced_by_token_hash"] == DBNull.Value
                ? null
                : Convert.ToString(row["replaced_by_token_hash"], global::System.Globalization.CultureInfo.InvariantCulture),
            Convert.ToBoolean(row["MustChangePassword"], global::System.Globalization.CultureInfo.InvariantCulture),
            Convert.ToString(row["SecurityStamp"], global::System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty);
    }

    public async Task<IReadOnlyList<string>> ListRoleCodesAsync(long userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var rows = await _db.Ado.SqlQueryAsync<string>(
            """
            SELECT r.code
            FROM sys_role r
            INNER JOIN sys_user_role ur ON ur.role_id = r.id
            WHERE ur.user_id = @userId
              AND r.status = 'enabled'
            ORDER BY r.code
            """,
            new SugarParameter("@userId", userId));

        return rows;
    }

    public async Task<IReadOnlyList<string>> ListPermissionCodesAsync(long userId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var rows = await _db.Ado.SqlQueryAsync<string>(
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

        return rows;
    }

    public async Task RecordFailedLoginAsync(FailedLoginRecord record, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var insertedRows = await _db.Ado.ExecuteCommandAsync(
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
    }

    public async Task RecordSecurityEventAsync(SecurityEventRecord record, CancellationToken cancellationToken)
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

    public async Task RecordAuditLogAsync(AuditLogRecord record, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var insertedRows = await _db.Ado.ExecuteCommandAsync(
            """
            INSERT INTO sys_audit_log (user_id, username, module, resource, action, target_id, request_method, request_path, ip_address, user_agent, trace_id, result, detail, created_at)
            VALUES (@userId, @username, @module, @resource, @action, @targetId, @requestMethod, @requestPath, @ipAddress, @userAgent, @traceId, @result, @detail, @createdAt)
            """,
            new SugarParameter("@userId", record.UserId),
            new SugarParameter("@username", record.Username),
            new SugarParameter("@module", record.Module),
            new SugarParameter("@resource", record.Resource),
            new SugarParameter("@action", record.Action),
            new SugarParameter("@targetId", record.TargetId),
            new SugarParameter("@requestMethod", record.RequestMethod),
            new SugarParameter("@requestPath", record.RequestPath),
            new SugarParameter("@ipAddress", record.Ip),
            new SugarParameter("@userAgent", record.UserAgent),
            new SugarParameter("@traceId", record.TraceId),
            new SugarParameter("@result", record.Result),
            new SugarParameter("@detail", record.Detail),
            new SugarParameter("@createdAt", record.CreatedAt.UtcDateTime));

        if (insertedRows != 1)
        {
            throw new InvalidOperationException($"Expected to insert one audit log row, inserted {insertedRows}.");
        }
    }

    public async Task CompleteSuccessfulLoginAsync(SuccessfulLoginRecord record, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var updatedRows = await _db.Ado.ExecuteCommandAsync(
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

        var insertedRows = await _db.Ado.ExecuteCommandAsync(
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
    }

    public async Task CompleteRefreshRotationAsync(RefreshRotationRecord record, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var revokedRows = await _db.Ado.ExecuteCommandAsync(
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

        var insertedRows = await _db.Ado.ExecuteCommandAsync(
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
    }

    public async Task RevokeRefreshTokenFamilyAsync(string familyId, DateTimeOffset revokedAt, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        await _db.Ado.ExecuteCommandAsync(
            """
            UPDATE sys_refresh_token
            SET revoked_at = @revokedAt
            WHERE family_id = @familyId
              AND revoked_at IS NULL
            """,
            new SugarParameter("@revokedAt", revokedAt.UtcDateTime),
            new SugarParameter("@familyId", familyId));
    }

    private sealed class AuthUserRow
    {
        public long Id { get; set; }

        public string Username { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public bool IsSuperAdmin { get; set; }

        public bool MustChangePassword { get; set; }

        public string SecurityStamp { get; set; } = string.Empty;

        public AuthUserRecord ToRecord()
        {
            return new AuthUserRecord(Id, Username, DisplayName, PasswordHash, Status, IsSuperAdmin, MustChangePassword, SecurityStamp);
        }
    }
}
