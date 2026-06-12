using WeCms.Api.Extensions;
using WeCms.Modules.System.System;

namespace WeCms.Tests.Architecture;

public sealed class EndpointRegistrationDependencyInjectionTests
{
    [Theory]
    [InlineData("backend/src/WeCms.Api/Extensions/AuthEndpointMappings.cs")]
    [InlineData("backend/src/WeCms.Modules.System/System/SystemEndpoints.cs")]
    public void EndpointRegistrationFiles_ShouldNotUse_ServiceLocator(string relativePath)
    {
        var repositoryRoot = FindRepositoryRoot();
        var filePath = Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));

        Assert.True(File.Exists(filePath), $"Expected source file was not found: {filePath}");

        var source = File.ReadAllText(filePath);

        Assert.DoesNotContain("RequestServices.GetRequiredService", source, StringComparison.Ordinal);
        Assert.DoesNotContain("RequestServices.GetService", source, StringComparison.Ordinal);
    }

    [Fact]
    public void EndpointRegistrationTypes_ShouldRemainDiscoverable()
    {
        Assert.Equal("WeCms.Api.Extensions", typeof(AuthEndpointMappings).Namespace);
        Assert.Equal("WeCms.Modules.System.System", typeof(SystemEndpoints).Namespace);
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);

        while (current is not null)
        {
            var agentsPath = Path.Combine(current.FullName, "AGENTS.md");
            var backendPath = Path.Combine(current.FullName, "backend");
            if (File.Exists(agentsPath) && Directory.Exists(backendPath))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Could not locate repository root from test output directory.");
    }
}
