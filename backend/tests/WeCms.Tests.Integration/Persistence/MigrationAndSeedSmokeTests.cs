using WeCms.Persistence.Data;
using WeCms.Persistence.Migration;

namespace WeCms.Tests.Integration.Persistence;

public sealed class MigrationAndSeedSmokeTests
{
    [Fact]
    public async Task MigrationAndSeed_CanRunTwiceAgainstEmptyDatabase()
    {
        var baseConnectionString = RequiredConnectionString();
        var databaseName = $"wecms_m0_{Guid.NewGuid():N}";

        using var serverClient = new SqlSugarClientFactory(baseConnectionString).Create();
        serverClient.Ado.ExecuteCommand($"DROP DATABASE IF EXISTS `{databaseName}`");
        serverClient.Ado.ExecuteCommand($"CREATE DATABASE `{databaseName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci");

        try
        {
            using var db = new SqlSugarClientFactory(WithDatabase(baseConnectionString, databaseName)).Create();
            var migrationRunner = new DbMigrationRunner(db);
            var seedRunner = new SeedRunner(db);

            var firstMigrationRun = await migrationRunner.MigrateAsync(RepoPath("database", "migrations"));
            var secondMigrationRun = await migrationRunner.MigrateAsync(RepoPath("database", "migrations"));

            await seedRunner.SeedAsync(RepoPath("database", "seeds"), new SeedRunnerOptions("Development", null));
            await seedRunner.SeedAsync(RepoPath("database", "seeds"), new SeedRunnerOptions("Development", null));

            Assert.Equal(3, firstMigrationRun.Count);
            Assert.Empty(secondMigrationRun);
            Assert.Equal(3, Scalar<int>(db, "SELECT COUNT(1) FROM sys_schema_migration"));
            Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM sys_user WHERE username = 'admin'"));
            Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM sys_role WHERE code = 'super_admin'"));
            Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM sys_permission WHERE code = 'sys:system:secure-ping'"));
            Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM sys_user_role"));
            Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM sys_role_permission"));
        }
        finally
        {
            serverClient.Ado.ExecuteCommand($"DROP DATABASE IF EXISTS `{databaseName}`");
        }
    }

    [Fact]
    public async Task Migration_FailsWhenTargetTableExistsWithoutMigrationRecord()
    {
        var baseConnectionString = RequiredConnectionString();
        var databaseName = $"wecms_m0_{Guid.NewGuid():N}";

        using var serverClient = new SqlSugarClientFactory(baseConnectionString).Create();
        serverClient.Ado.ExecuteCommand($"DROP DATABASE IF EXISTS `{databaseName}`");
        serverClient.Ado.ExecuteCommand($"CREATE DATABASE `{databaseName}` CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci");

        try
        {
            using var db = new SqlSugarClientFactory(WithDatabase(baseConnectionString, databaseName)).Create();
            db.Ado.ExecuteCommand("CREATE TABLE sys_user (id BIGINT NOT NULL PRIMARY KEY) ENGINE=InnoDB");

            var migrationRunner = new DbMigrationRunner(db);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => migrationRunner.MigrateAsync(RepoPath("database", "migrations")));
        }
        finally
        {
            serverClient.Ado.ExecuteCommand($"DROP DATABASE IF EXISTS `{databaseName}`");
        }
    }

    private static T Scalar<T>(SqlSugar.ISqlSugarClient db, string sql)
    {
        var scalar = db.Ado.GetScalar(sql);

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
