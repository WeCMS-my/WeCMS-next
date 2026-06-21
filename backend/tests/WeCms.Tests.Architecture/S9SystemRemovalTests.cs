namespace WeCms.Tests.Architecture;

public sealed class S9SystemRemovalTests
{
    [Fact]
    public void LegacySystemProject_IsRemoved()
    {
        var systemProject = Path.Combine(TestPaths.SourceRoot, LegacyBoundaryNames.SystemModule, LegacyBoundaryNames.SystemProject);

        Assert.False(File.Exists(systemProject), "Sprint 9 final state does not allow the legacy " + LegacyBoundaryNames.SystemModule + " project.");
    }

    [Fact]
    public async Task BackendProductionSource_DoesNotReferenceLegacySystemNamespace()
    {
        var offenders = new List<string>();
        foreach (var path in Directory.EnumerateFiles(TestPaths.SourceRoot, "*.cs", SearchOption.AllDirectories))
        {
            if (path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal)
                || path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            {
                continue;
            }

            var source = await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken);
            if (source.Contains(LegacyBoundaryNames.SystemModule, StringComparison.Ordinal))
            {
                offenders.Add(Path.GetRelativePath(TestPaths.RepoRoot, path));
            }
        }

        Assert.True(
            offenders.Count == 0,
            "Legacy " + LegacyBoundaryNames.SystemModule + " references remain: " + string.Join(", ", offenders.Order(StringComparer.Ordinal)));
    }

    [Fact]
    public async Task BackendProjectFiles_DoNotReferenceLegacySystemProject()
    {
        var projectFiles = Directory
            .EnumerateFiles(TestPaths.BackendRoot, "*.*proj", SearchOption.AllDirectories)
            .Concat([Path.Combine(TestPaths.BackendRoot, "WeCms.slnx")])
            .Where(File.Exists)
            .ToArray();

        var offenders = new List<string>();
        foreach (var path in projectFiles)
        {
            var source = await File.ReadAllTextAsync(path, TestContext.Current.CancellationToken);
            if (source.Contains(LegacyBoundaryNames.SystemModule, StringComparison.Ordinal))
            {
                offenders.Add(Path.GetRelativePath(TestPaths.RepoRoot, path));
            }
        }

        Assert.True(
            offenders.Count == 0,
            "Legacy " + LegacyBoundaryNames.SystemModule + " project references remain: " + string.Join(", ", offenders.Order(StringComparer.Ordinal)));
    }
}
