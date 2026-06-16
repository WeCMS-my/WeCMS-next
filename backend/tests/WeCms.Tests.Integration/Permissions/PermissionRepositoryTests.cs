using SqlSugar;
using WeCms.Modules.System.Permissions;
using WeCms.Persistence.Data;
using WeCms.Persistence.Migration;
using WeCms.Persistence.Modules.System.Permissions;

namespace WeCms.Tests.Integration.Permissions;

public sealed class PermissionRepositoryTests
{
    [Fact]
    public async Task UserHasPermissionAsync_UsesPersistedRolePermissionAssignments()
    {
        var baseConnectionString = RequiredConnectionString();
        var databaseName = $"wecms_permission_{Guid.NewGuid():N}";

        using var serverClient = new SqlSugarClientFactory(baseConnectionString).Create();
        serverClient.Ado.ExecuteCommand($"DROP DATABASE IF EXISTS `{databaseName}`");
        serverClient.Ado.ExecuteCommand($"CREATE DATABASE `{databaseName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci");

        try
        {
            using var db = new SqlSugarClientFactory(WithDatabase(baseConnectionString, databaseName)).Create();
            await new DbMigrationRunner(db).MigrateAsync(RepoPath("database", "migrations"));
            await new SeedRunner(db).SeedAsync(RepoPath("database", "seeds"), new SeedRunnerOptions("Development", null));

            var repository = new PermissionRepository(db);
            var adminUserId = Scalar<long>(db, "SELECT id FROM sys_user WHERE username = 'admin' LIMIT 1");

            var user = await repository.FindUserAsync(adminUserId, CancellationToken.None);
            var hasSecurePing = await repository.UserHasPermissionAsync(
                adminUserId,
                SystemPermissions.SecurePing,
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
            serverClient.Ado.ExecuteCommand($"DROP DATABASE IF EXISTS `{databaseName}`");
        }
    }

    [Fact]
    public async Task PermissionChecker_ReturnsUserDisabledFromPersistedUserStatus()
    {
        var baseConnectionString = RequiredConnectionString();
        var databaseName = $"wecms_permission_disabled_{Guid.NewGuid():N}";

        using var serverClient = new SqlSugarClientFactory(baseConnectionString).Create();
        serverClient.Ado.ExecuteCommand($"DROP DATABASE IF EXISTS `{databaseName}`");
        serverClient.Ado.ExecuteCommand($"CREATE DATABASE `{databaseName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci");

        try
        {
            using var db = new SqlSugarClientFactory(WithDatabase(baseConnectionString, databaseName)).Create();
            await new DbMigrationRunner(db).MigrateAsync(RepoPath("database", "migrations"));
            await new SeedRunner(db).SeedAsync(RepoPath("database", "seeds"), new SeedRunnerOptions("Development", null));

            var adminUserId = Scalar<long>(db, "SELECT id FROM sys_user WHERE username = 'admin' LIMIT 1");
            db.Ado.ExecuteCommand(
                "UPDATE sys_user SET status = 'disabled' WHERE id = @userId",
                new SugarParameter("@userId", adminUserId));

            var checker = new PermissionChecker(new PermissionRepository(db));

            var result = await checker.CheckAsync(adminUserId, SystemPermissions.SecurePing, CancellationToken.None);

            Assert.Equal(PermissionCheckResult.UserDisabled, result);
        }
        finally
        {
            serverClient.Ado.ExecuteCommand($"DROP DATABASE IF EXISTS `{databaseName}`");
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
        var connectionString = Environment.GetEnvironmentVariable("WECMS_TEST_MYSQL_CONNECTION_STRING");
        Assert.False(
            string.IsNullOrWhiteSpace(connectionString),
            "Set WECMS_TEST_MYSQL_CONNECTION_STRING to run MySQL integration tests.");

        return connectionString;
    }

    private static string WithDatabase(string connectionString, string databaseName)
    {
        var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(part => !part.StartsWith("database=", StringComparison.OrdinalIgnoreCase)
                && !part.StartsWith("initial catalog=", StringComparison.OrdinalIgnoreCase))
            .Append($"database={databaseName}");

        return string.Join(';', parts);
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
