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
    public async Task OpenApiExport_DoesNotUseRuntimeHostBuilderOrNewtonsoft()
    {
        var source = await File.ReadAllTextAsync(Path.Combine(
            TestPaths.RepoRoot,
            "backend",
            "src",
            "WeCms.Api",
            "Extensions",
            "OpenApiExtensions.cs"));

        Assert.DoesNotContain("CreateBuilder", source, StringComparison.Ordinal);
        Assert.DoesNotContain("CreateSlimBuilder", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Newtonsoft", source, StringComparison.Ordinal);
        Assert.Contains("System.Text.Json", source, StringComparison.Ordinal);
    }
}
