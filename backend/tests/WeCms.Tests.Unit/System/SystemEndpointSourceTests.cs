namespace WeCms.Tests.Unit.SystemApi;

public sealed class SystemEndpointSourceTests
{
    [Fact]
    public async Task SystemEndpoints_MapExpectedRoutesAndDoNotExposeDatabaseExceptionMessages()
    {
        var source = await File.ReadAllTextAsync(RepoPath(
            "backend",
            "src",
            "WeCms.Modules.System",
            "System",
            "SystemEndpointExtensions.cs"), TestContext.Current.CancellationToken);

        Assert.Contains("MapGet(\"/health/live\"", source, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/health/ready\"", source, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/health/dependencies\"", source, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/api/v1/system/ping\"", source, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/api/v1/system/version\"", source, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/api/v1/system/db-check\"", source, StringComparison.Ordinal);
        Assert.Contains("DatabaseUnavailableMessage", source, StringComparison.Ordinal);
        Assert.Contains("ISystemMigrationProbe", source, StringComparison.Ordinal);
        Assert.DoesNotContain(".Message", source, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DependenciesEndpoint_IsProtected()
    {
        var source = await File.ReadAllTextAsync(RepoPath(
            "backend",
            "src",
            "WeCms.Modules.System",
            "System",
            "SystemEndpointExtensions.cs"), TestContext.Current.CancellationToken);

        var dependenciesStart = source.IndexOf("MapGet(\"/health/dependencies\"", StringComparison.Ordinal);
        var nextRoute = source.IndexOf("MapGet(\"/api/v1/system/ping\"", dependenciesStart, StringComparison.Ordinal);
        Assert.True(dependenciesStart >= 0);
        Assert.True(nextRoute > dependenciesStart);

        var dependenciesBlock = source[dependenciesStart..nextRoute];
        Assert.Contains(".RequireAuthorization()", dependenciesBlock, StringComparison.Ordinal);
        Assert.DoesNotContain(".AllowAnonymous()", dependenciesBlock, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LiveEndpoint_DoesNotRequireDatabaseProbe()
    {
        var source = await File.ReadAllTextAsync(RepoPath(
            "backend",
            "src",
            "WeCms.Modules.System",
            "System",
            "SystemEndpointExtensions.cs"), TestContext.Current.CancellationToken);

        var liveStart = source.IndexOf("MapGet(\"/health/live\"", StringComparison.Ordinal);
        var readyStart = source.IndexOf("MapGet(\"/health/ready\"", StringComparison.Ordinal);
        Assert.True(liveStart >= 0);
        Assert.True(readyStart > liveStart);

        var liveBlock = source[liveStart..readyStart];
        Assert.DoesNotContain("ISystemDatabaseProbe", liveBlock, StringComparison.Ordinal);
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
