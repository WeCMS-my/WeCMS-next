namespace WeCms.Tests.Architecture;

public sealed class SecurityEventApiScanTests
{
    [Fact]
    public void SecurityEventEndpoints_AreReadOnlyAndPermissionProtected()
    {
        var source = File.ReadAllText(Path.Combine(TestPaths.RepoRoot, "backend", "src", "WeCms.Modules.System", "Logs", "SecurityEventEndpoints.cs"));

        Assert.Contains("MapGroup(\"/api/v1/system\")", source, StringComparison.Ordinal);
        Assert.Contains(".RequireAuthorization()", source, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/security-events\"", source, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/security-events/{id:long}\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPost", source, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPut", source, StringComparison.Ordinal);
        Assert.DoesNotContain("MapDelete", source, StringComparison.Ordinal);
        Assert.Contains("RequirePermission(LogPermissions.SecurityEventList)", source, StringComparison.Ordinal);
        Assert.Contains("RequirePermission(LogPermissions.SecurityEventDetail)", source, StringComparison.Ordinal);
    }
}
