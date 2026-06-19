namespace WeCms.Tests.Architecture;

public sealed class PostApiScanTests
{
    [Fact]
    public async Task PostEndpoints_AreExplicitlyRegisteredWithPermissions()
    {
        var source = await File.ReadAllTextAsync(Path.Combine(TestPaths.RepoRoot, "backend", "src", "WeCms.Modules.System", "Posts", "PostEndpoints.cs"), TestContext.Current.CancellationToken);

        Assert.Contains("MapGroup(\"/api/v1/system/posts\")", source, StringComparison.Ordinal);
        Assert.Contains(".RequireAuthorization()", source, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"\"", source, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/{id:long}\"", source, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"\"", source, StringComparison.Ordinal);
        Assert.Contains("MapPut(\"/{id:long}\"", source, StringComparison.Ordinal);
        Assert.Contains("MapDelete(\"/{id:long}\"", source, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/{id:long}/enable\"", source, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/{id:long}/disable\"", source, StringComparison.Ordinal);
        Assert.Contains("RequirePermission(PostPermissions.List)", source, StringComparison.Ordinal);
        Assert.Contains("RequirePermission(PostPermissions.Detail)", source, StringComparison.Ordinal);
        Assert.Contains("RequirePermission(PostPermissions.Create)", source, StringComparison.Ordinal);
        Assert.Contains("RequirePermission(PostPermissions.Update)", source, StringComparison.Ordinal);
        Assert.Contains("RequirePermission(PostPermissions.Delete)", source, StringComparison.Ordinal);
        Assert.Contains("RequirePermission(PostPermissions.Enable)", source, StringComparison.Ordinal);
        Assert.Contains("RequirePermission(PostPermissions.Disable)", source, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Program_RegistersPostEndpoints()
    {
        var source = await File.ReadAllTextAsync(Path.Combine(TestPaths.RepoRoot, "backend", "src", "WeCms.Api", "Program.cs"), TestContext.Current.CancellationToken);

        Assert.Contains("builder.Services.AddWeCmsSystemPosts();", source, StringComparison.Ordinal);
        Assert.Contains("app.MapPostEndpoints();", source, StringComparison.Ordinal);
    }
}

