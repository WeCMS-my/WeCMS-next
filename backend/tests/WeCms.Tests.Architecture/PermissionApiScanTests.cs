namespace WeCms.Tests.Architecture;

public sealed class PermissionApiScanTests
{
    [Fact]
    public async Task PermissionEndpoints_AreExplicitlyRegisteredWithPermissions()
    {
        var source = await File.ReadAllTextAsync(Path.Combine(
            TestPaths.RepoRoot,
            "backend",
            "src",
            "WeCms.Modules.System",
            "Permissions",
            "PermissionManagementEndpoints.cs"), TestContext.Current.CancellationToken);

        Assert.Contains("MapGroup(\"/api/v1/system/permissions\")", source, StringComparison.Ordinal);
        Assert.Contains(".RequireAuthorization()", source, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"\"", source, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/tree\"", source, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/{id:long}\"", source, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"\"", source, StringComparison.Ordinal);
        Assert.Contains("MapPut(\"/{id:long}\"", source, StringComparison.Ordinal);
        Assert.Contains("MapDelete(\"/{id:long}\"", source, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/{id:long}/enable\"", source, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/{id:long}/disable\"", source, StringComparison.Ordinal);
        Assert.Contains("RequirePermission(PermissionManagementPermissions.List)", source, StringComparison.Ordinal);
        Assert.Contains("RequirePermission(PermissionManagementPermissions.Tree)", source, StringComparison.Ordinal);
        Assert.Contains("RequirePermission(PermissionManagementPermissions.Detail)", source, StringComparison.Ordinal);
        Assert.Contains("RequirePermission(PermissionManagementPermissions.Create)", source, StringComparison.Ordinal);
        Assert.Contains("RequirePermission(PermissionManagementPermissions.Update)", source, StringComparison.Ordinal);
        Assert.Contains("RequirePermission(PermissionManagementPermissions.Delete)", source, StringComparison.Ordinal);
        Assert.Contains("RequirePermission(PermissionManagementPermissions.Enable)", source, StringComparison.Ordinal);
        Assert.Contains("RequirePermission(PermissionManagementPermissions.Disable)", source, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Program_RegistersPermissionManagementEndpoints()
    {
        var source = await File.ReadAllTextAsync(Path.Combine(TestPaths.RepoRoot, "backend", "src", "WeCms.Api", "Program.cs"), TestContext.Current.CancellationToken);

        Assert.Contains("builder.Services.AddWeCmsSystemPermissions();", source, StringComparison.Ordinal);
        Assert.Contains("app.MapPermissionManagementEndpoints();", source, StringComparison.Ordinal);
    }
}

