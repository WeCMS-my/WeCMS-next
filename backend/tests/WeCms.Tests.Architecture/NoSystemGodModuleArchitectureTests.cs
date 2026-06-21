namespace WeCms.Tests.Architecture;

public sealed class NoSystemGodModuleArchitectureTests
{
    private const string SystemSplitAdr = "0018-system-foundation-module-split.md";

    private static readonly string[] TargetModules =
    [
        "WeCms.Modules.Identity",
        "WeCms.Modules.AccessControl",
        "WeCms.Modules.Organization",
        "WeCms.Modules.Configuration",
        "WeCms.Modules.Audit",
        "WeCms.Modules.Security",
        "WeCms.Modules.FileCenter",
        "WeCms.Modules.Platform"
    ];

    [Fact]
    public void SystemFoundationModuleSplitDecision_IsAccepted()
    {
        var adrPath = Path.Combine(TestPaths.RepoRoot, "docs", "adr", SystemSplitAdr);
        Assert.True(File.Exists(adrPath), $"Missing ADR: docs/adr/{SystemSplitAdr}");

        var adr = File.ReadAllText(adrPath);
        Assert.Contains("## 状态", adr, StringComparison.Ordinal);
        Assert.Contains("Accepted", adr, StringComparison.Ordinal);
        Assert.Contains(LegacyBoundaryNames.SystemModule + " 最终删除", adr, StringComparison.Ordinal);
        Assert.Contains("Posts -> Positions", adr, StringComparison.Ordinal);
        Assert.Contains("WeCms.Modules.Cms 暂不启用", adr, StringComparison.Ordinal);

        foreach (var module in TargetModules)
        {
            Assert.Contains(module, adr, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void SystemGodModule_IsRejectedByDefault()
    {
        var systemProjectPath = Path.Combine(TestPaths.SourceRoot, LegacyBoundaryNames.SystemModule, LegacyBoundaryNames.SystemProject);
        Assert.False(File.Exists(systemProjectPath), "Final system split mode does not allow " + LegacyBoundaryNames.SystemModule + ".");
    }

    [Fact]
    public void NoSystemGodModuleGate_IsFinalByDefault()
    {
        var scriptPath = Path.Combine(TestPaths.RepoRoot, "scripts", "checks", "check-no-system-god-module.sh");
        var script = File.ReadAllText(scriptPath);

        Assert.DoesNotContain("WECMS_ARCHITECTURE_FINAL_SYSTEM_SPLIT", script, StringComparison.Ordinal);
        Assert.DoesNotContain("迁移期间允许旧 " + LegacyBoundaryNames.SystemModule + " 暂存", script, StringComparison.Ordinal);
        Assert.Contains("final mode does not allow " + LegacyBoundaryNames.SystemModule, script, StringComparison.Ordinal);
    }
}
