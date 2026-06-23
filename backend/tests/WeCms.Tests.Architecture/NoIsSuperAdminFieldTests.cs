namespace WeCms.Tests.Architecture;

public sealed class NoIsSuperAdminFieldTests
{
    private static readonly string[] ForbiddenTokens =
    [
        "isSuperAdmin",
        "IsSuperAdmin",
        "is_super_admin"
    ];

    private static readonly string[] ScanRoots =
    [
        Path.Combine(TestPaths.SourceRoot, "WeCms.Api"),
        Path.Combine(TestPaths.SourceRoot, "WeCms.Modules.AccessControl"),
        Path.Combine(TestPaths.SourceRoot, "WeCms.Modules.AccessControl.SqlSugar"),
        Path.Combine(TestPaths.SourceRoot, "WeCms.Modules.Identity"),
        Path.Combine(TestPaths.SourceRoot, "WeCms.Modules.Identity.SqlSugar"),
        Path.Combine(TestPaths.SourceRoot, "WeCms.Modules.Security"),
        Path.Combine(TestPaths.SourceRoot, "WeCms.Modules.Security.SqlSugar"),
        Path.Combine(TestPaths.RepoRoot, "database", "migrations"),
        Path.Combine(TestPaths.RepoRoot, "database", "seeds"),
        Path.Combine(TestPaths.RepoRoot, "frontend", "soybean-admin", "src", "api", "types"),
        Path.Combine(TestPaths.RepoRoot, "frontend", "soybean-admin", "src", "views", "system", "users"),
        Path.Combine(TestPaths.RepoRoot, "artifacts", "openapi")
    ];

    [Fact]
    public void ActiveSourcesContractsAndSeeds_DoNotContainIsSuperAdminUserFlag()
    {
        var offenders = new List<string>();

        foreach (var root in ScanRoots.Where(Directory.Exists))
        {
            foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
            {
                if (file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    || file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                {
                    continue;
                }

                var text = File.ReadAllText(file);
                foreach (var token in ForbiddenTokens)
                {
                    if (text.Contains(token, StringComparison.Ordinal))
                    {
                        offenders.Add($"{Path.GetRelativePath(TestPaths.RepoRoot, file)} contains {token}");
                    }
                }
            }
        }

        Assert.True(offenders.Count == 0, string.Join(Environment.NewLine, offenders));
    }
}
