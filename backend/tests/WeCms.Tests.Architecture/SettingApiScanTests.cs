namespace WeCms.Tests.Architecture;

public sealed class SettingApiScanTests
{
    [Fact]
    public void SettingEndpoints_AreExplicitAndPermissionProtected()
    {
        var source = File.ReadAllText(Path.Combine(TestPaths.RepoRoot, "backend", "src", "WeCms.Modules.System", "Settings", "SettingEndpoints.cs"));

        Assert.Contains("MapGroup(\"/api/v1/system\")", source);
        Assert.Contains(".RequireAuthorization()", source);
        Assert.Contains("MapGet(\"/settings\"", source);
        Assert.Contains("MapGet(\"/settings/{key}\"", source);
        Assert.Contains("MapPut(\"/settings/{key}\"", source);
        Assert.Contains("RequirePermission(SettingPermissions.List)", source);
        Assert.Contains("RequirePermission(SettingPermissions.Detail)", source);
        Assert.Contains("RequirePermission(SettingPermissions.Update)", source);
    }

    [Fact]
    public void SettingModule_DoesNotContainSqlOrOrmReferences()
    {
        var moduleRoot = Path.Combine(TestPaths.RepoRoot, "backend", "src", "WeCms.Modules.System", "Settings");
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
