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
            "WeCms.Modules.AccessControl",
            "Roles",
            "RoleEndpoints.cs"), TestContext.Current.CancellationToken);

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
        Assert.Contains("RequireEndpointPermission(RolePermissions.List)", source, StringComparison.Ordinal);
        Assert.Contains("RequireEndpointPermission(RolePermissions.Detail)", source, StringComparison.Ordinal);
        Assert.Contains("RequireEndpointPermission(RolePermissions.Create)", source, StringComparison.Ordinal);
        Assert.Contains("RequireEndpointPermission(RolePermissions.Update)", source, StringComparison.Ordinal);
        Assert.Contains("RequireEndpointPermission(RolePermissions.Delete)", source, StringComparison.Ordinal);
        Assert.Contains("RequireEndpointPermission(RolePermissions.Enable)", source, StringComparison.Ordinal);
        Assert.Contains("RequireEndpointPermission(RolePermissions.Disable)", source, StringComparison.Ordinal);
        Assert.Contains("RequireEndpointPermission(RolePermissions.AssignPermission)", source, StringComparison.Ordinal);
        Assert.Contains("RequireEndpointPermission(RolePermissions.AssignMenu)", source, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Program_RegistersRoleEndpoints()
    {
        var programSource = await File.ReadAllTextAsync(Path.Combine(TestPaths.RepoRoot, "backend", "src", "WeCms.Api", "Program.cs"), TestContext.Current.CancellationToken);
        var endpointMapSource = await File.ReadAllTextAsync(Path.Combine(TestPaths.RepoRoot, "backend", "src", "WeCms.Api", "Endpoints", "WeCmsApiEndpointRouteBuilderExtensions.cs"), TestContext.Current.CancellationToken);

        Assert.Contains("builder.Services.AddWeCmsAccessControl();", programSource, StringComparison.Ordinal);
        Assert.Contains("endpoints.MapAccessControlEndpoints();", endpointMapSource, StringComparison.Ordinal);
    }
}
