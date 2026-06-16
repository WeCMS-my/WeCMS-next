namespace WeCms.Tests.Architecture;

public sealed class PermissionMetadataScanTests
{
    [Fact]
    public async Task SecurePingEndpoint_BindsAuthorizationAndPermissionMetadata()
    {
        var source = await File.ReadAllTextAsync(Path.Combine(
            TestPaths.RepoRoot,
            "backend",
            "src",
            "WeCms.Modules.System",
            "Permissions",
            "PermissionEndpointExtensions.cs"));

        Assert.Contains("MapGet(\"/api/v1/system/secure-ping\"", source, StringComparison.Ordinal);
        Assert.Contains(".RequireAuthorization()", source, StringComparison.Ordinal);
        Assert.Contains(".RequirePermission(SystemPermissions.SecurePing)", source, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SecurePingPermissionCode_IsSingleSystemPermissionConstant()
    {
        var source = await File.ReadAllTextAsync(Path.Combine(
            TestPaths.RepoRoot,
            "backend",
            "src",
            "WeCms.Modules.System",
            "Permissions",
            "SystemPermissions.cs"));

        Assert.Contains("public const string SecurePing = \"sys:system:secure-ping\";", source, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SystemEndpoints_DoNotRequirePermissionUnlessExplicitlyProtected()
    {
        var source = await File.ReadAllTextAsync(Path.Combine(
            TestPaths.RepoRoot,
            "backend",
            "src",
            "WeCms.Modules.System",
            "System",
            "SystemEndpointExtensions.cs"));

        Assert.Contains("MapGet(\"/api/v1/system/ping\"", source, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/api/v1/system/version\"", source, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/api/v1/system/db-check\"", source, StringComparison.Ordinal);
        Assert.Contains(".AllowAnonymous()", source, StringComparison.Ordinal);
        Assert.DoesNotContain("RequirePermission(", source, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SystemPermissions_Boundary_IsExplicitAndLimitedToSecurePing()
    {
        var permissionSource = await File.ReadAllTextAsync(Path.Combine(
            TestPaths.RepoRoot,
            "backend",
            "src",
            "WeCms.Modules.System",
            "Permissions",
            "PermissionEndpointExtensions.cs"));
        var systemSource = await File.ReadAllTextAsync(Path.Combine(
            TestPaths.RepoRoot,
            "backend",
            "src",
            "WeCms.Modules.System",
            "System",
            "SystemEndpointExtensions.cs"));

        Assert.Contains("MapGet(\"/api/v1/system/secure-ping\"", permissionSource, StringComparison.Ordinal);
        Assert.Contains(".RequirePermission(SystemPermissions.SecurePing)", permissionSource, StringComparison.Ordinal);
        Assert.DoesNotContain("RequirePermission(", systemSource, StringComparison.Ordinal);
    }
}
