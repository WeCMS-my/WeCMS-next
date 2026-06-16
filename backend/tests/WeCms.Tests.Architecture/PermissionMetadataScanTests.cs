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
}
