using SqlSugar;
using WeCms.Modules.Security;
using WeCms.Modules.Security.SqlSugar.Repositories;
using WeCms.Data.SqlSugar;
using WeCms.Shared.Security;

namespace WeCms.Tests.Integration.Security;

[Collection(nameof(SharedMySqlCollection))]
public sealed class SecurityBanRepositoryTests : PerTestDatabaseResetBase
{
    [DbFact]
    public async Task FindActiveAsync_IgnoresExpiredAndRevokedBans()
    {
        var connectionString = IntegrationTestDatabase.GetConnectionString();
        using var db = new SqlSugarClientFactory(connectionString).Create();
        await PrepareDatabaseAsync(db);
        var repository = new SecurityBanRepository(db, new SecurityEventClassifier());
        var now = new DateTimeOffset(2026, 6, 18, 0, 0, 0, TimeSpan.Zero);

        await InsertBanAsync(db, SecurityBanTypes.Ip, "192.168.1.10", "warning", now.AddMinutes(-10), null);
        await InsertBanAsync(db, SecurityBanTypes.Ip, "192.168.1.11", "warning", now.AddMinutes(10), now.AddMinutes(-1));
        var activeId = await InsertBanAsync(db, SecurityBanTypes.Ip, "192.168.1.12", "warning", now.AddMinutes(10), null);

        Assert.Null(await repository.FindActiveAsync(SecurityBanTypes.Ip, "192.168.1.10", now, CancellationToken.None));
        Assert.Null(await repository.FindActiveAsync(SecurityBanTypes.Ip, "192.168.1.11", now, CancellationToken.None));

        var active = await repository.FindActiveAsync(SecurityBanTypes.Ip, "192.168.1.12", now, CancellationToken.None);
        Assert.NotNull(active);
        Assert.Equal(activeId, active.Id);
    }

    [DbFact]
    public async Task ListStatusDetailAndRevokeAsync_UseSecurityBanSchema()
    {
        var connectionString = IntegrationTestDatabase.GetConnectionString();
        using var db = new SqlSugarClientFactory(connectionString).Create();
        await PrepareDatabaseAsync(db);
        var repository = new SecurityBanRepository(db, new SecurityEventClassifier());
        var now = new DateTimeOffset(2026, 6, 18, 0, 0, 0, TimeSpan.Zero);

        var expiredId = await InsertBanAsync(db, SecurityBanTypes.Ip, "192.168.1.10", "warning", now.AddMinutes(-10), null);
        var userBanId = await InsertBanAsync(db, SecurityBanTypes.User, "42", "critical", now.AddMinutes(30), null);
        var operatorUserId = await InsertUserAsync(db, "security-operator", false);

        var status = await repository.GetStatusAsync(now, CancellationToken.None);
        Assert.Equal(1, status.ActiveBans);
        Assert.Equal(0, status.ActiveIpBans);
        Assert.Equal(1, status.ActiveUserBans);
        Assert.Equal(1, status.CriticalActiveBans);

        var page = await repository.ListAsync(new SecurityBanListCriteria(1, 20, null, null, null, null, true, now), CancellationToken.None);
        Assert.Equal(1, page.Total);
        Assert.Equal(userBanId, page.Records.Single().Id);

        var all = await repository.ListAsync(new SecurityBanListCriteria(1, 20, null, null, null, null, false, now), CancellationToken.None);
        Assert.Equal(2, all.Total);
        Assert.Contains(all.Records, row => row.Id == expiredId);

        var detail = await repository.GetAsync(userBanId, CancellationToken.None);
        Assert.NotNull(detail);
        Assert.Equal("42", detail.Target);

        await repository.RevokeAsync(new SecurityBanRevokeRecord(userBanId, operatorUserId, "reviewed", now), CancellationToken.None);

        var revoked = await repository.GetAsync(userBanId, CancellationToken.None);
        Assert.NotNull(revoked);
        Assert.Equal("reviewed", revoked.RevokeReason);
        Assert.NotNull(revoked.RevokedAt);
    }

    [DbFact]
    public async Task AuditSecurityEventAndAdminRoleLookupAsync_WriteExpectedRows()
    {
        var connectionString = IntegrationTestDatabase.GetConnectionString();
        using var db = new SqlSugarClientFactory(connectionString).Create();
        await PrepareDatabaseAsync(db);
        var repository = new SecurityBanRepository(db, new SecurityEventClassifier());
        var now = new DateTimeOffset(2026, 6, 18, 0, 0, 0, TimeSpan.Zero);

        var userId = await InsertUserAsync(db, "security-admin", assignAdminRole: true);

        Assert.True(await repository.UserHasRoleCodeAsync(userId, "super_admin", CancellationToken.None));

        await repository.RecordAuditAsync(
            new SecurityBanAuditRecord(userId, "security-admin", "unban", 7, "127.0.0.1", "integration", "trace", "success", "done", now),
            CancellationToken.None);
        await repository.RecordSecurityEventAsync(
            new SecurityBanSecurityEventRecord("security.ban_unbanned", userId, "security-admin", "127.0.0.1", "critical", "done", now),
            CancellationToken.None);

        Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM sys_audit_log WHERE resource = 'security-ban' AND action = 'unban'"));
        Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM sys_security_event WHERE event_type = 'security.ban_unbanned'"));
    }

    private static async Task<long> InsertBanAsync(
        ISqlSugarClient db,
        string banType,
        string target,
        string severity,
        DateTimeOffset? expiresAt,
        DateTimeOffset? revokedAt)
    {
        await db.Ado.ExecuteCommandAsync(
            """
            INSERT INTO sys_security_ban (ban_type, target, reason, severity, source, expires_at, revoked_at, revoked_by, revoke_reason, created_at, updated_at)
            VALUES (@banType, @target, 'test', @severity, 'unit', @expiresAt, @revokedAt, NULL, NULL, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6))
            """,
            new SugarParameter("@banType", banType),
            new SugarParameter("@target", target),
            new SugarParameter("@severity", severity),
            new SugarParameter("@expiresAt", expiresAt?.UtcDateTime),
            new SugarParameter("@revokedAt", revokedAt?.UtcDateTime));

        return Convert.ToInt64(
            await db.Ado.GetScalarAsync("SELECT id FROM sys_security_ban WHERE target = @target ORDER BY id DESC LIMIT 1", new SugarParameter("@target", target)),
            global::System.Globalization.CultureInfo.InvariantCulture);
    }

    private static T Scalar<T>(ISqlSugarClient db, string sql)
    {
        return (T)Convert.ChangeType(db.Ado.GetScalar(sql), typeof(T), global::System.Globalization.CultureInfo.InvariantCulture);
    }

    private static async Task<long> InsertUserAsync(ISqlSugarClient db, string username, bool assignAdminRole)
    {
        await db.Ado.ExecuteCommandAsync(
            """
            INSERT INTO sys_user (username, display_name, password_hash, status, must_change_password, security_stamp, permission_version, created_at, updated_at, deleted_at)
            VALUES (@username, @displayName, 'x', 'enabled', FALSE, 'stamp', 0, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6), NULL)
            """,
            new SugarParameter("@username", username),
            new SugarParameter("@displayName", username));

        var userId = Convert.ToInt64(
            await db.Ado.GetScalarAsync("SELECT id FROM sys_user WHERE username = @username", new SugarParameter("@username", username)),
            global::System.Globalization.CultureInfo.InvariantCulture);
        if (assignAdminRole)
        {
            await db.Ado.ExecuteCommandAsync(
                """
                INSERT INTO sys_user_role (user_id, role_id, created_at)
                SELECT @userId, r.id, UTC_TIMESTAMP(6)
                FROM sys_role r
                WHERE r.code = 'super_admin'
                  AND r.deleted_at IS NULL
                """,
                new SugarParameter("@userId", userId));
        }

        return userId;
    }

    private static string RepoPath(params string[] segments)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "backend", "WeCms.slnx")))
            {
                return Path.Combine([directory.FullName, .. segments]);
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
