using WeCms.Persistence.Migration;

namespace WeCms.Api.Extensions;

public static class DatabaseMigrationCommand
{
    private const string MigrateArgument = "--migrate";

    public static bool IsMigrationCommand(string[] args)
    {
        return args.Any(argument => string.Equals(argument, MigrateArgument, StringComparison.Ordinal));
    }

    public static async Task RunAsync(WebApplication app, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(app);

        using var scope = app.Services.CreateScope();
        var migrationRunner = scope.ServiceProvider.GetRequiredService<IDbMigrationRunner>();
        var seedRunner = scope.ServiceProvider.GetRequiredService<ISeedRunner>();
        var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var repoRoot = FindRepoRoot(environment.ContentRootPath);

        await migrationRunner.MigrateAsync(Path.Combine(repoRoot, "database", "migrations"), cancellationToken);
        await seedRunner.SeedAsync(
            Path.Combine(repoRoot, "database", "seeds"),
            new SeedRunnerOptions(environment.EnvironmentName, configuration["Database:SeedAdminPassword"]),
            cancellationToken);
    }

    private static string FindRepoRoot(string startPath)
    {
        var directory = new DirectoryInfo(startPath);
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "database"))
                && File.Exists(Path.Combine(directory.FullName, "backend", "WeCms.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root for database migrations.");
    }
}
