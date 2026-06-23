using System.Text.RegularExpressions;

namespace WeCms.Tests.Architecture;

public sealed class P3RawSqlPredicateBuilderTests
{
    [Fact]
    public void P3ReviewedRawSqlBlocks_UsePredicateBuilderOrExplicitException()
    {
        var violations = Directory.EnumerateFiles(TestPaths.SourceRoot, "*.cs", SearchOption.AllDirectories)
            .Where(file => file.Contains(".SqlSugar", StringComparison.Ordinal))
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .SelectMany(FindReviewedRawSqlViolations)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            "P3-reviewed guarded raw SQL must use PredicateBuilder or carry an explicit exception:"
            + Environment.NewLine
            + string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void RawSqlGuardrailDocs_RequirePredicateBuilderFirstForNewGuardedSql()
    {
        var docs = File.ReadAllText(Path.Combine(TestPaths.RepoRoot, "docs", "specs", "s10-data-platform-upgrade", "query-filter-raw-sql.md"));

        Assert.Contains("Prefer `SoftDeleteSqlPredicateBuilder`", docs, StringComparison.Ordinal);
        Assert.Contains("Prefer `TenantSqlPredicateBuilder`", docs, StringComparison.Ordinal);
        Assert.Contains("Prefer `DataScopeSqlPredicateBuilder`", docs, StringComparison.Ordinal);
        Assert.Contains("explicit exception", docs, StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> FindReviewedRawSqlViolations(string file)
    {
        var lines = File.ReadAllLines(file);
        for (var index = 0; index < lines.Length; index++)
        {
            if (!lines[index].Contains("P3 raw-sql-reviewed", StringComparison.Ordinal))
            {
                continue;
            }

            var reviewWindow = string.Join(
                '\n',
                lines.Skip(index).Take(10));
            if (Regex.IsMatch(reviewWindow, @"(?:SoftDelete|Tenant|DataScope)SqlPredicateBuilder\.Build") ||
                reviewWindow.Contains("P3 raw-sql-exception:", StringComparison.Ordinal))
            {
                continue;
            }

            yield return $"{Path.GetRelativePath(TestPaths.RepoRoot, file)}:{index + 1}";
        }
    }
}
