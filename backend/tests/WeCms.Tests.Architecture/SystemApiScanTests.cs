namespace WeCms.Tests.Architecture;

public sealed class SystemApiScanTests
{
    [Fact]
    public async Task ApiHost_ExplicitlyMapsSystemEndpoints()
    {
        var source = await File.ReadAllTextAsync(Path.Combine(
            TestPaths.RepoRoot,
            "backend",
            "src",
            "WeCms.Api",
            "Program.cs"));

        Assert.Contains("WebApplication.CreateSlimBuilder(args)", source, StringComparison.Ordinal);
        Assert.Contains("app.MapSystemEndpoints();", source, StringComparison.Ordinal);
        Assert.Contains("app.MapSystemPermissionEndpoints();", source, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SystemResponses_AreIncludedInJsonSerializerContext()
    {
        var source = await File.ReadAllTextAsync(Path.Combine(
            TestPaths.RepoRoot,
            "backend",
            "src",
            "WeCms.Api",
            "Json",
            "WeCmsJsonSerializerContext.cs"));

        Assert.Contains("ApiResult<SystemLiveResponse>", source, StringComparison.Ordinal);
        Assert.Contains("ApiResult<SystemReadyResponse>", source, StringComparison.Ordinal);
        Assert.Contains("ApiResult<SystemPingResponse>", source, StringComparison.Ordinal);
        Assert.Contains("ApiResult<SystemVersionResponse>", source, StringComparison.Ordinal);
        Assert.Contains("ApiResult<SystemDbCheckResponse>", source, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SystemAndAuthEndpoints_AllHaveExplicitAuthorizationOrPermissionIntent()
    {
        var authSource = await File.ReadAllTextAsync(Path.Combine(
            TestPaths.RepoRoot,
            "backend",
            "src",
            "WeCms.Modules.System",
            "Auth",
            "AuthEndpoints.cs"));
        var systemSource = await File.ReadAllTextAsync(Path.Combine(
            TestPaths.RepoRoot,
            "backend",
            "src",
            "WeCms.Modules.System",
            "System",
            "SystemEndpointExtensions.cs"));
        var permissionSource = await File.ReadAllTextAsync(Path.Combine(
            TestPaths.RepoRoot,
            "backend",
            "src",
            "WeCms.Modules.System",
            "Permissions",
            "PermissionEndpointExtensions.cs"));

        var declarations = CollectSystemAuthEndpointDeclarations(authSource)
            .Concat(CollectSystemAuthEndpointDeclarations(systemSource))
            .Concat(CollectSystemAuthEndpointDeclarations(permissionSource))
            .ToArray();

        Assert.NotEmpty(declarations);

        var missing = declarations
            .Where(endpoint => !endpoint.IsAuthenticationIntentExplicit)
            .Select(endpoint => $"{endpoint.Method} {endpoint.Path}")
            .ToArray();

        Assert.True(missing.Length == 0, "System/Auth endpoint missing explicit auth/permission intent: " + string.Join(", ", missing));
    }

    private static IEnumerable<SystemAuthEndpointDeclaration> CollectSystemAuthEndpointDeclarations(string source)
    {
        var methods = new[] { "Get", "Post", "Put", "Patch", "Delete" };

        foreach (var method in methods)
        {
            var token = $"Map{method}(\"";
            var searchStart = 0;
            while (true)
            {
                var methodIndex = source.IndexOf(token, searchStart, StringComparison.Ordinal);
                if (methodIndex < 0)
                {
                    break;
                }

                var pathStart = methodIndex + token.Length;
                var pathEnd = source.IndexOf('"', pathStart);
                if (pathEnd < 0)
                {
                    break;
                }

                var path = source[pathStart..pathEnd];
                if (!IsSystemOrAuthEndpoint(path))
                {
                    searchStart = methodIndex + 1;
                    continue;
                }

                var segmentEnd = source.IndexOf(");", methodIndex, StringComparison.Ordinal);
                if (segmentEnd < 0)
                {
                    segmentEnd = source.Length - 1;
                }

                var segment = source.AsSpan(methodIndex, segmentEnd - methodIndex + 2).ToString();
                yield return new SystemAuthEndpointDeclaration(
                    path,
                    method.ToUpperInvariant(),
                    HasAuthenticationIntent(segment));

                searchStart = methodIndex + 1;
            }
        }
    }

    private static bool HasAuthenticationIntent(string segment)
    {
        return segment.Contains(".AllowAnonymous()", StringComparison.Ordinal)
            || segment.Contains(".RequireAuthorization()", StringComparison.Ordinal)
            || segment.Contains(".RequirePermission(", StringComparison.Ordinal);
    }

    private static bool IsSystemOrAuthEndpoint(string path)
    {
        return path is "/health/live" or "/health/ready"
            || path.StartsWith("/api/v1/system/", StringComparison.Ordinal)
            || path.StartsWith("/api/v1/auth/", StringComparison.Ordinal);
    }

    private sealed record SystemAuthEndpointDeclaration(string Path, string Method, bool IsAuthenticationIntentExplicit);
}
