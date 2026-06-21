namespace WeCms.Tests.Architecture;

public sealed class S11CachingBoundaryTests
{
    [Fact]
    public void CachingProject_DoesNotReferenceOutOfScopeInfrastructure()
    {
        var projectFile = Path.Combine(TestPaths.SourceRoot, "WeCms.Caching", "WeCms.Caching.csproj");
        var content = File.ReadAllText(projectFile);

        Assert.DoesNotContain(Forbidden("Entity", "Framework"), content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(Forbidden("StackExchange", ".", "Red", "is"), content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(Forbidden("Auto", "fac"), content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(Forbidden("Castle", ".", "Core"), content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void CachingSource_DoesNotIntroduceForbiddenRuntimeBoundaries()
    {
        var source = ReadCachingSource();

        Assert.DoesNotContain(Forbidden("Controller"), source, StringComparison.Ordinal);
        Assert.DoesNotContain(Forbidden("Add", "Controllers"), source, StringComparison.Ordinal);
        Assert.DoesNotContain(Forbidden("Map", "Controllers"), source, StringComparison.Ordinal);
        Assert.DoesNotContain(Forbidden("Raz", "or"), source, StringComparison.Ordinal);
        Assert.DoesNotContain(Forbidden("Db", "Context"), source, StringComparison.Ordinal);
        Assert.DoesNotContain(Forbidden("Sql", "Sugar"), source, StringComparison.Ordinal);
        Assert.DoesNotContain(Forbidden("Event", "Bus"), source, StringComparison.Ordinal);
        Assert.DoesNotContain(Forbidden("Out", "box"), source, StringComparison.Ordinal);
        Assert.DoesNotContain(Forbidden("Mini", "Profiler"), source, StringComparison.Ordinal);
        Assert.DoesNotContain(Forbidden("Sca", "lar"), source, StringComparison.Ordinal);
        Assert.DoesNotContain(Forbidden("A", "I"), source, StringComparison.Ordinal);
    }

    [Fact]
    public void CachingSource_DoesNotUseSyncBlocking()
    {
        var source = ReadCachingSource();

        Assert.DoesNotContain(Forbidden(".", "Result"), source, StringComparison.Ordinal);
        Assert.DoesNotContain(Forbidden(".", "Wait", "("), source, StringComparison.Ordinal);
        Assert.DoesNotContain(Forbidden("Wait", "All", "("), source, StringComparison.Ordinal);
    }

    private static string ReadCachingSource()
    {
        var sourceDirectory = Path.Combine(TestPaths.SourceRoot, "WeCms.Caching");
        var files = Directory.GetFiles(sourceDirectory, "*.cs", SearchOption.AllDirectories);

        return string.Join(Environment.NewLine, files.Select(File.ReadAllText));
    }

    private static string Forbidden(params string[] parts)
    {
        return string.Concat(parts);
    }
}
