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
            "WeCms.Modules.Platform",
            "System",
            "PlatformSystemEndpointExtensions.cs"), TestContext.Current.CancellationToken);

        Assert.Contains("MapGet(\"/api/v1/system/secure-ping\"", source, StringComparison.Ordinal);
        Assert.Contains(".RequireAuthorization()", source, StringComparison.Ordinal);
        Assert.Contains(".RequireEndpointPermission(PlatformPermissions.SecurePing)", source, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SecurePingPermissionCode_IsSingleSystemPermissionConstant()
    {
        var source = await File.ReadAllTextAsync(Path.Combine(
            TestPaths.RepoRoot,
            "backend",
            "src",
            "WeCms.Modules.Platform",
            "Permissions",
            "PlatformPermissions.cs"), TestContext.Current.CancellationToken);

        Assert.Contains("public const string SecurePing = \"sys:system:secure-ping\";", source, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SystemEndpoints_DoNotRequirePermissionUnlessExplicitlyProtected()
    {
        var source = await File.ReadAllTextAsync(Path.Combine(
            TestPaths.RepoRoot,
            "backend",
            "src",
            "WeCms.Modules.Platform",
            "System",
            "PlatformSystemEndpointExtensions.cs"), TestContext.Current.CancellationToken);

        Assert.Contains("MapGet(\"/api/v1/system/version\"", source, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/api/v1/system/db-check\"", source, StringComparison.Ordinal);
        Assert.Contains(".AllowAnonymous()", source, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/health/dependencies\"", source, StringComparison.Ordinal);
        Assert.Contains(".RequireEndpointPermission(PlatformPermissions.SecurePing)", source, StringComparison.Ordinal);
        Assert.Equal(2, CountOccurrences(source, "RequireEndpointPermission("));
    }

    [Fact]
    public async Task PlatformPermissions_Boundary_IsExplicitAndLimitedToSecurePing()
    {
        var systemSource = await File.ReadAllTextAsync(Path.Combine(
            TestPaths.RepoRoot,
            "backend",
            "src",
            "WeCms.Modules.Platform",
            "System",
            "PlatformSystemEndpointExtensions.cs"), TestContext.Current.CancellationToken);

        Assert.Contains("MapGet(\"/api/v1/system/secure-ping\"", systemSource, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/health/dependencies\"", systemSource, StringComparison.Ordinal);
        Assert.Contains(".RequireEndpointPermission(PlatformPermissions.SecurePing)", systemSource, StringComparison.Ordinal);
        Assert.Equal(2, CountOccurrences(systemSource, "RequireEndpointPermission("));
    }

    private static int CountOccurrences(string source, string value)
    {
        var count = 0;
        var index = 0;
        while ((index = source.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += value.Length;
        }

        return count;
    }
}
