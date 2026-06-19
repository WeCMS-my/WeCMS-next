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
            "PermissionEndpointExtensions.cs"), TestContext.Current.CancellationToken);

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
            "SystemPermissions.cs"), TestContext.Current.CancellationToken);

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
            "SystemEndpointExtensions.cs"), TestContext.Current.CancellationToken);

        Assert.Contains("MapGet(\"/api/v1/system/ping\"", source, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/api/v1/system/version\"", source, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/api/v1/system/db-check\"", source, StringComparison.Ordinal);
        Assert.Contains(".AllowAnonymous()", source, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/health/dependencies\"", source, StringComparison.Ordinal);
        Assert.Contains(".RequirePermission(SystemPermissions.SecurePing)", source, StringComparison.Ordinal);
        Assert.Equal(1, CountOccurrences(source, "RequirePermission("));
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
            "PermissionEndpointExtensions.cs"), TestContext.Current.CancellationToken);
        var systemSource = await File.ReadAllTextAsync(Path.Combine(
            TestPaths.RepoRoot,
            "backend",
            "src",
            "WeCms.Modules.System",
            "System",
            "SystemEndpointExtensions.cs"), TestContext.Current.CancellationToken);

        Assert.Contains("MapGet(\"/api/v1/system/secure-ping\"", permissionSource, StringComparison.Ordinal);
        Assert.Contains(".RequirePermission(SystemPermissions.SecurePing)", permissionSource, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/health/dependencies\"", systemSource, StringComparison.Ordinal);
        Assert.Contains(".RequirePermission(SystemPermissions.SecurePing)", systemSource, StringComparison.Ordinal);
        Assert.Equal(1, CountOccurrences(systemSource, "RequirePermission("));
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

