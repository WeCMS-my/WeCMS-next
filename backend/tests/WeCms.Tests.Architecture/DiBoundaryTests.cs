using System.Text.RegularExpressions;

namespace WeCms.Tests.Architecture;

public sealed partial class DiBoundaryTests
{
    private static readonly string[] GuidNewGuidAllowedFiles =
    [
        Path.Combine("WeCms.Infrastructure", "Id", "SystemIdGenerator.cs")
    ];

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
    public void ProductionCode_UsesIdGeneratorInsteadOfDirectGuidCreation()
    {
        var violations = Directory.EnumerateFiles(TestPaths.SourceRoot, "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(file => GuidNewGuidPattern().IsMatch(File.ReadAllText(file)))
            .Where(file => !IsAllowedGuidGeneratorFile(file))
            .Select(file => Path.GetRelativePath(TestPaths.RepoRoot, file))
            .ToArray();

        Assert.True(violations.Length == 0, $"Production code must use IIdGenerator instead of Guid.NewGuid:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
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

    [Fact]
    public void EndpointFilters_UseConstructorInjectionInsteadOfRequestServicesLookup()
    {
        var violations = Directory.EnumerateFiles(TestPaths.SourceRoot, "*.cs", SearchOption.AllDirectories)
            .Where(file => IsBusinessProject(file))
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(file => Path.GetFileName(file).EndsWith("Filter.cs", StringComparison.Ordinal))
            .Where(file =>
            {
                var text = File.ReadAllText(file);
                return text.Contains("IEndpointFilter", StringComparison.Ordinal)
                    && text.Contains("RequestServices.GetRequiredService", StringComparison.Ordinal);
            })
            .Select(file => Path.GetRelativePath(TestPaths.RepoRoot, file))
            .ToArray();

        Assert.True(violations.Length == 0, $"Endpoint filters must use constructor injection instead of RequestServices lookup:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    [Fact]
    public void BusinessRuntimeCode_DoesNotUseServiceLocator()
    {
        var violations = Directory.EnumerateFiles(TestPaths.SourceRoot, "*.cs", SearchOption.AllDirectories)
            .Where(file => IsBusinessProject(file))
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(file => !Path.GetFileName(file).EndsWith("ServiceCollectionExtensions.cs", StringComparison.Ordinal))
            .SelectMany(file => ServiceLocatorPatterns()
                .Where(pattern => pattern.Regex.IsMatch(File.ReadAllText(file)))
                .Select(pattern => $"{Path.GetRelativePath(TestPaths.RepoRoot, file)} matches {pattern.Description}"))
            .ToArray();

        Assert.True(violations.Length == 0, $"Business runtime code must use constructor injection instead of service locator APIs:{Environment.NewLine}{string.Join(Environment.NewLine, violations)}");
    }

    private static bool IsBusinessProject(string file)
    {
        var relative = Path.GetRelativePath(TestPaths.SourceRoot, file);
        var projectName = relative.Split(Path.DirectorySeparatorChar)[0];

        return projectName.StartsWith("WeCms.Modules.", StringComparison.Ordinal)
            && !projectName.EndsWith(".SqlSugar", StringComparison.Ordinal);
    }

    private static bool IsAllowedGuidGeneratorFile(string file)
    {
        var relativePath = Path.GetRelativePath(TestPaths.SourceRoot, file);
        return GuidNewGuidAllowedFiles.Any(allowed => string.Equals(relativePath, allowed, StringComparison.Ordinal));
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

    private static (Regex Regex, string Description)[] ServiceLocatorPatterns()
    {
        return
        [
            (GetRequiredServicePattern(), "GetRequiredService"),
            (GetServicePattern(), "GetService"),
            (RequestServicesPattern(), "RequestServices"),
            (BuildServiceProviderPattern(), "BuildServiceProvider"),
            (IServiceProviderFieldPattern(), "IServiceProvider field")
        ];
    }

    [GeneratedRegex(@"\.GetRequiredService\s*<", RegexOptions.CultureInvariant)]
    private static partial Regex GetRequiredServicePattern();

    [GeneratedRegex(@"\.GetService\s*<", RegexOptions.CultureInvariant)]
    private static partial Regex GetServicePattern();

    [GeneratedRegex(@"\bRequestServices\b", RegexOptions.CultureInvariant)]
    private static partial Regex RequestServicesPattern();

    [GeneratedRegex(@"\.BuildServiceProvider\s*\(", RegexOptions.CultureInvariant)]
    private static partial Regex BuildServiceProviderPattern();

    [GeneratedRegex(@"\bIServiceProvider\s+[_a-zA-Z]", RegexOptions.CultureInvariant)]
    private static partial Regex IServiceProviderFieldPattern();
}
