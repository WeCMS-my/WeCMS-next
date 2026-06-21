namespace WeCms.Tests.Architecture;

public sealed class PositionApiScanTests
{
    [Fact]
    public async Task PositionEndpoints_AreExplicitlyRegisteredWithPermissions()
    {
        var source = await File.ReadAllTextAsync(Path.Combine(TestPaths.RepoRoot, "backend", "src", "WeCms.Modules.Organization", "Positions", "PositionEndpoints.cs"), TestContext.Current.CancellationToken);

        Assert.Contains("MapGroup(\"/api/v1/system/positions\")", source, StringComparison.Ordinal);
        Assert.Contains(".RequireAuthorization()", source, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"\"", source, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/{id:long}\"", source, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"\"", source, StringComparison.Ordinal);
        Assert.Contains("MapPut(\"/{id:long}\"", source, StringComparison.Ordinal);
        Assert.Contains("MapDelete(\"/{id:long}\"", source, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/{id:long}/enable\"", source, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/{id:long}/disable\"", source, StringComparison.Ordinal);
        Assert.Contains("RequireEndpointPermission(PositionPermissions.List)", source, StringComparison.Ordinal);
        Assert.Contains("RequireEndpointPermission(PositionPermissions.Detail)", source, StringComparison.Ordinal);
        Assert.Contains("RequireEndpointPermission(PositionPermissions.Create)", source, StringComparison.Ordinal);
        Assert.Contains("RequireEndpointPermission(PositionPermissions.Update)", source, StringComparison.Ordinal);
        Assert.Contains("RequireEndpointPermission(PositionPermissions.Delete)", source, StringComparison.Ordinal);
        Assert.Contains("RequireEndpointPermission(PositionPermissions.Enable)", source, StringComparison.Ordinal);
        Assert.Contains("RequireEndpointPermission(PositionPermissions.Disable)", source, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Program_RegistersPositionEndpoints()
    {
        var programSource = await File.ReadAllTextAsync(Path.Combine(TestPaths.RepoRoot, "backend", "src", "WeCms.Api", "Program.cs"), TestContext.Current.CancellationToken);
        var endpointMapSource = await File.ReadAllTextAsync(Path.Combine(TestPaths.RepoRoot, "backend", "src", "WeCms.Api", "Endpoints", "WeCmsApiEndpointRouteBuilderExtensions.cs"), TestContext.Current.CancellationToken);

        Assert.Contains("builder.Services.AddWeCmsOrganization();", programSource, StringComparison.Ordinal);
        Assert.Contains("endpoints.MapPositionEndpoints();", endpointMapSource, StringComparison.Ordinal);
    }
}
