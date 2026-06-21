namespace WeCms.Tests.Architecture;

public sealed class DictApiScanTests
{
    [Fact]
    public async Task DictEndpoints_AreExplicitlyRegisteredWithPermissions()
    {
        var source = await File.ReadAllTextAsync(Path.Combine(TestPaths.RepoRoot, "backend", "src", "WeCms.Modules.Configuration", "Dicts", "DictEndpoints.cs"), TestContext.Current.CancellationToken);

        Assert.Contains("MapGroup(\"/api/v1/system\")", source, StringComparison.Ordinal);
        Assert.Contains(".RequireAuthorization()", source, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/dict-types\"", source, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/dict-types/{id:long}\"", source, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/dict-types\"", source, StringComparison.Ordinal);
        Assert.Contains("MapPut(\"/dict-types/{id:long}\"", source, StringComparison.Ordinal);
        Assert.Contains("MapDelete(\"/dict-types/{id:long}\"", source, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/dict-types/{id:long}/enable\"", source, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/dict-types/{id:long}/disable\"", source, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/dict-types/{typeCode}/values\"", source, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/dict-types/{typeCode}/values\"", source, StringComparison.Ordinal);
        Assert.Contains("MapPut(\"/dict-values/{id:long}\"", source, StringComparison.Ordinal);
        Assert.Contains("MapDelete(\"/dict-values/{id:long}\"", source, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/dict-values/{id:long}/enable\"", source, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/dict-values/{id:long}/disable\"", source, StringComparison.Ordinal);
        Assert.Contains("RequireEndpointPermission(DictPermissions.TypeList)", source, StringComparison.Ordinal);
        Assert.Contains("RequireEndpointPermission(DictPermissions.TypeCreate)", source, StringComparison.Ordinal);
        Assert.Contains("RequireEndpointPermission(DictPermissions.TypeUpdate)", source, StringComparison.Ordinal);
        Assert.Contains("RequireEndpointPermission(DictPermissions.TypeDelete)", source, StringComparison.Ordinal);
        Assert.Contains("RequireEndpointPermission(DictPermissions.TypeEnable)", source, StringComparison.Ordinal);
        Assert.Contains("RequireEndpointPermission(DictPermissions.TypeDisable)", source, StringComparison.Ordinal);
        Assert.Contains("RequireEndpointPermission(DictPermissions.ValueList)", source, StringComparison.Ordinal);
        Assert.Contains("RequireEndpointPermission(DictPermissions.ValueCreate)", source, StringComparison.Ordinal);
        Assert.Contains("RequireEndpointPermission(DictPermissions.ValueUpdate)", source, StringComparison.Ordinal);
        Assert.Contains("RequireEndpointPermission(DictPermissions.ValueDelete)", source, StringComparison.Ordinal);
        Assert.Contains("RequireEndpointPermission(DictPermissions.ValueEnable)", source, StringComparison.Ordinal);
        Assert.Contains("RequireEndpointPermission(DictPermissions.ValueDisable)", source, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Program_RegistersDictEndpoints()
    {
        var programSource = await File.ReadAllTextAsync(Path.Combine(TestPaths.RepoRoot, "backend", "src", "WeCms.Api", "Program.cs"), TestContext.Current.CancellationToken);
        var endpointMapSource = await File.ReadAllTextAsync(Path.Combine(TestPaths.RepoRoot, "backend", "src", "WeCms.Api", "Endpoints", "WeCmsApiEndpointRouteBuilderExtensions.cs"), TestContext.Current.CancellationToken);
        var configurationSource = await File.ReadAllTextAsync(Path.Combine(TestPaths.RepoRoot, "backend", "src", "WeCms.Modules.Configuration", "ConfigurationServiceCollectionExtensions.cs"), TestContext.Current.CancellationToken);

        Assert.Contains("builder.Services.AddWeCmsConfiguration();", programSource, StringComparison.Ordinal);
        Assert.Contains("endpoints.MapDictEndpoints();", endpointMapSource, StringComparison.Ordinal);
        Assert.Contains("services.AddWeCmsConfigurationDicts();", configurationSource, StringComparison.Ordinal);
    }
}
