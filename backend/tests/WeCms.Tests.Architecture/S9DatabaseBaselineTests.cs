using System.Text.Json;

namespace WeCms.Tests.Architecture;

public sealed class S9DatabaseBaselineTests
{
    [Fact]
    public void DatabaseBaseline_UsesSingleSchemaMigrationAndTwoSeeds()
    {
        var migrations = Directory.GetFiles(DatabasePath("migrations"), "*.sql")
            .Select(path => Path.GetFileName(path)!)
            .Order(StringComparer.Ordinal)
            .ToArray();
        var seeds = Directory.GetFiles(DatabasePath("seeds"), "*.sql")
            .Select(path => Path.GetFileName(path)!)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(["000001_baseline_system_schema.sql"], migrations);
        Assert.Equal(
            [
                "000002_seed_system_permissions.sql",
                "000003_seed_super_admin.sql",
            ],
            seeds);
    }

    [Fact]
    public void LatestRequiredMigration_UsesResetBaselineId()
    {
        foreach (var fileName in new[]
        {
            "appsettings.json",
            "appsettings.Development.json",
            "appsettings.Development.example.json",
            "appsettings.Production.example.json",
        })
        {
            using var document = JsonDocument.Parse(File.ReadAllText(ApiPath(fileName)));
            var migration = document.RootElement
                .GetProperty("Database")
                .GetProperty("LatestRequiredMigration")
                .GetString();

            Assert.Equal("000001_baseline_system_schema", migration);
        }
    }

    [Fact]
    public void BaselineSchema_ContainsSystemFoundationTablesWithoutLegacyPostNames()
    {
        var source = ReadDatabaseFile("migrations", "000001_baseline_system_schema.sql");

        foreach (var table in new[]
        {
            "sys_schema_migration",
            "sys_user",
            "sys_role",
            "sys_permission",
            "sys_menu",
            "sys_position",
            "sys_user_position",
            "sys_security_event",
            "sys_i18n_message",
            "sys_file",
        })
        {
            Assert.Matches($@"CREATE TABLE(?: IF NOT EXISTS)? {table}\b", source);
        }

        Assert.DoesNotContain("sys_" + "post", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("sys_user_" + "post", source, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Seeds_SplitPermissionsFromSuperAdminSecretRotation()
    {
        var permissionSeed = ReadDatabaseFile("seeds", "000002_seed_system_permissions.sql");
        var superAdminSeed = ReadDatabaseFile("seeds", "000003_seed_super_admin.sql");

        Assert.Contains("'sys:user:page'", permissionSeed, StringComparison.Ordinal);
        Assert.Contains("'sys:system:secure-ping'", permissionSeed, StringComparison.Ordinal);
        Assert.Contains("'sys.users'", permissionSeed, StringComparison.Ordinal);
        Assert.DoesNotContain("{{ADMIN_PASSWORD_HASH}}", permissionSeed, StringComparison.Ordinal);

        Assert.Contains("{{ADMIN_PASSWORD_HASH}}", superAdminSeed, StringComparison.Ordinal);
        Assert.Contains("{{ADMIN_MUST_CHANGE_PASSWORD}}", superAdminSeed, StringComparison.Ordinal);
        Assert.Contains("JOIN sys_permission p", superAdminSeed, StringComparison.Ordinal);
        Assert.Contains("WHERE r.code = 'super_admin'", superAdminSeed, StringComparison.Ordinal);
        Assert.Contains("WHERE rp.role_id = r.id", superAdminSeed, StringComparison.Ordinal);
        Assert.DoesNotContain("p.code IN", superAdminSeed, StringComparison.Ordinal);
    }

    [Fact]
    public void SmokeTests_ExpectResetBaselineMigrationCount()
    {
        var source = File.ReadAllText(
            Path.Combine(
                TestPaths.BackendRoot,
                "tests",
                "WeCms.Tests.Integration",
                "Persistence",
                "MigrationAndSeedSmokeTests.cs"));

        Assert.Contains("Assert.Single(firstMigrationRun);", source, StringComparison.Ordinal);
        Assert.Contains("Assert.Equal(1, Scalar<int>(db, \"SELECT COUNT(1) FROM sys_schema_migration\"));", source, StringComparison.Ordinal);
        Assert.Contains("Assert.Single(migrationRun);", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Assert.Equal(19,", source, StringComparison.Ordinal);
    }

    [Fact]
    public void PermissionCoverageScripts_ReadResetSuperAdminSeed()
    {
        var systemCoverage = File.ReadAllText(
            Path.Combine(TestPaths.RepoRoot, "scripts", "checks", "check-system-permission-coverage.sh"));
        var lockedRole = File.ReadAllText(
            Path.Combine(TestPaths.RepoRoot, "scripts", "checks", "check-locked-role-seed.sh"));
        var oldRolePermissionSeed = "000005_seed_" + "m1_role_permissions.sql";

        Assert.Contains("000003_seed_super_admin.sql", systemCoverage, StringComparison.Ordinal);
        Assert.Contains("000003_seed_super_admin.sql", lockedRole, StringComparison.Ordinal);
        Assert.DoesNotContain(oldRolePermissionSeed, systemCoverage, StringComparison.Ordinal);
        Assert.DoesNotContain(oldRolePermissionSeed, lockedRole, StringComparison.Ordinal);
    }

    private static string ReadDatabaseFile(string directory, string fileName)
    {
        return File.ReadAllText(Path.Combine(DatabasePath(directory), fileName));
    }

    private static string DatabasePath(string directory)
    {
        return Path.Combine(TestPaths.RepoRoot, "database", directory);
    }

    private static string ApiPath(string fileName)
    {
        return Path.Combine(TestPaths.BackendRoot, "src", "WeCms.Api", fileName);
    }
}
