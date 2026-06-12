namespace WeCms.Tests.Architecture;

public sealed class DatabaseSeedSafetyTests
{
    private static readonly string RepoRoot = GetRepositoryRoot();

    [Fact]
    public void SqlSeeds_ShouldNotPersistRuntimePasswordHashPlaceholders()
    {
        var seedDir = Path.Combine(RepoRoot, "database", "seeds");
        var matches = new List<string>();

        foreach (var seedFile in Directory.EnumerateFiles(seedDir, "*.sql", SearchOption.TopDirectoryOnly))
        {
            var lines = File.ReadAllLines(seedFile);
            for (var i = 0; i < lines.Length; i++)
            {
                if (lines[i].Contains("PLACEHOLDER_RUNTIME_HASH", StringComparison.Ordinal))
                {
                    matches.Add($"{Path.GetRelativePath(RepoRoot, seedFile)}:{i + 1}: {lines[i].Trim()}");
                }
            }
        }

        Assert.Empty(matches);
    }

    private static string GetRepositoryRoot()
    {
        var directoryCandidates = new[]
        {
            Directory.GetCurrentDirectory(),
            AppContext.BaseDirectory
        };

        foreach (var candidate in directoryCandidates)
        {
            var current = new DirectoryInfo(candidate);
            while (current is not null)
            {
                if (File.Exists(Path.Combine(current.FullName, "backend", "WeCms.sln")) ||
                    File.Exists(Path.Combine(current.FullName, "backend", "WeCms.slnx")))
                {
                    return current.FullName;
                }

                current = current.Parent;
            }
        }

        throw new DirectoryNotFoundException("Could not locate repository root containing backend/WeCms.sln");
    }
}
