namespace WeCms.Tests.Unit.Persistence;

public sealed class DatabaseGovernanceSourceTests
{
    [Fact]
    public async Task Program_ProvidesExplicitMigrationCommandAndConfiguredStartupMigration()
    {
        var program = await File.ReadAllTextAsync(RepoPath("backend", "src", "WeCms.Api", "Program.cs"), TestContext.Current.CancellationToken);

        Assert.Contains("DatabaseMigrationCommand.IsMigrationCommand(args)", program, StringComparison.Ordinal);
        Assert.Contains("DatabaseMigrationCommand.RunAsync(app)", program, StringComparison.Ordinal);
        Assert.Contains("useMigrationConnectionString: isMigrationCommand", program, StringComparison.Ordinal);
        Assert.Contains("DatabaseStartupMigrationOptions.ShouldRunMigrationsOnStartup", program, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Program_UsesWeCmsFileStorageExtensionRegistration()
    {
        var program = await File.ReadAllTextAsync(RepoPath("backend", "src", "WeCms.Api", "Program.cs"), TestContext.Current.CancellationToken);

        Assert.Contains("builder.Services.AddWeCmsFileStorage(", program, StringComparison.Ordinal);
        Assert.DoesNotContain("AddScoped<IFileStorage>", program, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StartupMigrationOptions_DefaultToDevelopmentOnly()
    {
        var source = await File.ReadAllTextAsync(RepoPath("backend", "src", "WeCms.Api", "Extensions", "DatabaseStartupMigrationOptions.cs"), TestContext.Current.CancellationToken);

        Assert.Contains("Database:RunMigrationsOnStartup", source, StringComparison.Ordinal);
        Assert.Contains("environment.IsDevelopment()", source, StringComparison.Ordinal);
        Assert.Contains("must be true or false", source, StringComparison.Ordinal);
    }

    [Fact]
    public async Task IntegrationDatabase_DefaultHostIsLocalhost()
    {
        var source = await File.ReadAllTextAsync(RepoPath("backend", "tests", "WeCms.Tests.Integration", "IntegrationTestDatabase.cs"), TestContext.Current.CancellationToken);

        Assert.Contains("AllowedHost = \"127.0.0.1\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain("AllowedHost = \"192.168.101.199\"", source, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MigrationCommand_RunsMigrationAndSeedRunners()
    {
        var source = await File.ReadAllTextAsync(RepoPath("backend", "src", "WeCms.Api", "Extensions", "DatabaseMigrationCommand.cs"), TestContext.Current.CancellationToken);

        Assert.Contains("--migrate", source, StringComparison.Ordinal);
        Assert.Contains("IDbMigrationRunner", source, StringComparison.Ordinal);
        Assert.Contains("ISeedRunner", source, StringComparison.Ordinal);
        Assert.Contains("Database:SeedAdminPassword", source, StringComparison.Ordinal);
        Assert.Contains("FindRepoRoot", source, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SqlSugarDataRegistration_CanUseMigrationConnectionString()
    {
        var source = await File.ReadAllTextAsync(RepoPath("backend", "src", "WeCms.Data.SqlSugar", "SqlSugarDataServiceCollectionExtensions.cs"), TestContext.Current.CancellationToken);

        Assert.Contains("useMigrationConnectionString", source, StringComparison.Ordinal);
        Assert.Contains("GetConnectionString(\"Migration\")", source, StringComparison.Ordinal);
        Assert.Contains("DatabasePlatformOptions", source, StringComparison.Ordinal);
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
