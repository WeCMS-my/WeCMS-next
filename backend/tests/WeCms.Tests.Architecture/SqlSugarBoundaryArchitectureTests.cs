namespace WeCms.Tests.Architecture;

public sealed class SqlSugarBoundaryArchitectureTests
{
    private const string SqlSugarDataPlatformAdr = "0019-sqlsugar-data-platform.md";
    private const string FinalDataPlatformFlag = "WECMS_ARCHITECTURE_FINAL_SQLSUGAR_PLATFORM";

    private static readonly string[] DatabaseTokens =
    [
        "SqlSugarCore",
        "SqlSugarClient",
        "SqlSugarScope",
        "ISqlSugarClient",
        "MySqlConnection",
        "MySqlConnector"
    ];

    private static readonly string[] RawSqlTokens =
    [
        ".Ado.",
        "SqlQuery",
        "GetScalar",
        "ExecuteCommand",
        "SELECT "
    ];

    [Fact]
    public void SqlSugarDataPlatformDecision_IsAccepted()
    {
        var adrPath = Path.Combine(TestPaths.RepoRoot, "docs", "adr", SqlSugarDataPlatformAdr);
        Assert.True(File.Exists(adrPath), $"Missing ADR: docs/adr/{SqlSugarDataPlatformAdr}");

        var adr = File.ReadAllText(adrPath);
        Assert.Contains("## 状态", adr, StringComparison.Ordinal);
        Assert.Contains("Accepted", adr, StringComparison.Ordinal);
        Assert.Contains("CodeFirst", adr, StringComparison.Ordinal);
        Assert.Contains("Migration", adr, StringComparison.Ordinal);
        Assert.Contains("QueryFilter", adr, StringComparison.Ordinal);
        Assert.Contains("SQL 审计", adr, StringComparison.Ordinal);
        Assert.Contains("WeCms.Data.SqlSugar", adr, StringComparison.Ordinal);
        Assert.Contains("WeCms.Modules.*.SqlSugar", adr, StringComparison.Ordinal);
        Assert.Contains("旧 " + LegacyBoundaryNames.Persistence + " 不作为长期合法项目", adr, StringComparison.Ordinal);
    }

    [Fact]
    public void SqlSugarUsage_IsLimitedToCurrentOrTargetDataProjects()
    {
        var violations = ProductionFiles()
            .Where(file => ContainsAnyDatabaseToken(file))
            .Where(file => !IsAllowedDatabaseProject(file))
            .Select(RelativePath)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            "Database/ORM tokens are only allowed in current transition or target data projects:"
            + Environment.NewLine
            + string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void RawSqlUsage_IsLimitedToExplicitDataBoundaries()
    {
        var violations = ProductionFiles()
            .Where(file => ContainsAnyRawSqlToken(file))
            .Where(file => !IsAllowedRawSqlBoundary(file))
            .Select(RelativePath)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            "Raw SQL and Ado APIs are only allowed in explicit data boundaries because SqlSugar QueryFilter does not govern raw SQL automatically:"
            + Environment.NewLine
            + string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void BusinessModules_DoNotBypassQueryFilterWithRawSql()
    {
        var violations = ProductionFiles()
            .Where(IsUnderBusinessModuleProject)
            .Where(file => ContainsAnyRawSqlToken(file) || ContainsAnyDatabaseToken(file))
            .Select(RelativePath)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            "Business modules must not use raw SQL, Ado APIs, or ORM clients directly; put data access in Modules.*.SqlSugar and account for soft-delete/tenant/data-scope filters explicitly:"
            + Environment.NewLine
            + string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void FinalSqlSugarPlatformMode_RejectsLegacyPersistenceProject()
    {
        if (!string.Equals(Environment.GetEnvironmentVariable(FinalDataPlatformFlag), "true", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var persistenceProject = Path.Combine(TestPaths.SourceRoot, LegacyBoundaryNames.Persistence, LegacyBoundaryNames.PersistenceProject);
        Assert.False(File.Exists(persistenceProject), "Final SqlSugar platform mode does not allow " + LegacyBoundaryNames.Persistence + ".");
    }

    private static IEnumerable<string> ProductionFiles()
    {
        return Directory.EnumerateFiles(TestPaths.SourceRoot, "*.*", SearchOption.AllDirectories)
            .Where(file => file.EndsWith(".cs", StringComparison.Ordinal) || file.EndsWith(".csproj", StringComparison.Ordinal))
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal));
    }

    private static bool IsAllowedDatabaseProject(string file)
    {
        if (IsFinalDataPlatformMode())
        {
            return IsUnderProject(file, "WeCms.Data.SqlSugar") || IsUnderModuleSqlSugarProject(file);
        }

        return IsUnderProject(file, LegacyBoundaryNames.Persistence)
            || IsUnderProject(file, "WeCms.Data.SqlSugar")
            || IsUnderProject(file, "WeCms.EventBus.SqlSugar")
            || IsUnderModuleSqlSugarProject(file);
    }

    private static bool IsAllowedRawSqlBoundary(string file)
    {
        return IsUnderProject(file, "WeCms.Data.SqlSugar")
            || IsUnderProject(file, "WeCms.EventBus.SqlSugar")
            || IsUnderModuleSqlSugarProject(file);
    }

    private static bool IsFinalDataPlatformMode()
    {
        return string.Equals(Environment.GetEnvironmentVariable(FinalDataPlatformFlag), "true", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsUnderProject(string file, string projectName)
    {
        var projectRoot = Path.Combine(TestPaths.SourceRoot, projectName);
        return file.StartsWith(projectRoot + Path.DirectorySeparatorChar, StringComparison.Ordinal);
    }

    private static bool IsUnderModuleSqlSugarProject(string file)
    {
        var relative = Path.GetRelativePath(TestPaths.SourceRoot, file);
        var firstSegment = relative.Split(Path.DirectorySeparatorChar)[0];
        return firstSegment.StartsWith("WeCms.Modules.", StringComparison.Ordinal)
            && firstSegment.EndsWith(".SqlSugar", StringComparison.Ordinal);
    }

    private static bool IsUnderBusinessModuleProject(string file)
    {
        var relative = Path.GetRelativePath(TestPaths.SourceRoot, file);
        var firstSegment = relative.Split(Path.DirectorySeparatorChar)[0];
        return firstSegment.StartsWith("WeCms.Modules.", StringComparison.Ordinal)
            && !firstSegment.EndsWith(".SqlSugar", StringComparison.Ordinal);
    }

    private static bool ContainsAnyDatabaseToken(string file)
    {
        var source = File.ReadAllText(file);
        return DatabaseTokens.Any(token => source.Contains(token, StringComparison.Ordinal));
    }

    private static bool ContainsAnyRawSqlToken(string file)
    {
        var source = File.ReadAllText(file);
        return RawSqlTokens.Any(token => source.Contains(token, StringComparison.OrdinalIgnoreCase));
    }

    private static string RelativePath(string file)
    {
        return Path.GetRelativePath(TestPaths.RepoRoot, file);
    }
}
