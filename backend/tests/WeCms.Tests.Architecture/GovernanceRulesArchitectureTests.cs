namespace WeCms.Tests.Architecture;

public sealed class GovernanceRulesArchitectureTests
{
    [Fact]
    public void AgentsRules_DescribeSystemFoundationUpgradeGovernance()
    {
        var source = ReadRepoFile("AGENTS.md");

        Assert.Contains("允许 Autofac / DynamicProxy", source, StringComparison.Ordinal);
        Assert.Contains("AOP 只能拦截 Application Service 接口", source, StringComparison.Ordinal);
        Assert.Contains("CodeFirst 建模", source, StringComparison.Ordinal);
        Assert.Contains("WeCms.Data.SqlSugar", source, StringComparison.Ordinal);
        Assert.Contains("WeCms.Modules.*.SqlSugar", source, StringComparison.Ordinal);
        Assert.Contains(LegacyBoundaryNames.SystemModule + "` 已从 active source 删除", source, StringComparison.Ordinal);
        Assert.Contains(LegacyBoundaryNames.Persistence + "` 已从 active source 删除", source, StringComparison.Ordinal);
        Assert.Contains("不得重新引入", source, StringComparison.Ordinal);
        Assert.Contains("无生产环境，允许重置数据库 baseline", source, StringComparison.Ordinal);
        Assert.Contains("CMS 模块暂不实现", source, StringComparison.Ordinal);
        Assert.Contains("允许 Swagger / Scalar / MiniProfiler", source, StringComparison.Ordinal);
    }

    [Fact]
    public void CodeReviewRules_DescribeNewP0Boundaries()
    {
        var source = ReadRepoFile("code_review.md");

        Assert.Contains("AOP 只能用于 Application Service 接口", source, StringComparison.Ordinal);
        Assert.Contains("Repository 不得被 AOP 拦截", source, StringComparison.Ordinal);
        Assert.Contains("业务模块不得引用 WeCms.Data.SqlSugar", source, StringComparison.Ordinal);
        Assert.Contains("业务模块不得引用 WeCms.Modules.*.SqlSugar", source, StringComparison.Ordinal);
        Assert.Contains("新增 Endpoint 无权限或审计 metadata", source, StringComparison.Ordinal);
        Assert.Contains("SqlAudit 未脱敏", source, StringComparison.Ordinal);
        Assert.Contains("重构必须先有架构测试保护", source, StringComparison.Ordinal);
        Assert.Contains("Swagger / Scalar / MiniProfiler", source, StringComparison.Ordinal);
    }

    [Fact]
    public void TraeRules_DoNotContradictSystemFoundationUpgradeGovernance()
    {
        var source = ReadRepoFile(Path.Combine(".trae", "rules", "wecms-engineering-principles.md"));

        Assert.Contains("允许 Autofac / DynamicProxy", source, StringComparison.Ordinal);
        Assert.Contains("AOP 只能拦截 Application Service 接口", source, StringComparison.Ordinal);
        Assert.Contains("CodeFirst 建模", source, StringComparison.Ordinal);
        Assert.Contains("WeCms.Data.SqlSugar", source, StringComparison.Ordinal);
        Assert.Contains("WeCms.Modules.*.SqlSugar", source, StringComparison.Ordinal);
        Assert.Contains(LegacyBoundaryNames.SystemModule + "` 与 `" + LegacyBoundaryNames.Persistence + "` 已退出 active source", source, StringComparison.Ordinal);
        Assert.Contains("不得重新引入", source, StringComparison.Ordinal);
        Assert.Contains("允许 Swagger / Scalar / MiniProfiler", source, StringComparison.Ordinal);
    }

    private static string ReadRepoFile(string relativePath)
    {
        return File.ReadAllText(Path.Combine(TestPaths.RepoRoot, relativePath));
    }
}
