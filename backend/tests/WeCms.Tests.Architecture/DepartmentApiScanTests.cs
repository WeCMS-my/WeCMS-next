namespace WeCms.Tests.Architecture;

public sealed class DepartmentApiScanTests
{
    [Fact]
    public async Task DepartmentEndpoints_AreExplicitlyRegisteredWithPermissions()
    {
        var source = await File.ReadAllTextAsync(Path.Combine(TestPaths.RepoRoot, "backend", "src", "WeCms.Modules.System", "Departments", "DepartmentEndpoints.cs"));

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
        Assert.Contains("RequirePermission(DepartmentPermissions.List)", source, StringComparison.Ordinal);
        Assert.Contains("RequirePermission(DepartmentPermissions.Tree)", source, StringComparison.Ordinal);
        Assert.Contains("RequirePermission(DepartmentPermissions.Detail)", source, StringComparison.Ordinal);
        Assert.Contains("RequirePermission(DepartmentPermissions.Create)", source, StringComparison.Ordinal);
        Assert.Contains("RequirePermission(DepartmentPermissions.Update)", source, StringComparison.Ordinal);
        Assert.Contains("RequirePermission(DepartmentPermissions.Delete)", source, StringComparison.Ordinal);
        Assert.Contains("RequirePermission(DepartmentPermissions.Enable)", source, StringComparison.Ordinal);
        Assert.Contains("RequirePermission(DepartmentPermissions.Disable)", source, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Program_RegistersDepartmentEndpoints()
    {
        var source = await File.ReadAllTextAsync(Path.Combine(TestPaths.RepoRoot, "backend", "src", "WeCms.Api", "Program.cs"));

        Assert.Contains("builder.Services.AddWeCmsSystemDepartments();", source, StringComparison.Ordinal);
        Assert.Contains("app.MapDepartmentEndpoints();", source, StringComparison.Ordinal);
    }
}
