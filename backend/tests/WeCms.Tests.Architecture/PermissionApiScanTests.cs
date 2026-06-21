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
            "WeCms.Modules.AccessControl",
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
        Assert.Contains("RequireEndpointPermission(PermissionManagementPermissions.List)", source, StringComparison.Ordinal);
        Assert.Contains("RequireEndpointPermission(PermissionManagementPermissions.Tree)", source, StringComparison.Ordinal);
        Assert.Contains("RequireEndpointPermission(PermissionManagementPermissions.Detail)", source, StringComparison.Ordinal);
        Assert.Contains("RequireEndpointPermission(PermissionManagementPermissions.Create)", source, StringComparison.Ordinal);
        Assert.Contains("RequireEndpointPermission(PermissionManagementPermissions.Update)", source, StringComparison.Ordinal);
        Assert.Contains("RequireEndpointPermission(PermissionManagementPermissions.Delete)", source, StringComparison.Ordinal);
        Assert.Contains("RequireEndpointPermission(PermissionManagementPermissions.Enable)", source, StringComparison.Ordinal);
        Assert.Contains("RequireEndpointPermission(PermissionManagementPermissions.Disable)", source, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Program_RegistersPermissionManagementEndpoints()
    {
        var programSource = await File.ReadAllTextAsync(Path.Combine(TestPaths.RepoRoot, "backend", "src", "WeCms.Api", "Program.cs"), TestContext.Current.CancellationToken);
        var endpointMapSource = await File.ReadAllTextAsync(Path.Combine(TestPaths.RepoRoot, "backend", "src", "WeCms.Api", "Endpoints", "WeCmsApiEndpointRouteBuilderExtensions.cs"), TestContext.Current.CancellationToken);

        Assert.Contains("builder.Services.AddScoped<IAccessControlPermissionVersionService>", programSource, StringComparison.Ordinal);
        Assert.Contains("endpoints.MapAccessControlEndpoints();", endpointMapSource, StringComparison.Ordinal);
    }
}
