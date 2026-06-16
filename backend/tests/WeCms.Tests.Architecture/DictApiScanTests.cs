namespace WeCms.Tests.Architecture;

public sealed class DictApiScanTests
{
    [Fact]
    public async Task DictEndpoints_AreExplicitlyRegisteredWithPermissions()
    {
        var source = await File.ReadAllTextAsync(Path.Combine(TestPaths.RepoRoot, "backend", "src", "WeCms.Modules.System", "Dicts", "DictEndpoints.cs"));

        Assert.Contains("MapGroup(\"/api/v1/system\")", source, StringComparison.Ordinal);
        Assert.Contains(".RequireAuthorization()", source, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/dict-types\"", source, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/dict-types/{id:long}\"", source, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/dict-types\"", source, StringComparison.Ordinal);
        Assert.Contains("MapPut(\"/dict-types/{id:long}\"", source, StringComparison.Ordinal);
        Assert.Contains("MapDelete(\"/dict-types/{id:long}\"", source, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/dict-types/{typeCode}/values\"", source, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/dict-types/{typeCode}/values\"", source, StringComparison.Ordinal);
        Assert.Contains("MapPut(\"/dict-values/{id:long}\"", source, StringComparison.Ordinal);
        Assert.Contains("MapDelete(\"/dict-values/{id:long}\"", source, StringComparison.Ordinal);
        Assert.Contains("RequirePermission(DictPermissions.TypeList)", source, StringComparison.Ordinal);
        Assert.Contains("RequirePermission(DictPermissions.TypeCreate)", source, StringComparison.Ordinal);
        Assert.Contains("RequirePermission(DictPermissions.TypeUpdate)", source, StringComparison.Ordinal);
        Assert.Contains("RequirePermission(DictPermissions.TypeDelete)", source, StringComparison.Ordinal);
        Assert.Contains("RequirePermission(DictPermissions.ValueList)", source, StringComparison.Ordinal);
        Assert.Contains("RequirePermission(DictPermissions.ValueCreate)", source, StringComparison.Ordinal);
        Assert.Contains("RequirePermission(DictPermissions.ValueUpdate)", source, StringComparison.Ordinal);
        Assert.Contains("RequirePermission(DictPermissions.ValueDelete)", source, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Program_RegistersDictEndpoints()
    {
        var source = await File.ReadAllTextAsync(Path.Combine(TestPaths.RepoRoot, "backend", "src", "WeCms.Api", "Program.cs"));

        Assert.Contains("builder.Services.AddWeCmsSystemDicts();", source, StringComparison.Ordinal);
        Assert.Contains("app.MapDictEndpoints();", source, StringComparison.Ordinal);
    }
}
