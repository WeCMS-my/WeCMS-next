namespace WeCms.Tests.Architecture;

public sealed class S11AopAttributeBoundaryTests
{
    private static readonly string[] AopAttributeTokens =
    [
        Forbidden("Unit", "Of", "Work"),
        Forbidden("Cache", "able"),
        Forbidden("Cache", "Evict"),
        Forbidden("Audit", "ed")
    ];

    [Fact]
    public void RepositoryTypes_AreNotAnnotatedForAop()
    {
        var offenders = FindAnnotatedFiles(file => Path.GetFileName(file).Contains("Repository", StringComparison.Ordinal));

        Assert.Empty(offenders);
    }

    [Fact]
    public void EndpointHandlers_AreNotAnnotatedForAop()
    {
        var offenders = FindAnnotatedFiles(file => Path.GetFileName(file).Contains("Endpoint", StringComparison.Ordinal));

        Assert.Empty(offenders);
    }

    [Fact]
    public void DomainEntities_AreNotAnnotatedForAop()
    {
        var offenders = FindAnnotatedFiles(file =>
            file.Contains($"{Path.DirectorySeparatorChar}Entities{Path.DirectorySeparatorChar}", StringComparison.Ordinal));

        Assert.Empty(offenders);
    }

    private static string[] FindAnnotatedFiles(Func<string, bool> fileFilter)
    {
        return Directory.GetFiles(TestPaths.SourceRoot, "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}WeCms.Aop{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(fileFilter)
            .SelectMany(file =>
            {
                var source = File.ReadAllText(file);
                return AopAttributeTokens
                    .Where(token => source.Contains($"[{token}", StringComparison.Ordinal))
                    .Select(token => $"{Path.GetRelativePath(TestPaths.RepoRoot, file)} contains [{token}]");
            })
            .ToArray();
    }

    private static string Forbidden(params string[] parts)
    {
        return string.Concat(parts);
    }
}
