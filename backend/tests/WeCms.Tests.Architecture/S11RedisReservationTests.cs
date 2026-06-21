namespace WeCms.Tests.Architecture;

public sealed class S11RedisReservationTests
{
    [Fact]
    public void RedisPackage_IsNotIntroducedWithoutAdr()
    {
        var projectFiles = Directory.GetFiles(TestPaths.BackendRoot, "*.csproj", SearchOption.AllDirectories);

        var offenders = projectFiles
            .Where(file => File.ReadAllText(file).Contains(Forbidden("StackExchange", ".", "Red", "is"), StringComparison.OrdinalIgnoreCase))
            .Select(file => Path.GetRelativePath(TestPaths.RepoRoot, file))
            .ToArray();

        Assert.Empty(offenders);
    }

    [Fact]
    public void BusinessCode_DoesNotReferenceRedisDirectly()
    {
        var businessRoots = new[]
        {
            Path.Combine(TestPaths.SourceRoot, "WeCms.Api"),
            Path.Combine(TestPaths.SourceRoot, "WeCms.Modules.Identity"),
            Path.Combine(TestPaths.SourceRoot, "WeCms.Modules.AccessControl"),
            Path.Combine(TestPaths.SourceRoot, "WeCms.Modules.Organization"),
            Path.Combine(TestPaths.SourceRoot, "WeCms.Modules.Configuration"),
            Path.Combine(TestPaths.SourceRoot, "WeCms.Modules.Audit"),
            Path.Combine(TestPaths.SourceRoot, "WeCms.Modules.Security"),
            Path.Combine(TestPaths.SourceRoot, "WeCms.Modules.FileCenter"),
            Path.Combine(TestPaths.SourceRoot, "WeCms.Modules.Platform"),
            Path.Combine(TestPaths.SourceRoot, "WeCms.Modules.Cms")
        };

        var token = Forbidden("Red", "is");
        var offenders = businessRoots
            .Where(Directory.Exists)
            .SelectMany(root => Directory.GetFiles(root, "*.cs", SearchOption.AllDirectories))
            .Where(file => File.ReadAllText(file).Contains(token, StringComparison.OrdinalIgnoreCase))
            .Select(file => Path.GetRelativePath(TestPaths.RepoRoot, file))
            .ToArray();

        Assert.Empty(offenders);
    }

    [Fact]
    public void CachingSource_DoesNotReferenceRedisRuntimeTypes()
    {
        var cachingRoot = Path.Combine(TestPaths.SourceRoot, "WeCms.Caching");
        var runtimeTokens = new[]
        {
            Forbidden("Connection", "Multiplexer"),
            Forbidden("IDatabase"),
            Forbidden("IConnection", "Multiplexer"),
            Forbidden("Add", "StackExchange", "Redis")
        };

        var offenders = Directory.GetFiles(cachingRoot, "*.cs", SearchOption.AllDirectories)
            .SelectMany(file =>
            {
                var source = File.ReadAllText(file);
                return runtimeTokens
                    .Where(token => source.Contains(token, StringComparison.Ordinal))
                    .Select(token => $"{Path.GetRelativePath(TestPaths.RepoRoot, file)} contains {token}");
            })
            .ToArray();

        Assert.Empty(offenders);
    }

    private static string Forbidden(params string[] parts)
    {
        return string.Concat(parts);
    }
}
