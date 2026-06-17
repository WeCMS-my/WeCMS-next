using System.Text.RegularExpressions;

namespace WeCms.Tests.Architecture;

public sealed partial class PersistenceBoundaryTests
{
    private static readonly string[] ForbiddenDatabaseTokens =
    [
        "SqlSugarCore",
        "SqlSugarClient",
        "SqlSugarScope",
        "ISqlSugarClient",
        "MySqlConnection",
        "MySqlConnector",
        "DbConnection",
        "DbTransaction"
    ];

    [Fact]
    public void OnlyPersistenceProject_CanReferenceSqlSugarOrMySql()
    {
        var violations = ProductionFiles()
            .Where(file => !IsUnderProject(file, "WeCms.Persistence"))
            .SelectMany(file => ForbiddenDatabaseTokens
                .Where(token => File.ReadAllText(file).Contains(token, StringComparison.Ordinal))
                .Select(token => $"{RelativePath(file)} contains {token}"))
            .ToArray();

        Assert.True(violations.Length == 0, string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void Modules_DoNotContainSqlText()
    {
        var violations = ProductionFiles()
            .Where(file => IsUnderProject(file, "WeCms.Modules.System") || IsUnderProject(file, "WeCms.Modules.Cms"))
            .Where(file => SqlKeywordPattern().IsMatch(File.ReadAllText(file)))
            .Select(RelativePath)
            .ToArray();

        Assert.True(violations.Length == 0, $"Module files contain SQL keywords:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    [Fact]
    public void UserRepository_LocksEnabledLockedRoleHoldersWhenCounting()
    {
        var source = File.ReadAllText(Path.Combine(
            TestPaths.SourceRoot,
            "WeCms.Persistence",
            "Modules",
            "System",
            "Users",
            "UserRepository.cs"));

        Assert.Contains("CountEnabledUsersByRoleForUpdateAsync", source, StringComparison.Ordinal);
        Assert.Contains("FOR UPDATE", source, StringComparison.Ordinal);
    }

    private static IEnumerable<string> ProductionFiles()
    {
        return Directory.EnumerateFiles(TestPaths.SourceRoot, "*.*", SearchOption.AllDirectories)
            .Where(file => file.EndsWith(".cs", StringComparison.Ordinal) || file.EndsWith(".csproj", StringComparison.Ordinal))
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal));
    }

    private static bool IsUnderProject(string file, string projectName)
    {
        var projectRoot = Path.Combine(TestPaths.SourceRoot, projectName);

        return file.StartsWith(projectRoot + Path.DirectorySeparatorChar, StringComparison.Ordinal);
    }

    private static string RelativePath(string file)
    {
        return Path.GetRelativePath(TestPaths.RepoRoot, file);
    }

    [GeneratedRegex("""\b(SELECT\s+.+\s+FROM|INSERT\s+INTO|UPDATE\s+[A-Za-z_][A-Za-z0-9_]*|DELETE\s+FROM)\b""", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SqlKeywordPattern();
}
