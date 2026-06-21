using System.Text.Json;
using WeCms.Api.Extensions;
using WeCms.Modules.Identity.Services;
using WeCms.Modules.Organization.Departments;
using WeCms.Modules.Configuration.Dicts;
using WeCms.Modules.FileCenter.Files;
using WeCms.Modules.Audit.Logs;
using WeCms.Modules.Security.Events;
using WeCms.Modules.AccessControl.Menus;
using WeCms.Modules.Platform.Permissions;
using WeCms.Modules.Organization.Positions;
using WeCms.Modules.AccessControl.Roles;
using WeCms.Modules.Security;
using WeCms.Modules.Configuration.Settings;

namespace WeCms.Tests.Unit.OpenApi;

public sealed partial class OpenApiExportTests
{
    [Fact]
    public async Task ExportOpenApiAsync_WritesExpectedContract()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"wecms-openapi-{Guid.NewGuid():N}.json");
        try
        {
            var handled = await OpenApiExtensions.ExportOpenApiAsync(["--export-openapi", outputPath], TestContext.Current.CancellationToken);

            Assert.True(handled);
            using var document = JsonDocument.Parse(await File.ReadAllTextAsync(outputPath, TestContext.Current.CancellationToken));
            var root = document.RootElement;
            var paths = root.GetProperty("paths");
            var schemas = root.GetProperty("components").GetProperty("schemas");

            Assert.True(paths.TryGetProperty("/api/v1/auth/login", out var loginPath));
            Assert.True(loginPath.GetProperty("post").TryGetProperty("requestBody", out _));
            Assert.True(paths.TryGetProperty("/api/v1/auth/refresh", out var refreshPath));
            Assert.False(refreshPath.GetProperty("post").TryGetProperty("requestBody", out _));
            Assert.True(paths.TryGetProperty("/api/v1/auth/logout", out var logoutPath));
            Assert.False(logoutPath.GetProperty("post").TryGetProperty("requestBody", out _));
            Assert.False(logoutPath.GetProperty("post").TryGetProperty("security", out _));
            Assert.True(schemas.TryGetProperty("ApiResult", out _));
            Assert.True(schemas.TryGetProperty("LoginResponse", out var loginResponse));
            Assert.False(loginResponse.GetProperty("properties").TryGetProperty("refreshToken", out _));
            AssertRoleSchemasExposeLockedFlag(schemas);

            Assert.True(paths.TryGetProperty("/health/live", out _));
            Assert.True(paths.TryGetProperty("/health/ready", out _));
            Assert.True(paths.TryGetProperty("/health/dependencies", out var dependenciesPath));
            var healthDependencies = dependenciesPath.GetProperty("get");
            Assert.True(healthDependencies.TryGetProperty("security", out _));
            Assert.Equal("sys:system:secure-ping", healthDependencies.GetProperty("x-wecms-permission").GetString());
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

    private static void AssertRoleSchemasExposeLockedFlag(JsonElement schemas)
    {
        var summary = schemas.GetProperty(nameof(RoleSummaryDto));
        var summaryRequired = summary.GetProperty("required").EnumerateArray().Select(item => item.GetString()).ToHashSet();
        Assert.Contains("isLocked", summaryRequired);
        Assert.Equal("boolean", summary.GetProperty("properties").GetProperty("isLocked").GetProperty("type").GetString());

        var detail = schemas.GetProperty(nameof(RoleDetailDto));
        var detailRequired = detail.GetProperty("required").EnumerateArray().Select(item => item.GetString()).ToHashSet();
        Assert.Contains("isLocked", detailRequired);
        Assert.Equal("boolean", detail.GetProperty("properties").GetProperty("isLocked").GetProperty("type").GetString());
    }

    [Fact]
    public async Task ExportOpenApiAsync_CoverageMatchesRegisteredEndpoints()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"wecms-openapi-coverage-{Guid.NewGuid():N}.json");
        try
        {
            var handled = await OpenApiExtensions.ExportOpenApiAsync(["--export-openapi", outputPath], TestContext.Current.CancellationToken);

            Assert.True(handled);
            using var document = JsonDocument.Parse(await File.ReadAllTextAsync(outputPath, TestContext.Current.CancellationToken));
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
            var handled = await OpenApiExtensions.ExportOpenApiAsync(["--export-openapi", outputPath], TestContext.Current.CancellationToken);

            Assert.True(handled);
            using var document = JsonDocument.Parse(await File.ReadAllTextAsync(outputPath, TestContext.Current.CancellationToken));
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
            var handled = await OpenApiExtensions.ExportOpenApiAsync(["--export-openapi", outputPath], TestContext.Current.CancellationToken);

            Assert.True(handled);
            using var document = JsonDocument.Parse(await File.ReadAllTextAsync(outputPath, TestContext.Current.CancellationToken));
            var paths = document.RootElement.GetProperty("paths");
            var schemas = document.RootElement.GetProperty("components").GetProperty("schemas");

            foreach (var registered in CollectRegisteredEndpointMetadata().Where(operation => operation.Method is "post" or "put" && operation.RequestBody is not null))
            {
                var operation = paths.GetProperty(registered.Path).GetProperty(registered.Method);
                var requestBody = operation.GetProperty("requestBody");
                var contentType = (registered.Path == "/api/v1/system/files" || registered.Path == "/api/v1/account/avatar") && registered.Method == "post"
                    ? "multipart/form-data"
                    : "application/json";
                var schemaRef = requestBody
                    .GetProperty("content")
                    .GetProperty(contentType)
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
    public async Task ExportOpenApiAsync_FileUploadSchemaIncludesPolicy()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"wecms-openapi-file-policy-{Guid.NewGuid():N}.json");
        try
        {
            var handled = await OpenApiExtensions.ExportOpenApiAsync(["--export-openapi", outputPath], TestContext.Current.CancellationToken);

            Assert.True(handled);
            using var document = JsonDocument.Parse(await File.ReadAllTextAsync(outputPath, TestContext.Current.CancellationToken));
            var properties = document.RootElement
                .GetProperty("components")
                .GetProperty("schemas")
                .GetProperty(nameof(CreateFileRequest))
                .GetProperty("properties");

            Assert.True(properties.TryGetProperty("policy", out var policy));
            Assert.Equal("string", policy.GetProperty("type").GetString());
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
    public async Task ExportOpenApiAsync_BodylessCommandOperationsDoNotDeclareRequestBody()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"wecms-openapi-bodyless-{Guid.NewGuid():N}.json");
        try
        {
            var handled = await OpenApiExtensions.ExportOpenApiAsync(["--export-openapi", outputPath], TestContext.Current.CancellationToken);

            Assert.True(handled);
            using var document = JsonDocument.Parse(await File.ReadAllTextAsync(outputPath, TestContext.Current.CancellationToken));
            var paths = document.RootElement.GetProperty("paths");
            var bodylessOperations = new (string Path, string Method)[]
            {
                ("/api/v1/system/users/{id:long}/enable", "post"),
                ("/api/v1/system/users/{id:long}/disable", "post"),
                ("/api/v1/system/roles/{id:long}/enable", "post"),
                ("/api/v1/system/roles/{id:long}/disable", "post"),
                ("/api/v1/system/menus/{id:long}/enable", "post"),
                ("/api/v1/system/menus/{id:long}/disable", "post"),
                ("/api/v1/system/permissions/{id:long}/enable", "post"),
                ("/api/v1/system/permissions/{id:long}/disable", "post"),
                ("/api/v1/system/depts/{id:long}/enable", "post"),
                ("/api/v1/system/depts/{id:long}/disable", "post"),
                ("/api/v1/system/positions/{id:long}/enable", "post"),
                ("/api/v1/system/positions/{id:long}/disable", "post")
            };

            foreach (var operationKey in bodylessOperations)
            {
                var operation = paths.GetProperty(operationKey.Path).GetProperty(operationKey.Method);
                Assert.False(
                    operation.TryGetProperty("requestBody", out _),
                    $"{operationKey.Method.ToUpperInvariant()} {operationKey.Path} should not declare requestBody.");
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
    public async Task ExportOpenApiAsync_ListEndpointsDeclareQueryParameters()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"wecms-openapi-query-{Guid.NewGuid():N}.json");
        try
        {
            var handled = await OpenApiExtensions.ExportOpenApiAsync(["--export-openapi", outputPath], TestContext.Current.CancellationToken);

            Assert.True(handled);
            using var document = JsonDocument.Parse(await File.ReadAllTextAsync(outputPath, TestContext.Current.CancellationToken));
            var paths = document.RootElement.GetProperty("paths");
            var expected = new Dictionary<string, string[]>
            {
                ["/api/v1/system/users"] = ["page", "pageSize", "keyword", "status"],
                ["/api/v1/system/roles"] = ["page", "pageSize", "keyword", "status"],
                ["/api/v1/system/positions"] = ["page", "pageSize", "keyword", "status"],
                ["/api/v1/system/dict-types"] = ["page", "pageSize", "keyword", "status"],
                ["/api/v1/system/settings"] = ["page", "pageSize", "keyword", "groupCode"],
                ["/api/v1/system/login-logs"] = ["page", "pageSize", "username", "ip", "result", "from", "to"],
                ["/api/v1/system/audit-logs"] = ["page", "pageSize", "user", "module", "resource", "action", "result", "from", "to"],
                ["/api/v1/system/security/bans"] = ["page", "pageSize", "banType", "target", "severity", "source", "activeOnly"],
                ["/api/v1/system/security-events"] = ["page", "pageSize", "eventType", "severity", "user", "ip", "from", "to"],
                ["/api/v1/system/files"] = ["page", "pageSize", "keyword", "mimeType", "status"]
            };

            foreach (var endpoint in expected)
            {
                var operation = paths.GetProperty(endpoint.Key).GetProperty("get");
                Assert.True(operation.TryGetProperty("parameters", out var parameters), $"{endpoint.Key} missing parameters.");
                var names = parameters.EnumerateArray()
                    .Select(parameter => parameter.GetProperty("name").GetString())
                    .ToArray();
                Assert.Equal(endpoint.Value, names);
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
            var handled = await OpenApiExtensions.ExportOpenApiAsync(["--export-openapi", outputPath], TestContext.Current.CancellationToken);

            Assert.True(handled);
            using var document = JsonDocument.Parse(await File.ReadAllTextAsync(outputPath, TestContext.Current.CancellationToken));
            var metadata = CollectOpenApiEndpointMetadata(document.RootElement.GetProperty("paths"));

            Assert.True(metadata.TryGetValue(("/api/v1/auth/login", "post"), out var login));
            Assert.False(login.RequiresAuthorization);
            Assert.Null(login.Permission);

            Assert.True(metadata.TryGetValue(("/api/v1/auth/refresh", "post"), out var refresh));
            Assert.False(refresh.RequiresAuthorization);
            Assert.Null(refresh.Permission);

            Assert.True(metadata.TryGetValue(("/api/v1/auth/logout", "post"), out var logout));
            Assert.False(logout.RequiresAuthorization);
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
            var handled = await OpenApiExtensions.ExportOpenApiAsync(["--export-openapi", outputPath], TestContext.Current.CancellationToken);

            Assert.True(handled);
            using var document = JsonDocument.Parse(await File.ReadAllTextAsync(outputPath, TestContext.Current.CancellationToken));
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

    private static HashSet<RegisteredEndpoint> CollectRegisteredEndpointMetadata()
    {
        return new HashSet<RegisteredEndpoint>(RegisteredEndpointMetadata);
    }

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
        var handled = await OpenApiExtensions.ExportOpenApiAsync([], TestContext.Current.CancellationToken);

        Assert.False(handled);
    }
}
