using System.Text.Json;
using System.Text.RegularExpressions;
using WeCms.Api.Extensions;
using WeCms.Modules.System.Auth;
using WeCms.Modules.System.Permissions;

namespace WeCms.Tests.Unit.OpenApi;

public sealed class OpenApiExportTests
{
    [Fact]
    public async Task ExportOpenApiAsync_WritesExpectedContract()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"wecms-openapi-{Guid.NewGuid():N}.json");
        try
        {
            var handled = await OpenApiExtensions.ExportOpenApiAsync(["--export-openapi", outputPath]);

            Assert.True(handled);
            using var document = JsonDocument.Parse(await File.ReadAllTextAsync(outputPath));
            var root = document.RootElement;
            var paths = root.GetProperty("paths");
            var schemas = root.GetProperty("components").GetProperty("schemas");

            Assert.True(paths.TryGetProperty("/api/v1/auth/login", out var loginPath));
            Assert.True(loginPath.GetProperty("post").TryGetProperty("requestBody", out _));
            Assert.True(paths.TryGetProperty("/api/v1/auth/refresh", out var refreshPath));
            Assert.True(refreshPath.GetProperty("post").TryGetProperty("requestBody", out _));
            Assert.True(paths.TryGetProperty("/api/v1/auth/logout", out var logoutPath));
            Assert.True(logoutPath.GetProperty("post").TryGetProperty("requestBody", out _));
            Assert.True(logoutPath.GetProperty("post").TryGetProperty("security", out var logoutSecurity));
            Assert.NotEqual(0, logoutSecurity.GetArrayLength());
            var logoutUsesBearerAuth = false;
            foreach (var securityEntry in logoutSecurity.EnumerateArray())
            {
                if (securityEntry.TryGetProperty("bearerAuth", out _))
                {
                    logoutUsesBearerAuth = true;
                }
            }

            Assert.True(logoutUsesBearerAuth);
            Assert.True(schemas.TryGetProperty("ApiResult", out _));
            Assert.True(schemas.TryGetProperty("LoginResponse", out _));

            Assert.True(paths.TryGetProperty("/health/live", out _));
            Assert.True(paths.TryGetProperty("/health/ready", out _));
            Assert.True(paths.TryGetProperty("/api/v1/system/db-check", out _));

            var securePing = paths.GetProperty("/api/v1/system/secure-ping").GetProperty("get");
            Assert.Equal("sys:system:secure-ping", securePing.GetProperty("x-wecms-permission").GetString());
            Assert.True(securePing.TryGetProperty("security", out _));
            AssertAllRefsResolve(root);
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    [Fact]
    public async Task ExportOpenApiAsync_CoverageMatchesRegisteredEndpoints()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"wecms-openapi-coverage-{Guid.NewGuid():N}.json");
        try
        {
            var handled = await OpenApiExtensions.ExportOpenApiAsync(["--export-openapi", outputPath]);

            Assert.True(handled);
            using var document = JsonDocument.Parse(await File.ReadAllTextAsync(outputPath));
            var root = document.RootElement;
            var contractOperationsList = CollectRegisteredEndpointMetadata().ToList();
            var openApiOperations = CollectOpenApiOperations(root.GetProperty("paths"));

            Assert.Equal(contractOperationsList.Count, openApiOperations.Count);
            AssertOpenApiOperationsMatch(contractOperationsList, openApiOperations);
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    [Fact]
    public async Task ExportOpenApiAsync_SourceEndpointsAreCoveredByContract()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"wecms-openapi-source-coverage-{Guid.NewGuid():N}.json");
        try
        {
            var handled = await OpenApiExtensions.ExportOpenApiAsync(["--export-openapi", outputPath]);

            Assert.True(handled);
            using var document = JsonDocument.Parse(await File.ReadAllTextAsync(outputPath));
            var paths = document.RootElement.GetProperty("paths");
            var contractOperations = CollectOpenApiOperations(paths);

            var sourceOperations = CollectSourceMappedEndpoints();
            Assert.All(sourceOperations, operation =>
            {
                Assert.True(
                    contractOperations.Contains(operation),
                    $"OpenAPI export missing {operation.Method.ToUpperInvariant()} {operation.Path}");
            });

            Assert.All(contractOperations, operation =>
            {
                Assert.True(
                    sourceOperations.Contains(operation),
                    $"OpenAPI export contains endpoint not mapped in source {operation.Method.ToUpperInvariant()} {operation.Path}");
            });
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    [Fact]
    public async Task ExportOpenApiAsync_RequestBodyRefsMatchOpenApiMetadata()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"wecms-openapi-requestbody-{Guid.NewGuid():N}.json");
        try
        {
            var handled = await OpenApiExtensions.ExportOpenApiAsync(["--export-openapi", outputPath]);

            Assert.True(handled);
            using var document = JsonDocument.Parse(await File.ReadAllTextAsync(outputPath));
            var paths = document.RootElement.GetProperty("paths");
            var schemas = document.RootElement.GetProperty("components").GetProperty("schemas");

            foreach (var registered in CollectRegisteredEndpointMetadata().Where(operation => operation.Method is "post" or "put"))
            {
                var operation = paths.GetProperty(registered.Path).GetProperty(registered.Method);
                var requestBody = operation.GetProperty("requestBody");
                var schemaRef = requestBody
                    .GetProperty("content")
                    .GetProperty("application/json")
                    .GetProperty("schema")
                    .GetProperty("$ref")
                    .GetString();

                Assert.NotNull(registered.RequestBody);
                Assert.Equal($"#/components/schemas/{registered.RequestBody}", schemaRef);
                Assert.True(schemas.TryGetProperty(registered.RequestBody, out _));
            }
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    [Fact]
    public async Task ExportOpenApiAsync_MapsEndpointAuthorizationAndPermissionMetadataCorrectly()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"wecms-openapi-auth-meta-{Guid.NewGuid():N}.json");
        try
        {
            var handled = await OpenApiExtensions.ExportOpenApiAsync(["--export-openapi", outputPath]);

            Assert.True(handled);
            using var document = JsonDocument.Parse(await File.ReadAllTextAsync(outputPath));
            var metadata = CollectOpenApiEndpointMetadata(document.RootElement.GetProperty("paths"));

            Assert.True(metadata.TryGetValue(("/api/v1/auth/login", "post"), out var login));
            Assert.False(login.RequiresAuthorization);
            Assert.Null(login.Permission);

            Assert.True(metadata.TryGetValue(("/api/v1/auth/refresh", "post"), out var refresh));
            Assert.False(refresh.RequiresAuthorization);
            Assert.Null(refresh.Permission);

            Assert.True(metadata.TryGetValue(("/api/v1/auth/logout", "post"), out var logout));
            Assert.True(logout.RequiresAuthorization);
            Assert.Null(logout.Permission);

            Assert.True(metadata.TryGetValue(("/api/v1/auth/me", "get"), out var me));
            Assert.True(me.RequiresAuthorization);
            Assert.Null(me.Permission);

            Assert.True(metadata.TryGetValue(("/api/v1/system/secure-ping", "get"), out var securePing));
            Assert.True(securePing.RequiresAuthorization);
            Assert.Equal("sys:system:secure-ping", securePing.Permission);
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    [Fact]
    public async Task ExportOpenApiAsync_SucceedsEvenWithInvalidDatabaseAndSecretEnvironment()
    {
        const string dbConnectionEnv = "ConnectionStrings__Default";
        const string secretEnv = "Auth__AccessTokenSecret";

        var originalDbConnection = Environment.GetEnvironmentVariable(dbConnectionEnv);
        var originalSecret = Environment.GetEnvironmentVariable(secretEnv);

        Environment.SetEnvironmentVariable(dbConnectionEnv, "server=invalid.host;database=invalid;user=invalid;password=invalid");
        Environment.SetEnvironmentVariable(secretEnv, "invalid-openapi-export-secret");

        var outputPath = Path.Combine(Path.GetTempPath(), $"wecms-openapi-env-safe-{Guid.NewGuid():N}.json");
        try
        {
            var handled = await OpenApiExtensions.ExportOpenApiAsync(["--export-openapi", outputPath]);

            Assert.True(handled);
            using var document = JsonDocument.Parse(await File.ReadAllTextAsync(outputPath));
            var root = document.RootElement;
            Assert.Equal("3.1.0", root.GetProperty("openapi").GetString());
            Assert.True(root.GetProperty("paths").GetProperty("/api/v1/system/db-check").EnumerateObject().Any());
        }
        finally
        {
            Environment.SetEnvironmentVariable(dbConnectionEnv, originalDbConnection);
            Environment.SetEnvironmentVariable(secretEnv, originalSecret);

            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    [Fact]
    public void SystemAndAuthEndpointMetadata_IsCoveredForAuthorizationAndPermission()
    {
        var registered = CollectRegisteredEndpointMetadata();
        var systemAndAuthEndpoints = registered
            .Where(endpoint => IsSystemOrAuthEndpoint(endpoint.Path))
            .ToDictionary(
                endpoint => $"{endpoint.Path}|{endpoint.Method}",
                endpoint => endpoint);

        var expected = new Dictionary<(string Path, string Method), (bool RequiresAuthorization, string? Permission)>
        {
            { ("/health/live", "get"), (false, null) },
            { ("/health/ready", "get"), (false, null) },
            { ("/api/v1/system/ping", "get"), (false, null) },
            { ("/api/v1/system/version", "get"), (false, null) },
            { ("/api/v1/system/db-check", "get"), (false, null) },
            { ("/api/v1/system/secure-ping", "get"), (true, "sys:system:secure-ping") },
            { ("/api/v1/auth/login", "post"), (false, null) },
            { ("/api/v1/auth/refresh", "post"), (false, null) },
            { ("/api/v1/auth/logout", "post"), (true, null) },
            { ("/api/v1/auth/me", "get"), (true, null) }
        };

        foreach (var expectedEndpoint in expected)
        {
            Assert.True(
                systemAndAuthEndpoints.TryGetValue(BuildEndpointKey(expectedEndpoint.Key.Path, expectedEndpoint.Key.Method), out var actual),
                $"System endpoint {expectedEndpoint.Key.Method.ToUpperInvariant()} {expectedEndpoint.Key.Path} is missing.");
            Assert.Equal(
                expectedEndpoint.Value.RequiresAuthorization,
                actual.RequiresAuthorization);
            Assert.Equal(
                expectedEndpoint.Value.Permission,
                actual.Permission);
        }

        var unexpected = systemAndAuthEndpoints
            .Where(endpoint => !expected.ContainsKey(ParseEndpointKey(endpoint.Key)))
            .Select(endpoint =>
            {
                var endpointKey = ParseEndpointKey(endpoint.Key);
                return $"{endpointKey.Method.ToUpperInvariant()} {endpointKey.Path}";
            })
            .ToArray();

        Assert.True(unexpected.Length == 0, $"Unexpected system endpoint metadata was found: {string.Join(", ", unexpected)}");
    }

    private static void AssertAllRefsResolve(JsonElement root)
    {
        var schemas = root.GetProperty("components").GetProperty("schemas");
        foreach (var element in Walk(root))
        {
            if (element.ValueKind != JsonValueKind.Object
                || !element.TryGetProperty("$ref", out var refElement))
            {
                continue;
            }

            var reference = refElement.GetString();
            Assert.NotNull(reference);
            const string prefix = "#/components/schemas/";
            Assert.StartsWith(prefix, reference, StringComparison.Ordinal);
            Assert.True(schemas.TryGetProperty(reference[prefix.Length..], out _), $"Dangling $ref: {reference}");
        }
    }

    private static IEnumerable<JsonElement> Walk(JsonElement element)
    {
        yield return element;

        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                foreach (var child in Walk(property.Value))
                {
                    yield return child;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                foreach (var child in Walk(item))
                {
                    yield return child;
                }
            }
        }
    }

    private static HashSet<(string Path, string Method)> CollectSourceMappedEndpoints()
    {
        var mappedEndpoints = new HashSet<(string Path, string Method)>();
        mappedEndpoints.UnionWith(CollectSourceMappedEndpointsFromFile(
            Path.Combine(SourceRoot, "WeCms.Modules.System", "Auth", "AuthEndpoints.cs")));
        mappedEndpoints.UnionWith(CollectSourceMappedEndpointsFromFile(
            Path.Combine(SourceRoot, "WeCms.Modules.System", "System", "SystemEndpointExtensions.cs")));
        mappedEndpoints.UnionWith(CollectSourceMappedEndpointsFromFile(
            Path.Combine(SourceRoot, "WeCms.Modules.System", "Permissions", "PermissionEndpointExtensions.cs")));

        return mappedEndpoints;
    }

    private static string SourceRoot
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "backend", "WeCms.slnx")))
                {
                    return Path.Combine(directory.FullName, "backend", "src");
                }

                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException("Could not locate repository root.");
        }
    }

    private static HashSet<(string Path, string Method)> CollectSourceMappedEndpointsFromFile(string filePath)
    {
        var source = File.ReadAllText(filePath);
        var result = new HashSet<(string Path, string Method)>();

        var groupPrefix = Regex.Match(source, @"\b(?<groupName>\w+)\s*=\s*endpoints\.MapGroup\(""(?<prefix>[^""]+)""\)");
        var routePrefix = groupPrefix.Success
            ? groupPrefix.Groups["prefix"].Value
            : string.Empty;
        var groupName = groupPrefix.Success ? groupPrefix.Groups["groupName"].Value : null;

        const string endpointPattern =
            @"(?<receiver>\w+)\.Map(?<method>Get|Post|Put|Patch|Delete)\(\s*""(?<path>/[^""\\]*)""\s*,";

        foreach (Match match in Regex.Matches(source, endpointPattern))
        {
            var method = match.Groups["method"].Value.ToLowerInvariant();
            var rawPath = match.Groups["path"].Value;
            var receiver = match.Groups["receiver"].Value;
            var path = receiver == groupName && !string.IsNullOrWhiteSpace(routePrefix)
                ? $"{routePrefix}{rawPath}"
                : rawPath;

            result.Add((path, method));
        }

        return result;
    }

    private static HashSet<(string Path, string Method)> CollectOpenApiOperations(JsonElement paths)
    {
        var operations = new HashSet<(string Path, string Method)>();

        foreach (var path in paths.EnumerateObject())
        {
            foreach (var method in path.Value.EnumerateObject())
            {
                operations.Add((path.Name, method.Name.ToLowerInvariant()));
            }
        }

        return operations;
    }

    private static Dictionary<(string Path, string Method), OpenApiMetadata> CollectOpenApiEndpointMetadata(JsonElement paths)
    {
        var metadata = new Dictionary<(string Path, string Method), OpenApiMetadata>();

        foreach (var path in paths.EnumerateObject())
        {
            foreach (var method in path.Value.EnumerateObject())
            {
                var operation = method.Value;
                var hasSecurity = operation.TryGetProperty("security", out var security)
                    && security.GetArrayLength() > 0;
                var permission = operation.TryGetProperty("x-wecms-permission", out var permissionElement)
                    ? permissionElement.GetString()
                    : null;

                metadata[(path.Name, method.Name.ToLowerInvariant())] = new OpenApiMetadata(
                    path.Name,
                    method.Name.ToLowerInvariant(),
                    hasSecurity,
                    permission);
            }
        }

        return metadata;
    }

    private static bool IsSystemOrAuthEndpoint(string path)
    {
        return path is "/health/live" or "/health/ready"
            || path.StartsWith("/api/v1/system/", StringComparison.Ordinal)
            || path.StartsWith("/api/v1/auth/", StringComparison.Ordinal);
    }

    private static string BuildEndpointKey(string path, string method)
    {
        return $"{path}|{method}";
    }

    private static (string Path, string Method) ParseEndpointKey(string key)
    {
        var separator = key.IndexOf('|');
        return separator < 0
            ? (key, string.Empty)
            : (key[..separator], key[(separator + 1)..]);
    }

    private static HashSet<RegisteredEndpoint> CollectRegisteredEndpointMetadata()
    {
        return new HashSet<RegisteredEndpoint>(RegisteredEndpointMetadata);
    }

    private static readonly HashSet<RegisteredEndpoint> RegisteredEndpointMetadata =
    [
        new RegisteredEndpoint("/health/live", "get", null, false, null),
        new RegisteredEndpoint("/health/ready", "get", null, false, null),
        new RegisteredEndpoint("/api/v1/system/ping", "get", null, false, null),
        new RegisteredEndpoint("/api/v1/system/version", "get", null, false, null),
        new RegisteredEndpoint("/api/v1/system/db-check", "get", null, false, null),
        new RegisteredEndpoint("/api/v1/system/secure-ping", "get", SystemPermissions.SecurePing, true, null),
        new RegisteredEndpoint("/api/v1/auth/login", "post", null, false, nameof(LoginRequest)),
        new RegisteredEndpoint("/api/v1/auth/refresh", "post", null, false, nameof(RefreshTokenRequest)),
        new RegisteredEndpoint("/api/v1/auth/logout", "post", null, true, nameof(LogoutRequest)),
        new RegisteredEndpoint("/api/v1/auth/me", "get", null, true, null)
    ];

    private sealed record RegisteredEndpoint(string Path, string Method, string? Permission, bool RequiresAuthorization, string? RequestBody);
    private sealed record OpenApiMetadata(string Path, string Method, bool RequiresAuthorization, string? Permission);
    
    private static void AssertOpenApiOperationsMatch(
        IEnumerable<RegisteredEndpoint> registered,
        HashSet<(string Path, string Method)> openApiOperations)
    {
        var registeredSet = registered.Select(endpoint => (endpoint.Path, endpoint.Method)).ToHashSet();

        Assert.All(registeredSet, operation =>
        {
            Assert.True(openApiOperations.Contains(operation), $"OpenAPI missing {operation.Method.ToUpperInvariant()} {operation.Path}");
        });

        Assert.All(openApiOperations, operation =>
        {
            Assert.True(
                registeredSet.Contains(operation),
                $"OpenAPI contains unregistered endpoint {operation.Method.ToUpperInvariant()} {operation.Path}");
        });
    }

    [Fact]
    public async Task ExportOpenApiAsync_ReturnsFalseWhenArgumentIsMissing()
    {
        var handled = await OpenApiExtensions.ExportOpenApiAsync([]);

        Assert.False(handled);
    }
}
