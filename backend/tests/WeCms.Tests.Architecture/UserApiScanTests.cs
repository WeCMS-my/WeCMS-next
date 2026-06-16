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
            "WeCms.Modules.System",
            "Users",
            "UserEndpoints.cs"));

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
        Assert.Contains("MapPut(\"/{id:long}/roles\"", source, StringComparison.Ordinal);
        Assert.Contains("MapPut(\"/{id:long}/posts\"", source, StringComparison.Ordinal);
        Assert.Contains("RequirePermission(UserPermissions.List)", source, StringComparison.Ordinal);
        Assert.Contains("RequirePermission(UserPermissions.Detail)", source, StringComparison.Ordinal);
        Assert.Contains("RequirePermission(UserPermissions.Create)", source, StringComparison.Ordinal);
        Assert.Contains("RequirePermission(UserPermissions.Update)", source, StringComparison.Ordinal);
        Assert.Contains("RequirePermission(UserPermissions.Delete)", source, StringComparison.Ordinal);
        Assert.Contains("RequirePermission(UserPermissions.Enable)", source, StringComparison.Ordinal);
        Assert.Contains("RequirePermission(UserPermissions.Disable)", source, StringComparison.Ordinal);
        Assert.Contains("RequirePermission(UserPermissions.ResetPassword)", source, StringComparison.Ordinal);
        Assert.Contains("RequirePermission(UserPermissions.AssignRole)", source, StringComparison.Ordinal);
        Assert.Contains("RequirePermission(UserPermissions.AssignPost)", source, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Program_RegistersUserEndpoints()
    {
        var source = await File.ReadAllTextAsync(Path.Combine(TestPaths.RepoRoot, "backend", "src", "WeCms.Api", "Program.cs"));

        Assert.Contains("builder.Services.AddWeCmsSystemUsers();", source, StringComparison.Ordinal);
        Assert.Contains("app.MapUserEndpoints();", source, StringComparison.Ordinal);
    }
}
