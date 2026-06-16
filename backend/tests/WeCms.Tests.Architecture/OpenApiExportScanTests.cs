namespace WeCms.Tests.Architecture;

public sealed class OpenApiExportScanTests
{
    [Fact]
    public async Task Program_HandlesOpenApiExportBeforeSlimBuilderStartup()
    {
        var source = await File.ReadAllTextAsync(Path.Combine(
            TestPaths.RepoRoot,
            "backend",
            "src",
            "WeCms.Api",
            "Program.cs"));

        var exportIndex = source.IndexOf("OpenApiExtensions.ExportOpenApiAsync(args)", StringComparison.Ordinal);
        var builderIndex = source.IndexOf("WebApplication.CreateSlimBuilder(args)", StringComparison.Ordinal);

        Assert.True(exportIndex >= 0);
        Assert.True(builderIndex > exportIndex);
    }

    [Fact]
    public async Task OpenApiExport_DoesNotStartRuntimeHostOrUseNewtonsoft()
    {
        var source = await File.ReadAllTextAsync(Path.Combine(
            TestPaths.RepoRoot,
            "backend",
            "src",
            "WeCms.Api",
            "Extensions",
            "OpenApiExtensions.cs"));

        Assert.DoesNotContain("app.Run()", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Newtonsoft", source, StringComparison.Ordinal);
        Assert.Contains("System.Text.Json", source, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OpenApiExport_DiscoveryRegistration_DoesNotTouchPersistence()
    {
        var source = await File.ReadAllTextAsync(Path.Combine(
            TestPaths.RepoRoot,
            "backend",
            "src",
            "WeCms.Api",
            "Extensions",
            "OpenApiExtensions.cs"));

        Assert.DoesNotContain("AddWeCmsPersistence", source, StringComparison.Ordinal);
        Assert.DoesNotContain("AddWeCmsSystemAuth", source, StringComparison.Ordinal);
        Assert.DoesNotContain("AddWeCmsSystemPermissions", source, StringComparison.Ordinal);
        Assert.DoesNotContain("AddInMemoryCollection", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ConnectionStrings:Default", source, StringComparison.Ordinal);
        Assert.DoesNotContain("DbMigrationRunner", source, StringComparison.Ordinal);
        Assert.DoesNotContain("IAuthTokenEntropy", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Auth:AccessTokenSecret", source, StringComparison.Ordinal);
    }
}
