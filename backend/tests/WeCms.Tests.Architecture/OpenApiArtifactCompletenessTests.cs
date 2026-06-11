using System.Text.Json;
using System.Text.Json.Nodes;

namespace WeCms.Tests.Architecture;

public sealed class OpenApiArtifactCompletenessTests
{
    private static readonly string[] ExpectedPaths =
    [
        "/health/live",
        "/health/ready",
        "/api/v1/system/ping",
        "/api/v1/system/version",
        "/api/v1/system/db-check",
        "/api/v1/system/secure-ping",
        "/api/v1/auth/login",
        "/api/v1/auth/refresh",
        "/api/v1/auth/logout",
        "/api/v1/auth/me",
    ];

    [Fact]
    public void OpenApiArtifact_ShouldUseStableServerUrl_AndContainAllRegisteredEndpoints()
    {
        using var document = JsonDocument.Parse(File.ReadAllText(GetArtifactPath()));

        var root = document.RootElement;
        Assert.Equal("http://localhost:5000/", root.GetProperty("servers")[0].GetProperty("url").GetString());

        var paths = root.GetProperty("paths").EnumerateObject()
            .Select(path => path.Name)
            .ToHashSet(StringComparer.Ordinal);

        var missingPaths = ExpectedPaths.Where(path => !paths.Contains(path)).ToArray();

        Assert.True(missingPaths.Length == 0, $"OpenAPI artifact is missing paths: {string.Join(", ", missingPaths)}");
    }

    private static string GetArtifactPath()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "AGENTS.md")))
            {
                return Path.Combine(current.FullName, "artifacts", "openapi", "wecms-api-v1.json");
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Unable to locate repository root.");
    }
}
