namespace WeCms.Tests.Architecture;

public sealed class MenuApiScanTests
{
    [Fact]
    public async Task MenuEndpoints_AreExplicitlyRegisteredWithPermissions()
    {
        var source = await File.ReadAllTextAsync(Path.Combine(
            TestPaths.RepoRoot,
            "backend",
            "src",
            "WeCms.Modules.System",
            "Menus",
            "MenuEndpoints.cs"));

        Assert.Contains("MapGroup(\"/api/v1/system/menus\")", source, StringComparison.Ordinal);
        Assert.Contains(".RequireAuthorization()", source, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"\"", source, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/tree\"", source, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/{id:long}\"", source, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"\"", source, StringComparison.Ordinal);
        Assert.Contains("MapPut(\"/{id:long}\"", source, StringComparison.Ordinal);
        Assert.Contains("MapPut(\"/sort\"", source, StringComparison.Ordinal);
        Assert.Contains("MapDelete(\"/{id:long}\"", source, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/{id:long}/enable\"", source, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/{id:long}/disable\"", source, StringComparison.Ordinal);
        Assert.Contains("RequirePermission(MenuPermissions.List)", source, StringComparison.Ordinal);
        Assert.Contains("RequirePermission(MenuPermissions.Tree)", source, StringComparison.Ordinal);
        Assert.Contains("RequirePermission(MenuPermissions.Detail)", source, StringComparison.Ordinal);
        Assert.Contains("RequirePermission(MenuPermissions.Create)", source, StringComparison.Ordinal);
        Assert.Contains("RequirePermission(MenuPermissions.Update)", source, StringComparison.Ordinal);
        Assert.Contains("RequirePermission(MenuPermissions.Sort)", source, StringComparison.Ordinal);
        Assert.Contains("RequirePermission(MenuPermissions.Delete)", source, StringComparison.Ordinal);
        Assert.Contains("RequirePermission(MenuPermissions.Enable)", source, StringComparison.Ordinal);
        Assert.Contains("RequirePermission(MenuPermissions.Disable)", source, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Program_RegistersMenuEndpoints()
    {
        var source = await File.ReadAllTextAsync(Path.Combine(TestPaths.RepoRoot, "backend", "src", "WeCms.Api", "Program.cs"));

        Assert.Contains("builder.Services.AddWeCmsSystemMenus();", source, StringComparison.Ordinal);
        Assert.Contains("app.MapMenuEndpoints();", source, StringComparison.Ordinal);
    }
}
