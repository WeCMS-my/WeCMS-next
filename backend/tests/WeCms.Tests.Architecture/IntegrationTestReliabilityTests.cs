namespace WeCms.Tests.Architecture;

public sealed class IntegrationTestReliabilityTests
{
    private static readonly string RepoRoot = GetRepositoryRoot();

    [Theory]
    [InlineData("backend/tests/WeCms.Tests.Integration/Auth/AuthRefreshConcurrencyTests.cs")]
    [InlineData("backend/tests/WeCms.Tests.Integration/Auth/AuthLogoutTests.cs")]
    public void AuthIntegrationTests_ShouldFail_WhenTestHostCannotStart(string relativePath)
    {
        var filePath = Path.Combine(RepoRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Assert.True(File.Exists(filePath), $"Expected integration test file was not found: {filePath}");

        var source = File.ReadAllText(filePath);

        Assert.DoesNotContain("_skipReason", source, StringComparison.Ordinal);
        Assert.DoesNotContain("catch (Exception", source, StringComparison.Ordinal);
        Assert.DoesNotContain("if (_client is null", source, StringComparison.Ordinal);
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
