using System.Text.RegularExpressions;

namespace WeCms.Tests.Architecture;

public sealed class P3AuditContextConsistencyTests
{
    private static readonly string[] RequiredAuditColumns =
    [
        "user_id",
        "username",
        "target_id",
        "request_method",
        "request_path",
        "ip_address",
        "user_agent",
        "trace_id"
    ];

    [Fact]
    public void SysAuditLogInserts_IncludeContextColumns()
    {
        var violations = Directory.EnumerateFiles(TestPaths.SourceRoot, "*.cs", SearchOption.AllDirectories)
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(file => !file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .SelectMany(FindAuditInsertViolations)
            .ToArray();

        Assert.True(
            violations.Length == 0,
            "sys_audit_log inserts must include actor, request context, trace, and target columns:"
            + Environment.NewLine
            + string.Join(Environment.NewLine, violations));
    }

    [Fact]
    public void ApplicationServiceAopAudit_HttpContextFieldsAreDocumentedAsNotApplicable()
    {
        var aopSource = File.ReadAllText(Path.Combine(TestPaths.SourceRoot, "WeCms.Aop", "ApplicationServiceAopInterceptor.cs"));
        var p3Spec = File.ReadAllText(Path.Combine(TestPaths.RepoRoot, "docs", "specs", "p3-foundation-hardening", "spec.md"));

        Assert.Contains("AuditRequestMethod = \"SERVICE\"", aopSource, StringComparison.Ordinal);
        Assert.Contains("Application Service AOP audit", p3Spec, StringComparison.Ordinal);
        Assert.Contains("HTTP request context is N/A", p3Spec, StringComparison.Ordinal);
    }

    private static IEnumerable<string> FindAuditInsertViolations(string file)
    {
        var source = File.ReadAllText(file);
        foreach (Match match in Regex.Matches(
            source,
            @"INSERT\s+INTO\s+sys_audit_log\s*\((?<columns>[^)]*)\)",
            RegexOptions.IgnoreCase | RegexOptions.Singleline))
        {
            var columns = match.Groups["columns"].Value;
            foreach (var column in RequiredAuditColumns)
            {
                if (!Regex.IsMatch(columns, $@"\b{Regex.Escape(column)}\b", RegexOptions.IgnoreCase))
                {
                    yield return $"{Path.GetRelativePath(TestPaths.RepoRoot, file)} missing {column}";
                }
            }
        }
    }
}
