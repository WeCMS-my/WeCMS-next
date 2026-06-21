namespace WeCms.Tests.Architecture;

public sealed class LoginLogApiScanTests
{
    [Fact]
    public void LoginLogEndpoints_AreReadOnlyAndPermissionProtected()
    {
        var source = File.ReadAllText(Path.Combine(TestPaths.RepoRoot, "backend", "src", "WeCms.Modules.Audit", "Logs", "LoginLogEndpoints.cs"));

        Assert.Contains("MapGroup(\"/api/v1/system\")", source, StringComparison.Ordinal);
        Assert.Contains(".RequireAuthorization()", source, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/login-logs\"", source, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/login-logs/{id:long}\"", source, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPost", source, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPut", source, StringComparison.Ordinal);
        Assert.DoesNotContain("MapDelete", source, StringComparison.Ordinal);
        Assert.Contains("RequireEndpointPermission(LogPermissions.LoginLogList)", source, StringComparison.Ordinal);
        Assert.Contains("RequireEndpointPermission(LogPermissions.LoginLogDetail)", source, StringComparison.Ordinal);
    }

    [Fact]
    public void LogModule_DoesNotContainSqlOrOrmReferences()
    {
        var moduleRoot = Path.Combine(TestPaths.RepoRoot, "backend", "src", "WeCms.Modules.Audit", "Logs");
        var sources = Directory.GetFiles(moduleRoot, "*.cs", SearchOption.AllDirectories)
            .Select(File.ReadAllText)
            .ToArray();

        Assert.All(sources, source =>
        {
            Assert.DoesNotContain("SqlSugar", source);
            Assert.DoesNotContain("SELECT ", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("INSERT INTO", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("UPDATE sys_", source, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("DELETE FROM", source, StringComparison.OrdinalIgnoreCase);
        });
    }
}
