namespace WeCms.Tests.Architecture;

public sealed class DatabaseSeedSafetyTests
{
    private static readonly string RepoRoot = GetRepositoryRoot();

    [Fact]
    public void DbMigrationRunner_ShouldNotEmbedApplicationSchemaOrSeedSql()
    {
        var runnerPath = Path.Combine(
            RepoRoot,
            "backend",
            "src",
            "WeCms.Persistence",
            "Migration",
            "DbMigrationRunner.cs");
        var source = File.ReadAllText(runnerPath);

        Assert.DoesNotContain("internal static class MigrationSql", source, StringComparison.Ordinal);
        Assert.DoesNotContain("internal static class SeedSql", source, StringComparison.Ordinal);
        Assert.DoesNotContain("CREATE TABLE IF NOT EXISTS `sys_user`", source, StringComparison.Ordinal);
        Assert.DoesNotContain("INSERT IGNORE INTO `sys_permission`", source, StringComparison.Ordinal);
    }

    [Fact]
    public void DbMigrationRunner_ShouldFailFastWhenAppliedChecksumDrifts()
    {
        var runnerPath = Path.Combine(
            RepoRoot,
            "backend",
            "src",
            "WeCms.Persistence",
            "Migration",
            "DbMigrationRunner.cs");
        var source = File.ReadAllText(runnerPath);

        Assert.Contains("Checksum drift", source, StringComparison.Ordinal);
        Assert.Contains("checksum", source, StringComparison.Ordinal);
    }

    [Fact]
    public void SchemaMigrationFiles_ShouldNotDefineRunnerMetadataTables()
    {
        var migrationDir = Path.Combine(RepoRoot, "database", "migrations");
        var matches = new List<string>();

        foreach (var migrationFile in Directory.EnumerateFiles(migrationDir, "*.sql", SearchOption.TopDirectoryOnly))
        {
            var text = File.ReadAllText(migrationFile);
            if (text.Contains("sys_schema_migration", StringComparison.Ordinal) ||
                text.Contains("sys_seed_migration", StringComparison.Ordinal))
            {
                matches.Add(Path.GetRelativePath(RepoRoot, migrationFile));
            }
        }

        Assert.Empty(matches);
    }

    [Fact]
    public void SqlSeeds_ShouldNotPersistRuntimePasswordHashPlaceholders()
    {
        var seedDir = Path.Combine(RepoRoot, "database", "seeds");
        var matches = new List<string>();

        foreach (var seedFile in Directory.EnumerateFiles(seedDir, "*.sql", SearchOption.TopDirectoryOnly))
        {
            var lines = File.ReadAllLines(seedFile);
            for (var i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("PLACEHOLDER_RUNTIME_HASH", StringComparison.Ordinal))
                {
                    matches.Add($"{Path.GetRelativePath(RepoRoot, seedFile)}:{i + 1}: {lines[i].Trim()}");
                }
            }
        }

        Assert.Empty(matches);
    }

    [Fact]
    public void SuperAdminSeed_ShouldUseRuntimePasswordHashParameter()
    {
        var seedPath = Path.Combine(RepoRoot, "database", "seeds", "000002_seed_m0_super_admin.sql");
        var seedSql = File.ReadAllText(seedPath);

        Assert.Contains("@PasswordHash", seedSql, StringComparison.Ordinal);
        Assert.DoesNotContain("PLACEHOLDER_RUNTIME_HASH", seedSql, StringComparison.Ordinal);
    }

    [Fact]
    public void ManualDatabaseScripts_ShouldUseApplicationMigrator()
    {
        var scriptPaths = new[]
        {
            Path.Combine(RepoRoot, "scripts", "db", "apply-migrations.sh"),
            Path.Combine(RepoRoot, "scripts", "db", "seed-dev.sh")
        };
        var matches = new List<string>();

        foreach (var scriptPath in scriptPaths)
        {
            var text = File.ReadAllText(scriptPath);
            if (text.Contains(" mysql ", StringComparison.Ordinal) ||
                text.Contains("< \"$file\"", StringComparison.Ordinal) ||
                text.Contains("mapfile", StringComparison.Ordinal))
            {
                matches.Add(Path.GetRelativePath(RepoRoot, scriptPath));
            }

            if (!text.Contains("--migrate-database", StringComparison.Ordinal))
            {
                matches.Add($"{Path.GetRelativePath(RepoRoot, scriptPath)} missing --migrate-database");
            }
        }

        Assert.Empty(matches);
    }

    private static string GetRepositoryRoot()
    {
        var directoryCandidates = new[]
        {
            Directory.GetCurrentDirectory(),
            AppContext.BaseDirectory
        };

        foreach (var candidate in directoryCandidates)
        {
            var current = new DirectoryInfo(candidate);
            while (current is not null)
            {
                if (File.Exists(Path.Combine(current.FullName, "backend", "WeCms.sln")) ||
                    File.Exists(Path.Combine(current.FullName, "backend", "WeCms.slnx")))
                {
                    return current.FullName;
                }

                current = current.Parent;
            }
        }

        throw new DirectoryNotFoundException("Could not locate repository root containing backend/WeCms.sln");
    }
}
