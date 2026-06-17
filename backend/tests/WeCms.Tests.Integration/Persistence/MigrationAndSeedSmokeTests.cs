using WeCms.Persistence.Data;
using WeCms.Persistence.Migration;
using WeCms.Tests.Integration;

namespace WeCms.Tests.Integration.Persistence;

public sealed class MigrationAndSeedSmokeTests : global::Xunit.IAsyncLifetime
{

    public Task InitializeAsync()
    {
        return IntegrationTestDatabase.ResetDatabaseAsync(RequiredConnectionString());
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [DbFact]
    public async Task MigrationAndSeed_CanRunTwiceAgainstEmptyDatabase()
    {
        var baseConnectionString = RequiredConnectionString();


        try
        {
            using var db = new SqlSugarClientFactory(baseConnectionString).Create();
            var migrationRunner = new DbMigrationRunner(db);
            var seedRunner = new SeedRunner(db);

            var firstMigrationRun = await migrationRunner.MigrateAsync(RepoPath("database", "migrations"));
            var secondMigrationRun = await migrationRunner.MigrateAsync(RepoPath("database", "migrations"));

            await seedRunner.SeedAsync(RepoPath("database", "seeds"), new SeedRunnerOptions("Development", null));
            await seedRunner.SeedAsync(RepoPath("database", "seeds"), new SeedRunnerOptions("Development", null));

            Assert.Equal(12, firstMigrationRun.Count);
            Assert.Empty(secondMigrationRun);
            Assert.Equal(12, Scalar<int>(db, "SELECT COUNT(1) FROM sys_schema_migration"));
            Assert.Equal(0, Scalar<int>(db, "SELECT COUNT(1) FROM sys_audit_log"));
            Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM sys_user WHERE username = 'admin'"));
            Assert.Equal(0, Scalar<int>(db, "SELECT COUNT(1) FROM sys_user WHERE username = 'admin' AND must_change_password = TRUE"));
            Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM sys_role WHERE code = 'super_admin'"));
            Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM sys_role WHERE code = 'super_admin' AND is_builtin = TRUE AND deleted_at IS NULL"));
            Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM sys_role WHERE code = 'super_admin' AND is_builtin = TRUE AND is_locked = TRUE AND status = 'enabled' AND deleted_at IS NULL"));
            Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM sys_user u JOIN sys_user_role ur ON ur.user_id = u.id JOIN sys_role r ON r.id = ur.role_id WHERE u.username = 'admin' AND u.status = 'enabled' AND u.deleted_at IS NULL AND r.code = 'super_admin' AND r.is_locked = TRUE AND r.deleted_at IS NULL"));
            Assert.Equal(0, Scalar<int>(db, "SELECT COUNT(1) FROM sys_role r WHERE r.is_locked = TRUE AND r.deleted_at IS NULL AND NOT EXISTS (SELECT 1 FROM sys_user_role ur JOIN sys_user u ON u.id = ur.user_id WHERE ur.role_id = r.id AND u.status = 'enabled' AND u.deleted_at IS NULL)"));
            Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = 'sys_role' AND column_name = 'is_locked' AND is_nullable = 'NO' AND column_default = '0'"));
            Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = 'sys_role_menu'"));
            Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM sys_permission WHERE code = 'sys:system:secure-ping' AND status = 'enabled' AND is_builtin = TRUE AND deleted_at IS NULL"));
            Assert.Equal(84, Scalar<int>(db, "SELECT COUNT(1) FROM sys_permission WHERE code LIKE 'sys:%' AND code <> 'sys:system:secure-ping'"));
            Assert.Equal(85, Scalar<int>(db, "SELECT COUNT(1) FROM sys_permission WHERE code LIKE 'sys:%' AND status = 'enabled' AND is_builtin = TRUE AND deleted_at IS NULL"));
            Assert.Equal(13, Scalar<int>(db, "SELECT COUNT(1) FROM sys_menu WHERE name LIKE 'sys.%'"));
            Assert.Equal(13, Scalar<int>(db, "SELECT COUNT(1) FROM sys_menu WHERE name LIKE 'sys.%' AND is_builtin = TRUE AND deleted_at IS NULL"));
            Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = 'sys_dict_type'"));
            Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = 'sys_dict_value'"));
            Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = 'sys_setting'"));
            Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM sys_setting WHERE `key` = 'security.passwordPepper' AND is_sensitive = TRUE"));
            Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = 'sys_file'"));
            Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM sys_user_role"));
            Assert.Equal(
                Scalar<int>(db, "SELECT COUNT(1) FROM sys_permission"),
                Scalar<int>(
                    db,
                    """
                    SELECT COUNT(1)
                    FROM sys_role_permission rp
                    JOIN sys_role r ON r.id = rp.role_id
                    WHERE r.code = 'super_admin'
                    """));
        }
        finally
        {
        }
    }
    [DbFact]
    public async Task Migration_FailsWhenTargetTableExistsWithoutMigrationRecord()
    {
        var baseConnectionString = RequiredConnectionString();


        try
        {
            using var db = new SqlSugarClientFactory(baseConnectionString).Create();
            db.Ado.ExecuteCommand("CREATE TABLE sys_user (id BIGINT NOT NULL PRIMARY KEY) ENGINE=InnoDB");

            var migrationRunner = new DbMigrationRunner(db);

            await Assert.ThrowsAsync<InvalidOperationException>(
                () => migrationRunner.MigrateAsync(RepoPath("database", "migrations")));
        }
        finally
        {
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
