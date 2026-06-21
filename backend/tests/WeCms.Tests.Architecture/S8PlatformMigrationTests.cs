namespace WeCms.Tests.Architecture;

public sealed class S8PlatformMigrationTests
{
    private static readonly string SystemPlatformRoot = Path.Combine(TestPaths.RepoRoot, "backend", "src", LegacyBoundaryNames.SystemModule, "System");
    private static readonly string PlatformRoot = Path.Combine(TestPaths.RepoRoot, "backend", "src", "WeCms.Modules.Platform");
    private static readonly string PlatformSqlSugarSystemRoot = Path.Combine(TestPaths.RepoRoot, "backend", "src", "WeCms.Modules.Platform.SqlSugar", "System");

    [Fact]
    public async Task SystemModule_DoesNotContainPlatformOwnership()
    {
        var text = Directory.Exists(SystemPlatformRoot) ? await ReadAllSourceAsync(SystemPlatformRoot) : string.Empty;

        foreach (var forbidden in new[]
        {
            "SystemEndpointExtensions",
            "SystemLiveResponse",
            "SystemReadyResponse",
            "SystemDependenciesResponse",
            "SystemDbCheckResponse",
            "ISystemDatabaseProbe",
            "ISystemMigrationProbe"
        })
        {
            Assert.DoesNotContain(forbidden, text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task PlatformModule_OwnsPlatformContractsAndEndpoints()
    {
        var text = await ReadAllSourceAsync(PlatformRoot);

        foreach (var required in new[]
        {
            "SystemLiveResponse",
            "SystemReadyResponse",
            "SystemDependenciesResponse",
            "SystemDbCheckResponse",
            "ISystemDatabaseProbe",
            "ISystemMigrationProbe",
            "MapPlatformEndpoints",
            "MapGet(\"/health/live\"",
            "MapGet(\"/api/v1/system/db-check\""
        })
        {
            Assert.Contains(required, text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task PlatformModule_DoesNotContainDatabaseInfrastructure()
    {
        var text = await ReadAllSourceAsync(PlatformRoot);

        foreach (var forbidden in new[]
        {
            "SqlSugar",
            "MySqlConnector",
            LegacyBoundaryNames.Persistence,
            "SELECT ",
            "INSERT INTO",
            "UPDATE sys_",
            "DELETE FROM"
        })
        {
            Assert.DoesNotContain(forbidden, text, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task PlatformSqlSugarKeepsOnlyPlatformProbeImplementations()
    {
        var text = Directory.Exists(PlatformSqlSugarSystemRoot) ? await ReadAllSourceAsync(PlatformSqlSugarSystemRoot) : string.Empty;

        Assert.Contains("SystemDatabaseProbe", text, StringComparison.Ordinal);
        Assert.Contains("SystemMigrationProbe", text, StringComparison.Ordinal);
        Assert.Contains("WeCms.Modules.Platform", text, StringComparison.Ordinal);
        Assert.DoesNotContain("SystemEndpointExtensions", text, StringComparison.Ordinal);
        Assert.DoesNotContain("SystemLiveResponse", text, StringComparison.Ordinal);
    }

    private static async Task<string> ReadAllSourceAsync(string root)
    {
        var files = Directory
            .EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(filePath => !filePath.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                && !filePath.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .OrderBy(filePath => filePath, StringComparer.Ordinal);

        var chunks = new List<string>();
        foreach (var file in files)
        {
            chunks.Add(await File.ReadAllTextAsync(file));
        }

        return string.Join('\n', chunks);
    }
}
