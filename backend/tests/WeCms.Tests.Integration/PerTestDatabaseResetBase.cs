using SqlSugar;
using WeCms.Data.SqlSugar;

namespace WeCms.Tests.Integration;

public abstract class PerTestDatabaseResetBase
{
    protected PerTestDatabaseResetBase()
    {
        IntegrationTestDatabase.ResetDatabaseAsync(IntegrationTestDatabase.GetConnectionString()).GetAwaiter().GetResult();
    }

    protected static async Task PrepareDatabaseWithSeedsAsync(ISqlSugarClient db)
    {
        await PrepareDatabaseWithSeedsAsync(db, new SeedRunnerOptions("Development", null));
    }

    protected static async Task PrepareDatabaseWithSeedsAsync(ISqlSugarClient db, SeedRunnerOptions options)
    {
        await IntegrationTestDatabase.ResetDatabaseAsync(db);
        await new DbMigrationRunner(db).MigrateAsync(BaseRepoPath("database", "migrations"));
        await new SeedRunner(db).SeedAsync(BaseRepoPath("database", "seeds"), options);
    }

    protected static async Task PrepareDatabaseAsync(ISqlSugarClient db)
    {
        await IntegrationTestDatabase.ResetDatabaseAsync(db);
        await new DbMigrationRunner(db).MigrateAsync(BaseRepoPath("database", "migrations"));
    }

    private static string BaseRepoPath(params string[] segments)
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
