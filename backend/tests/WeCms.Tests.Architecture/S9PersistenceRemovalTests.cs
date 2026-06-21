namespace WeCms.Tests.Architecture;

public sealed class S9PersistenceRemovalTests
{
    [Fact]
    public void LegacyPersistenceProject_IsRemoved()
    {
        var persistenceProject = Path.Combine(TestPaths.SourceRoot, LegacyBoundaryNames.Persistence, LegacyBoundaryNames.PersistenceProject);

        Assert.False(File.Exists(persistenceProject), "Sprint 9 final state does not allow the legacy " + LegacyBoundaryNames.Persistence + " project.");
    }

    [Fact]
    public void ProductionSource_DoesNotReferenceLegacyPersistenceNamespace()
    {
        var offenders = new List<string>();

        foreach (var file in Directory.EnumerateFiles(TestPaths.SourceRoot, "*.cs", SearchOption.AllDirectories))
        {
            if (file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            var source = File.ReadAllText(file);
            if (source.Contains(LegacyBoundaryNames.Persistence, StringComparison.Ordinal))
            {
                offenders.Add(Path.GetRelativePath(TestPaths.RepoRoot, file));
            }
        }

        Assert.Empty(offenders);
    }

    [Fact]
    public void ProjectFiles_DoNotReferenceLegacyPersistenceProject()
    {
        var offenders = new List<string>();
        var projectFiles = Directory.EnumerateFiles(
                Path.Combine(TestPaths.RepoRoot, "backend"),
                "*.*",
                SearchOption.AllDirectories)
            .Where(file => file.EndsWith(".csproj", StringComparison.Ordinal) || file.EndsWith(".slnx", StringComparison.Ordinal));

        foreach (var file in projectFiles)
        {
            var source = File.ReadAllText(file);
            if (source.Contains(LegacyBoundaryNames.Persistence, StringComparison.Ordinal))
            {
                offenders.Add(Path.GetRelativePath(TestPaths.RepoRoot, file));
            }
        }

        Assert.Empty(offenders);
    }

    [Fact]
    public void UnitAndIntegrationTests_DoNotReferenceLegacyPersistenceNamespace()
    {
        var offenders = new List<string>();
        var testRoots = new[]
        {
            Path.Combine(TestPaths.BackendRoot, "tests", "WeCms.Tests.Unit"),
            Path.Combine(TestPaths.BackendRoot, "tests", "WeCms.Tests.Integration")
        };

        foreach (var root in testRoots)
        {
            foreach (var file in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                if (file.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                    || file.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
                {
                    continue;
                }

                var source = File.ReadAllText(file);
                if (source.Contains(LegacyBoundaryNames.Persistence, StringComparison.Ordinal))
                {
                    offenders.Add(Path.GetRelativePath(TestPaths.RepoRoot, file));
                }
            }
        }

        Assert.Empty(offenders);
    }
}
