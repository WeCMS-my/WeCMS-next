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
            "WeCms.Modules.AccessControl",
            "Menus",
            "MenuEndpoints.cs"), TestContext.Current.CancellationToken);

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
        Assert.Contains("RequireEndpointPermission(MenuPermissions.List)", source, StringComparison.Ordinal);
        Assert.Contains("RequireEndpointPermission(MenuPermissions.Tree)", source, StringComparison.Ordinal);
        Assert.Contains("RequireEndpointPermission(MenuPermissions.Detail)", source, StringComparison.Ordinal);
        Assert.Contains("RequireEndpointPermission(MenuPermissions.Create)", source, StringComparison.Ordinal);
        Assert.Contains("RequireEndpointPermission(MenuPermissions.Update)", source, StringComparison.Ordinal);
        Assert.Contains("RequireEndpointPermission(MenuPermissions.Sort)", source, StringComparison.Ordinal);
        Assert.Contains("RequireEndpointPermission(MenuPermissions.Delete)", source, StringComparison.Ordinal);
        Assert.Contains("RequireEndpointPermission(MenuPermissions.Enable)", source, StringComparison.Ordinal);
        Assert.Contains("RequireEndpointPermission(MenuPermissions.Disable)", source, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Program_RegistersMenuEndpoints()
    {
        var programSource = await File.ReadAllTextAsync(Path.Combine(TestPaths.RepoRoot, "backend", "src", "WeCms.Api", "Program.cs"), TestContext.Current.CancellationToken);
        var endpointMapSource = await File.ReadAllTextAsync(Path.Combine(TestPaths.RepoRoot, "backend", "src", "WeCms.Api", "Endpoints", "WeCmsApiEndpointRouteBuilderExtensions.cs"), TestContext.Current.CancellationToken);

        Assert.Contains("builder.Services.AddWeCmsAccessControl();", programSource, StringComparison.Ordinal);
        Assert.Contains("endpoints.MapAccessControlEndpoints();", endpointMapSource, StringComparison.Ordinal);
    }
}
