namespace WeCms.Tests.Architecture;

public sealed class SystemApiScanTests
{
    [Fact]
    public async Task ApiHost_ExplicitlyMapsSystemEndpoints()
    {
        var source = await File.ReadAllTextAsync(Path.Combine(
            TestPaths.RepoRoot,
            "backend",
            "src",
            "WeCms.Api",
            "Program.cs"));

        Assert.Contains("WebApplication.CreateSlimBuilder(args)", source, StringComparison.Ordinal);
        Assert.Contains("app.MapSystemEndpoints();", source, StringComparison.Ordinal);
        Assert.Contains("app.MapSystemPermissionEndpoints();", source, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SystemResponses_AreIncludedInJsonSerializerContext()
    {
        var source = await File.ReadAllTextAsync(Path.Combine(
            TestPaths.RepoRoot,
            "backend",
            "src",
            "WeCms.Api",
            "Json",
            "WeCmsJsonSerializerContext.cs"));

        Assert.Contains("ApiResult<SystemLiveResponse>", source, StringComparison.Ordinal);
        Assert.Contains("ApiResult<SystemReadyResponse>", source, StringComparison.Ordinal);
        Assert.Contains("ApiResult<SystemPingResponse>", source, StringComparison.Ordinal);
        Assert.Contains("ApiResult<SystemVersionResponse>", source, StringComparison.Ordinal);
        Assert.Contains("ApiResult<SystemDbCheckResponse>", source, StringComparison.Ordinal);
    }
}
