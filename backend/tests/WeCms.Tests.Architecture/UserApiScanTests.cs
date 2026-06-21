namespace WeCms.Tests.Architecture;

public sealed class UserApiScanTests
{
    [Fact]
    public async Task UserEndpoints_AreExplicitlyRegisteredWithPermissions()
    {
        var source = await File.ReadAllTextAsync(Path.Combine(
            TestPaths.RepoRoot,
            "backend",
            "src",
            "WeCms.Modules.Identity",
            "Endpoints",
            "UserEndpointDefinition.cs"), TestContext.Current.CancellationToken);

        Assert.Contains("MapGroup(\"/api/v1/system/users\")", source, StringComparison.Ordinal);
        Assert.Contains(".RequireAuthorization()", source, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"\"", source, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/{id:long}\"", source, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"\"", source, StringComparison.Ordinal);
        Assert.Contains("MapPut(\"/{id:long}\"", source, StringComparison.Ordinal);
        Assert.Contains("MapDelete(\"/{id:long}\"", source, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/{id:long}/enable\"", source, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/{id:long}/disable\"", source, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/{id:long}/reset-password\"", source, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/{id:long}/reset-2fa\"", source, StringComparison.Ordinal);
        Assert.Contains("MapPut(\"/{id:long}/roles\"", source, StringComparison.Ordinal);
        Assert.Contains("MapPut(\"/{id:long}/positions\"", source, StringComparison.Ordinal);
        Assert.Contains("RequireEndpointPermission(IdentityUserPermissions.List)", source, StringComparison.Ordinal);
        Assert.Contains("RequireEndpointPermission(IdentityUserPermissions.Detail)", source, StringComparison.Ordinal);
        Assert.Contains("RequireEndpointPermission(IdentityUserPermissions.Create)", source, StringComparison.Ordinal);
        Assert.Contains("RequireEndpointPermission(IdentityUserPermissions.Update)", source, StringComparison.Ordinal);
        Assert.Contains("RequireEndpointPermission(IdentityUserPermissions.Delete)", source, StringComparison.Ordinal);
        Assert.Contains("RequireEndpointPermission(IdentityUserPermissions.Enable)", source, StringComparison.Ordinal);
        Assert.Contains("RequireEndpointPermission(IdentityUserPermissions.Disable)", source, StringComparison.Ordinal);
        Assert.Contains("RequireEndpointPermission(IdentityUserPermissions.ResetPassword)", source, StringComparison.Ordinal);
        Assert.Contains("RequireEndpointPermission(IdentityUserPermissions.ResetTwoFactor)", source, StringComparison.Ordinal);
        Assert.Contains("RequireEndpointPermission(IdentityUserPermissions.AssignRole)", source, StringComparison.Ordinal);
        Assert.Contains("RequireEndpointPermission(IdentityUserPermissions.AssignPosition)", source, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Program_RegistersUserEndpoints()
    {
        var programSource = await File.ReadAllTextAsync(Path.Combine(TestPaths.RepoRoot, "backend", "src", "WeCms.Api", "Program.cs"), TestContext.Current.CancellationToken);
        var endpointMapSource = await File.ReadAllTextAsync(Path.Combine(TestPaths.RepoRoot, "backend", "src", "WeCms.Api", "Endpoints", "WeCmsApiEndpointRouteBuilderExtensions.cs"), TestContext.Current.CancellationToken);

        Assert.Contains("builder.Services.AddWeCmsIdentity(builder.Configuration);", programSource, StringComparison.Ordinal);
        Assert.Contains("registry.Add(new UserEndpointDefinition());", endpointMapSource, StringComparison.Ordinal);
    }
}
