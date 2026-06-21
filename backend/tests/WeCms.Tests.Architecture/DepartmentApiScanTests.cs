namespace WeCms.Tests.Architecture;

public sealed class DepartmentApiScanTests
{
    [Fact]
    public async Task DepartmentEndpoints_AreExplicitlyRegisteredWithPermissions()
    {
        var source = await File.ReadAllTextAsync(Path.Combine(TestPaths.RepoRoot, "backend", "src", "WeCms.Modules.Organization", "Departments", "DepartmentEndpoints.cs"), TestContext.Current.CancellationToken);

        Assert.Contains("MapGroup(\"/api/v1/system/depts\")", source, StringComparison.Ordinal);
        Assert.Contains(".RequireAuthorization()", source, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"\"", source, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/tree\"", source, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/{id:long}\"", source, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"\"", source, StringComparison.Ordinal);
        Assert.Contains("MapPut(\"/{id:long}\"", source, StringComparison.Ordinal);
        Assert.Contains("MapDelete(\"/{id:long}\"", source, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/{id:long}/enable\"", source, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/{id:long}/disable\"", source, StringComparison.Ordinal);
        Assert.Contains("RequireEndpointPermission(DepartmentPermissions.List)", source, StringComparison.Ordinal);
        Assert.Contains("RequireEndpointPermission(DepartmentPermissions.Tree)", source, StringComparison.Ordinal);
        Assert.Contains("RequireEndpointPermission(DepartmentPermissions.Detail)", source, StringComparison.Ordinal);
        Assert.Contains("RequireEndpointPermission(DepartmentPermissions.Create)", source, StringComparison.Ordinal);
        Assert.Contains("RequireEndpointPermission(DepartmentPermissions.Update)", source, StringComparison.Ordinal);
        Assert.Contains("RequireEndpointPermission(DepartmentPermissions.Delete)", source, StringComparison.Ordinal);
        Assert.Contains("RequireEndpointPermission(DepartmentPermissions.Enable)", source, StringComparison.Ordinal);
        Assert.Contains("RequireEndpointPermission(DepartmentPermissions.Disable)", source, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Program_RegistersDepartmentEndpoints()
    {
        var programSource = await File.ReadAllTextAsync(Path.Combine(TestPaths.RepoRoot, "backend", "src", "WeCms.Api", "Program.cs"), TestContext.Current.CancellationToken);
        var endpointMapSource = await File.ReadAllTextAsync(Path.Combine(TestPaths.RepoRoot, "backend", "src", "WeCms.Api", "Endpoints", "WeCmsApiEndpointRouteBuilderExtensions.cs"), TestContext.Current.CancellationToken);

        Assert.Contains("builder.Services.AddWeCmsOrganization();", programSource, StringComparison.Ordinal);
        Assert.Contains("endpoints.MapDepartmentEndpoints();", endpointMapSource, StringComparison.Ordinal);
    }
}
