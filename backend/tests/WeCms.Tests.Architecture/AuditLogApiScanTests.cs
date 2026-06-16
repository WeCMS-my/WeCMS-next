namespace WeCms.Tests.Architecture;

public sealed class AuditLogApiScanTests
{
    [Fact]
    public void AuditLogEndpoints_AreReadOnlyAndPermissionProtected()
    {
        var source = File.ReadAllText(Path.Combine(TestPaths.RepoRoot, "backend", "src", "WeCms.Modules.System", "Logs", "AuditLogEndpoints.cs"));

        Assert.Contains("MapGroup(\"/api/v1/system\")", source, StringComparison.Ordinal);
        Assert.Contains(".RequireAuthorization()", source, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/audit-logs\"", source, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/audit-logs/{id:long}\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPost", source, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPut", source, StringComparison.Ordinal);
        Assert.DoesNotContain("MapDelete", source, StringComparison.Ordinal);
        Assert.Contains("RequirePermission(LogPermissions.AuditLogList)", source, StringComparison.Ordinal);
        Assert.Contains("RequirePermission(LogPermissions.AuditLogDetail)", source, StringComparison.Ordinal);
    }
}
