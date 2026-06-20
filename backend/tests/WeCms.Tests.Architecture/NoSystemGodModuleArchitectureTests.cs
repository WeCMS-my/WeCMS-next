namespace WeCms.Tests.Architecture;

public sealed class NoSystemGodModuleArchitectureTests
{
    private const string SystemSplitAdr = "0018-system-foundation-module-split.md";
    private const string FinalSplitFlag = "WECMS_ARCHITECTURE_FINAL_SYSTEM_SPLIT";

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
        Assert.Contains("WeCms.Modules.System 最终删除", adr, StringComparison.Ordinal);
        Assert.Contains("Posts -> Positions", adr, StringComparison.Ordinal);
        Assert.Contains("WeCms.Modules.Cms 暂不启用", adr, StringComparison.Ordinal);

        foreach (var module in TargetModules)
        {
            Assert.Contains(module, adr, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void SystemGodModule_IsOnlyAllowedDuringExplicitTransition()
    {
        var systemProjectPath = Path.Combine(TestPaths.SourceRoot, "WeCms.Modules.System", "WeCms.Modules.System.csproj");
        if (!File.Exists(systemProjectPath))
        {
            return;
        }

        var adr = File.ReadAllText(Path.Combine(TestPaths.RepoRoot, "docs", "adr", SystemSplitAdr));
        Assert.Contains("迁移期间允许旧 WeCms.Modules.System 暂存", adr, StringComparison.Ordinal);
        Assert.Contains("最终验收不得保留 WeCms.Modules.System", adr, StringComparison.Ordinal);
    }

    [Fact]
    public void FinalSystemSplitMode_RejectsSystemGodModule()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable(FinalSplitFlag), "true", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var systemProjectPath = Path.Combine(TestPaths.SourceRoot, "WeCms.Modules.System", "WeCms.Modules.System.csproj");
        Assert.False(File.Exists(systemProjectPath), "Final system split mode does not allow WeCms.Modules.System.");
    }
}
