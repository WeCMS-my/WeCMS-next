using SqlSugar;
using WeCms.Modules.AccessControl.Permissions;
using WeCms.Modules.Platform.Permissions;
using WeCms.Modules.AccessControl.Records;
using WeCms.Modules.AccessControl.SqlSugar.Repositories;
using WeCms.Data.SqlSugar;
using WeCms.Shared.Security;
using WeCms.Tests.Integration;

namespace WeCms.Tests.Integration.Permissions;

[Collection(nameof(SharedMySqlCollection))]
public sealed class PermissionRepositoryTests : PerTestDatabaseResetBase
{
    [DbFact]
    public async Task UserHasPermissionAsync_UsesPersistedRolePermissionAssignments()
    {
        var baseConnectionString = RequiredConnectionString();


        try
        {
            using var db = new SqlSugarClientFactory(baseConnectionString).Create();
            await PrepareDatabaseWithSeedsAsync(db);

            var repository = new PermissionRepository(db);
            var adminUserId = Scalar<long>(db, "SELECT id FROM sys_user WHERE username = 'admin' LIMIT 1");

            var user = await repository.FindUserAsync(adminUserId, CancellationToken.None);
            var hasSecurePing = await repository.UserHasPermissionAsync(
                adminUserId,
                PlatformPermissions.SecurePing,
                CancellationToken.None);
            var hasMissingPermission = await repository.UserHasPermissionAsync(
                adminUserId,
                "sys:system:missing",
                CancellationToken.None);

            Assert.NotNull(user);
            Assert.Equal("enabled", user.Status);
            Assert.True(hasSecurePing);
            Assert.False(hasMissingPermission);
        }
        finally
        {
        }
    }
    [DbFact]
    public async Task PermissionChecker_ReturnsUserDisabledFromPersistedUserStatus()
    {
        var baseConnectionString = RequiredConnectionString();


        try
        {
            using var db = new SqlSugarClientFactory(baseConnectionString).Create();
            await PrepareDatabaseWithSeedsAsync(db);

            var adminUserId = Scalar<long>(db, "SELECT id FROM sys_user WHERE username = 'admin' LIMIT 1");
            db.Ado.ExecuteCommand(
                "UPDATE sys_user SET status = 'disabled' WHERE id = @userId",
                new SugarParameter("@userId", adminUserId));

            var checker = new PermissionChecker(new PermissionRepository(db));

            var result = await checker.CheckAsync(adminUserId, PlatformPermissions.SecurePing, CancellationToken.None);

            Assert.Equal(PermissionCheckResult.UserDisabled, result);
        }
        finally
        {
        }
    }
    [DbFact]
    public async Task PermissionChecker_ReturnsUserDisabledFromPersistedUserSoftDelete()
    {
        var baseConnectionString = RequiredConnectionString();


        try
        {
            using var db = new SqlSugarClientFactory(baseConnectionString).Create();
            await PrepareDatabaseWithSeedsAsync(db);

            var checker = new PermissionChecker(new PermissionRepository(db));
            var adminUserId = Scalar<long>(db, "SELECT id FROM sys_user WHERE username = 'admin' LIMIT 1");
            db.Ado.ExecuteCommand(
                "UPDATE sys_user SET deleted_at = @deletedAt WHERE id = @userId",
                new SugarParameter("@deletedAt", DateTime.UtcNow),
                new SugarParameter("@userId", adminUserId));

            var result = await checker.CheckAsync(adminUserId, PlatformPermissions.SecurePing, CancellationToken.None);

            Assert.Equal(PermissionCheckResult.UserDisabled, result);
        }
        finally
        {
        }
    }

    [DbFact]
    public async Task PermissionSecurityEventRepository_RecordAsync_WritesClassifiedSecurityEvent()
    {
        var baseConnectionString = RequiredConnectionString();


        try
        {
            using var db = new SqlSugarClientFactory(baseConnectionString).Create();
            await PrepareDatabaseWithSeedsAsync(db);

            var repository = new PermissionSecurityEventRepository(db, new SecurityEventClassifier());
            var now = new DateTimeOffset(2026, 6, 19, 0, 0, 0, TimeSpan.Zero);

            await repository.RecordAsync(
                new PermissionSecurityEventRecord(
                    "permission_denied",
                    1,
                    "admin",
                    "192.168.101.199",
                    "Permission denied. Required permission: sys:user:delete.",
                    now,
                    "trace-permission"),
                CancellationToken.None);

            Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM sys_security_event WHERE event_type = 'permission_denied'"));
            Assert.Equal("warning", Scalar<string>(db, "SELECT severity FROM sys_security_event WHERE trace_id = 'trace-permission' LIMIT 1"));
            Assert.Equal("permission", Scalar<string>(db, "SELECT source FROM sys_security_event WHERE trace_id = 'trace-permission' LIMIT 1"));
        }
        finally
        {
        }
    }

    private static T Scalar<T>(ISqlSugarClient db, string sql, params SugarParameter[] parameters)
    {
        var scalar = db.Ado.GetScalar(sql, parameters);

        return scalar is T value
            ? value
            : (T)Convert.ChangeType(scalar, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
    }
    private static string RequiredConnectionString()
    {
        return IntegrationTestDatabase.GetConnectionString();
    }
    private static string RepoPath(params string[] segments)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "database"))
                && File.Exists(Path.Combine(directory.FullName, "backend", "WeCms.slnx")))
            {
                return Path.Combine([directory.FullName, .. segments]);
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
