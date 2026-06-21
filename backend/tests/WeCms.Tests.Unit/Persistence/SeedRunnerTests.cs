using WeCms.Data.SqlSugar;

namespace WeCms.Tests.Unit.Persistence;

public sealed class SeedRunnerTests
{
    [Fact]
    public async Task SeedAsync_ThrowsOutsideDevelopmentWhenAdminPasswordIsMissing()
    {
        var seedsDirectory = Directory.CreateTempSubdirectory("wecms-seeds-");
        var runner = new SeedRunner(null!);

        try
        {
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => runner.SeedAsync(seedsDirectory.FullName, new SeedRunnerOptions("Production", null), TestContext.Current.CancellationToken));
        }
        finally
        {
            seedsDirectory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task SeedAsync_ThrowsOutsideDevelopmentWhenAdminPasswordUsesDevelopmentDefault()
    {
        var seedsDirectory = Directory.CreateTempSubdirectory("wecms-seeds-");
        var runner = new SeedRunner(null!);

        try
        {
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => runner.SeedAsync(seedsDirectory.FullName, new SeedRunnerOptions("Production", "Admin@123"), TestContext.Current.CancellationToken));
        }
        finally
        {
            seedsDirectory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task SeedAsync_ThrowsOutsideDevelopmentWhenAdminPasswordIsWeak()
    {
        var seedsDirectory = Directory.CreateTempSubdirectory("wecms-seeds-");
        var runner = new SeedRunner(null!);

        try
        {
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => runner.SeedAsync(seedsDirectory.FullName, new SeedRunnerOptions("Production", "password123"), TestContext.Current.CancellationToken));
        }
        finally
        {
            seedsDirectory.Delete(recursive: true);
        }
    }

    [Fact]
    public async Task SeedAsync_ThrowsWhenSeedFileContainsUnresolvedPlaceholder()
    {
        var seedsDirectory = Directory.CreateTempSubdirectory("wecms-seeds-");
        await File.WriteAllTextAsync(Path.Combine(seedsDirectory.FullName, "000001_bad.sql"), "SELECT '{{MISSING_VALUE}}';", TestContext.Current.CancellationToken);
        var runner = new SeedRunner(null!);

        try
        {
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => runner.SeedAsync(seedsDirectory.FullName, new SeedRunnerOptions("Development", null), TestContext.Current.CancellationToken));
        }
        finally
        {
            seedsDirectory.Delete(recursive: true);
        }
    }
}
