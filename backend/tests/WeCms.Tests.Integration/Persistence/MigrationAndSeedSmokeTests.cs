using Microsoft.Extensions.DependencyInjection;
using WeCms.Data.SqlSugar;
using WeCms.Modules.AccessControl.SqlSugar;
using WeCms.Modules.Audit.SqlSugar;
using WeCms.Modules.Configuration.SqlSugar;
using WeCms.Modules.FileCenter.SqlSugar;
using WeCms.Modules.Identity.SqlSugar;
using WeCms.Modules.Organization.SqlSugar;
using WeCms.Modules.Platform.SqlSugar;
using WeCms.Modules.Security.SqlSugar;
using WeCms.Tests.Integration;

namespace WeCms.Tests.Integration.Persistence;

[Collection(nameof(SharedMySqlCollection))]
public sealed class MigrationAndSeedSmokeTests : PerTestDatabaseResetBase
{
    [DbFact]
    public async Task DbMigrationRunner_AppliesNewMigration()
    {
        using var db = new SqlSugarClientFactory(RequiredConnectionString()).Create();
        var migrationsDirectory = Directory.CreateTempSubdirectory("wecms-migrations-");

        try
        {
            await File.WriteAllTextAsync(
                Path.Combine(migrationsDirectory.FullName, "000001_create_s3_t05_probe.sql"),
                "CREATE TABLE s3_t05_probe (id BIGINT NOT NULL PRIMARY KEY);",
                TestContext.Current.CancellationToken);

            var applied = await new DbMigrationRunner(db).MigrateAsync(migrationsDirectory.FullName, TestContext.Current.CancellationToken);

            Assert.Equal(["000001_create_s3_t05_probe"], applied);
            Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = 's3_t05_probe'"));
        }
        finally
        {
            migrationsDirectory.Delete(recursive: true);
        }
    }

    [DbFact]
    public async Task DbMigrationRunner_SkipsAppliedMigration()
    {
        using var db = new SqlSugarClientFactory(RequiredConnectionString()).Create();
        var migrationsDirectory = Directory.CreateTempSubdirectory("wecms-migrations-");

        try
        {
            await File.WriteAllTextAsync(
                Path.Combine(migrationsDirectory.FullName, "000001_create_s3_t05_probe.sql"),
                "CREATE TABLE s3_t05_probe (id BIGINT NOT NULL PRIMARY KEY);",
                TestContext.Current.CancellationToken);
            var runner = new DbMigrationRunner(db);

            await runner.MigrateAsync(migrationsDirectory.FullName, TestContext.Current.CancellationToken);
            var applied = await runner.MigrateAsync(migrationsDirectory.FullName, TestContext.Current.CancellationToken);

            Assert.Empty(applied);
        }
        finally
        {
            migrationsDirectory.Delete(recursive: true);
        }
    }

    [DbFact]
    public async Task DbMigrationRunner_FailsOnChecksumDrift()
    {
        using var db = new SqlSugarClientFactory(RequiredConnectionString()).Create();
        var migrationsDirectory = Directory.CreateTempSubdirectory("wecms-migrations-");
        var migrationFile = Path.Combine(migrationsDirectory.FullName, "000001_create_s3_t05_probe.sql");

        try
        {
            await File.WriteAllTextAsync(
                migrationFile,
                "CREATE TABLE s3_t05_probe (id BIGINT NOT NULL PRIMARY KEY);",
                TestContext.Current.CancellationToken);
            var runner = new DbMigrationRunner(db);
            await runner.MigrateAsync(migrationsDirectory.FullName, TestContext.Current.CancellationToken);
            await File.WriteAllTextAsync(
                migrationFile,
                "CREATE TABLE s3_t05_probe (id BIGINT NOT NULL PRIMARY KEY, code VARCHAR(32) NULL);",
                TestContext.Current.CancellationToken);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => runner.MigrateAsync(migrationsDirectory.FullName, TestContext.Current.CancellationToken));

            Assert.Equal("Migration checksum drift detected for 000001_create_s3_t05_probe.", exception.Message);
        }
        finally
        {
            migrationsDirectory.Delete(recursive: true);
        }
    }

    [DbFact]
    public async Task SeedRunner_ReplacesAdminPasswordHashSafely()
    {
        using var db = new SqlSugarClientFactory(RequiredConnectionString()).Create();
        var seedsDirectory = Directory.CreateTempSubdirectory("wecms-seeds-");

        try
        {
            db.Ado.ExecuteCommand("CREATE TABLE s3_t05_seed_probe (password_hash VARCHAR(255) NOT NULL, must_change_password BOOLEAN NOT NULL)");
            await File.WriteAllTextAsync(
                Path.Combine(seedsDirectory.FullName, "000001_seed_probe.sql"),
                "INSERT INTO s3_t05_seed_probe (password_hash, must_change_password) VALUES ('{{ADMIN_PASSWORD_HASH}}', {{ADMIN_MUST_CHANGE_PASSWORD}});",
                TestContext.Current.CancellationToken);

            await new SeedRunner(db).SeedAsync(
                seedsDirectory.FullName,
                new SeedRunnerOptions("Production", "AdminRotation123!"),
                TestContext.Current.CancellationToken);

            var passwordHash = Scalar<string>(db, "SELECT password_hash FROM s3_t05_seed_probe LIMIT 1");
            Assert.StartsWith("wecms.pbkdf2-sha256.v1.600000.", passwordHash, StringComparison.Ordinal);
            Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM s3_t05_seed_probe WHERE must_change_password = TRUE"));
            Assert.Equal(0, Scalar<int>(db, "SELECT COUNT(1) FROM s3_t05_seed_probe WHERE password_hash LIKE '%{{%'"));
        }
        finally
        {
            seedsDirectory.Delete(recursive: true);
        }
    }

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

            Assert.Single(firstMigrationRun);
            Assert.Empty(secondMigrationRun);
            Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM sys_schema_migration"));
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
            Assert.Equal(104, Scalar<int>(db, "SELECT COUNT(1) FROM sys_permission WHERE code LIKE 'sys:%' AND code <> 'sys:system:secure-ping'"));
            Assert.Equal(105, Scalar<int>(db, "SELECT COUNT(1) FROM sys_permission WHERE code LIKE 'sys:%' AND status = 'enabled' AND is_builtin = TRUE AND deleted_at IS NULL"));
            Assert.Equal(15, Scalar<int>(db, "SELECT COUNT(1) FROM sys_menu WHERE name LIKE 'sys.%'"));
            Assert.Equal(15, Scalar<int>(db, "SELECT COUNT(1) FROM sys_menu WHERE name LIKE 'sys.%' AND is_builtin = TRUE AND deleted_at IS NULL"));
            Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = 'sys_dict_type'"));
            Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = 'sys_dict_value'"));
            Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = 'sys_setting'"));
            Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM sys_setting WHERE `key` = 'security.passwordPepper' AND is_sensitive = TRUE"));
            Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = 'sys_file'"));
            Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = 'sys_i18n_message'"));
            Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = 'sys_security_ban'"));
            Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = 'sys_login_failure_counter'"));
            Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = 'sys_security_event' AND column_name = 'source' AND is_nullable = 'NO'"));
            Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = 'sys_security_event' AND column_name = 'trace_id' AND is_nullable = 'NO'"));
            Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = 'sys_user_two_factor'"));
            Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = 'sys_user_two_factor' AND column_name = 'last_totp_step' AND is_nullable = 'YES'"));
            Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = 'sys_auth_challenge'"));
            Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = 'sys_user' AND column_name = 'avatar_object_key' AND is_nullable = 'YES'"));
            Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = 'sys_user' AND column_name = 'avatar_mime_type' AND is_nullable = 'YES'"));
            Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM information_schema.columns WHERE table_schema = DATABASE() AND table_name = 'sys_user' AND column_name = 'avatar_file_ext' AND is_nullable = 'YES'"));
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
    public async Task SchemaValidator_PassesAgainstSprint9Baseline()
    {
        var baseConnectionString = RequiredConnectionString();

        using var db = new SqlSugarClientFactory(baseConnectionString).Create();
        var migrationRunner = new DbMigrationRunner(db);

        await migrationRunner.MigrateAsync(RepoPath("database", "migrations"));

        var services = new ServiceCollection();
        services.AddWeCmsAccessControlSqlSugar();
        services.AddWeCmsAuditSqlSugar();
        services.AddWeCmsConfigurationSqlSugar();
        services.AddWeCmsFileCenterSqlSugar();
        services.AddWeCmsIdentitySqlSugar();
        services.AddWeCmsOrganizationSqlSugar();
        services.AddWeCmsPlatformSqlSugar();
        services.AddWeCmsSecuritySqlSugar();

        using var provider = services.BuildServiceProvider();
        var registry = new CodeFirstModelRegistry(provider.GetServices<ICodeFirstModelProvider>());
        var validator = new SqlSugarSchemaValidator(registry, db);

        var result = await validator.ValidateAsync(TestContext.Current.CancellationToken);

        Assert.True(result.IsValid, new MigrationScaffold().CreateReviewableDiff(result));
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

    [DbFact]
    public async Task ResetDatabaseAsync_DropsMigratedSchemaSoMigrationsCanRunAgain()
    {
        var baseConnectionString = RequiredConnectionString();

        using (var db = new SqlSugarClientFactory(baseConnectionString).Create())
        {
            var migrationRunner = new DbMigrationRunner(db);
            await migrationRunner.MigrateAsync(RepoPath("database", "migrations"));
            Assert.True(Scalar<int>(db, "SELECT COUNT(1) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = 'sys_user'") == 1);
        }

        await IntegrationTestDatabase.ResetDatabaseAsync(baseConnectionString);

        using (var verifyDb = new SqlSugarClientFactory(baseConnectionString).Create())
        {
            Assert.Equal(0, Scalar<int>(verifyDb, "SELECT COUNT(1) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = 'sys_user'"));
            Assert.Equal(0, Scalar<int>(verifyDb, "SELECT COUNT(1) FROM information_schema.tables WHERE table_schema = DATABASE() AND table_name = 'sys_schema_migration'"));

            var migrationRunner = new DbMigrationRunner(verifyDb);
            var migrationRun = await migrationRunner.MigrateAsync(RepoPath("database", "migrations"));
            Assert.Single(migrationRun);
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
