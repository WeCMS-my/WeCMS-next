namespace WeCms.Tests.Architecture;

public sealed class NoControllerArchitectureTests
{
    private const string MinimalApiAdr = "0017-minimal-api-remains-controller-forbidden.md";

    private static readonly string[] ForbiddenControllerTokens =
    [
        ": ControllerBase",
        ": Controller",
        "AddControllers(",
        "MapControllers(",
        "[ApiController]"
    ];

    [Fact]
    public void ProductionCode_DoesNotUseControllerApiSurface()
    {
        var violations = ProductionFiles()
            .SelectMany(file => ForbiddenControllerTokens
                .Where(token => File.ReadAllText(file).Contains(token, StringComparison.Ordinal))
                .Select(token => $"{RelativePath(file)} contains {token}"))
            .ToArray();

        Assert.True(violations.Length == 0, string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void MinimalApiControllerDecision_IsAcceptedAndReferencedByRules()
    {
        var adrPath = Path.Combine(TestPaths.RepoRoot, "docs", "adr", MinimalApiAdr);
        Assert.True(File.Exists(adrPath), $"Missing ADR: docs/adr/{MinimalApiAdr}");

        var adr = File.ReadAllText(adrPath);
        Assert.Contains("## 状态", adr, StringComparison.Ordinal);
        Assert.Contains("Accepted", adr, StringComparison.Ordinal);
        Assert.Contains("禁止 Controller", adr, StringComparison.Ordinal);
        Assert.Contains("禁止 ControllerBase", adr, StringComparison.Ordinal);
        Assert.Contains("禁止 AddControllers", adr, StringComparison.Ordinal);
        Assert.Contains("禁止 MapControllers", adr, StringComparison.Ordinal);

        AssertRuleReferencesAdr("AGENTS.md");
        AssertRuleReferencesAdr("code_review.md");
    }

    private static IEnumerable<string> ProductionFiles()
    {
        return Directory.EnumerateFiles(TestPaths.SourceRoot, "*.*", SearchOption.AllDirectories)
            .Where(file => file.EndsWith(".cs", StringComparison.Ordinal) || file.EndsWith(".csproj", StringComparison.Ordinal))
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal));
    }

    private static void AssertRuleReferencesAdr(string relativePath)
    {
        var source = File.ReadAllText(Path.Combine(TestPaths.RepoRoot, relativePath));
        Assert.Contains(MinimalApiAdr, source, StringComparison.Ordinal);
    }

    private static string RelativePath(string file)
    {
        return Path.GetRelativePath(TestPaths.RepoRoot, file);
    }
}
