namespace WeCms.Tests.Architecture;

public sealed class RoleApiScanTests
{
    [Fact]
    public async Task RoleEndpoints_AreExplicitlyRegisteredWithPermissions()
    {
        var source = await File.ReadAllTextAsync(Path.Combine(
            TestPaths.RepoRoot,
            "backend",
            "src",
            "WeCms.Modules.System",
            "Roles",
            "RoleEndpoints.cs"));

        Assert.Contains("MapGroup(\"/api/v1/system/roles\")", source, StringComparison.Ordinal);
        Assert.Contains(".RequireAuthorization()", source, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"\"", source, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/{id:long}\"", source, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"\"", source, StringComparison.Ordinal);
        Assert.Contains("MapPut(\"/{id:long}\"", source, StringComparison.Ordinal);
        Assert.Contains("MapDelete(\"/{id:long}\"", source, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/{id:long}/enable\"", source, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/{id:long}/disable\"", source, StringComparison.Ordinal);
        Assert.Contains("MapPut(\"/{id:long}/permissions\"", source, StringComparison.Ordinal);
        Assert.Contains("MapPut(\"/{id:long}/menus\"", source, StringComparison.Ordinal);
        Assert.Contains("RequirePermission(RolePermissions.List)", source, StringComparison.Ordinal);
        Assert.Contains("RequirePermission(RolePermissions.Detail)", source, StringComparison.Ordinal);
        Assert.Contains("RequirePermission(RolePermissions.Create)", source, StringComparison.Ordinal);
        Assert.Contains("RequirePermission(RolePermissions.Update)", source, StringComparison.Ordinal);
        Assert.Contains("RequirePermission(RolePermissions.Delete)", source, StringComparison.Ordinal);
        Assert.Contains("RequirePermission(RolePermissions.Enable)", source, StringComparison.Ordinal);
        Assert.Contains("RequirePermission(RolePermissions.Disable)", source, StringComparison.Ordinal);
        Assert.Contains("RequirePermission(RolePermissions.AssignPermission)", source, StringComparison.Ordinal);
        Assert.Contains("RequirePermission(RolePermissions.AssignMenu)", source, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Program_RegistersRoleEndpoints()
    {
        var source = await File.ReadAllTextAsync(Path.Combine(TestPaths.RepoRoot, "backend", "src", "WeCms.Api", "Program.cs"));

        Assert.Contains("builder.Services.AddWeCmsSystemRoles();", source, StringComparison.Ordinal);
        Assert.Contains("app.MapRoleEndpoints();", source, StringComparison.Ordinal);
    }
}
