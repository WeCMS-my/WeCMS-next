using System.Text.RegularExpressions;

namespace WeCms.Tests.Architecture;

public sealed partial class DiBoundaryTests
{
    [Fact]
    public void BusinessCode_DoesNotInstantiateSideEffectDependencies()
    {
        var violations = Directory.EnumerateFiles(TestPaths.SourceRoot, "*.cs", SearchOption.AllDirectories)
            .Where(file => IsBusinessProject(file))
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .SelectMany(file => ForbiddenInstantiationPatterns()
                .Where(pattern => pattern.Regex.IsMatch(File.ReadAllText(file)))
                .Select(pattern => $"{Path.GetRelativePath(TestPaths.RepoRoot, file)} matches {pattern.Description}"))
            .ToArray();

        Assert.True(violations.Length == 0, string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void BusinessConstructors_DoNotDependOnConcreteRepositoryImplementations()
    {
        var violations = Directory.EnumerateFiles(TestPaths.SourceRoot, "*.cs", SearchOption.AllDirectories)
            .Where(file => IsBusinessProject(file))
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(file => ConcreteRepositoryConstructorParameterPattern().IsMatch(File.ReadAllText(file)))
            .Select(file => Path.GetRelativePath(TestPaths.RepoRoot, file))
            .ToArray();

        Assert.True(violations.Length == 0, $"Business constructors depend on concrete repositories:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    private static bool IsBusinessProject(string file)
    {
        return file.Contains($"{Path.DirectorySeparatorChar}WeCms.Modules.System{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
            || file.Contains($"{Path.DirectorySeparatorChar}WeCms.Modules.Cms{Path.DirectorySeparatorChar}", StringComparison.Ordinal);
    }

    private static (Regex Regex, string Description)[] ForbiddenInstantiationPatterns()
    {
        return
        [
            (RepositoryConstructionPattern(), "direct Repository construction"),
            (SqlSugarConstructionPattern(), "direct SqlSugar construction"),
            (MySqlConstructionPattern(), "direct MySQL construction"),
            (HttpClientConstructionPattern(), "direct HttpClient construction"),
            (DateTimeUtcNowPattern(), "DateTime.UtcNow"),
            (GuidNewGuidPattern(), "Guid.NewGuid"),
            (RandomSharedPattern(), "Random.Shared")
        ];
    }

    [GeneratedRegex(@"\bnew\s+\w*Repository\s*\(", RegexOptions.CultureInvariant)]
    private static partial Regex RepositoryConstructionPattern();

    [GeneratedRegex(@"\bnew\s+(SqlSugarClient|SqlSugarScope)\s*\(", RegexOptions.CultureInvariant)]
    private static partial Regex SqlSugarConstructionPattern();

    [GeneratedRegex(@"\bnew\s+MySqlConnection\s*\(", RegexOptions.CultureInvariant)]
    private static partial Regex MySqlConstructionPattern();

    [GeneratedRegex(@"\bnew\s+HttpClient\s*\(", RegexOptions.CultureInvariant)]
    private static partial Regex HttpClientConstructionPattern();

    [GeneratedRegex(@"\bDateTime\.UtcNow\b", RegexOptions.CultureInvariant)]
    private static partial Regex DateTimeUtcNowPattern();

    [GeneratedRegex(@"\bGuid\.NewGuid\s*\(", RegexOptions.CultureInvariant)]
    private static partial Regex GuidNewGuidPattern();

    [GeneratedRegex(@"\bRandom\.Shared\b", RegexOptions.CultureInvariant)]
    private static partial Regex RandomSharedPattern();

    [GeneratedRegex(@"[(,]\s*(?:[A-Za-z_][\w.]*\.)?(?!I[A-Z])\w*Repository\s+\w+", RegexOptions.CultureInvariant)]
    private static partial Regex ConcreteRepositoryConstructorParameterPattern();
}
