using System.Text.Json;
using System.Text.RegularExpressions;
using WeCms.Api.Extensions;
using WeCms.Modules.System.Auth;
using WeCms.Modules.System.Departments;
using WeCms.Modules.System.Dicts;
using WeCms.Modules.System.Files;
using WeCms.Modules.System.Logs;
using WeCms.Modules.System.Menus;
using WeCms.Modules.System.Permissions;
using WeCms.Modules.System.Posts;
using WeCms.Modules.System.Roles;
using WeCms.Modules.System.Settings;
using WeCms.Modules.System.Users;

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

            foreach (var registered in CollectRegisteredEndpointMetadata().Where(operation => operation.Method is "post" or "put" && operation.RequestBody is not null))
            {
                var operation = paths.GetProperty(registered.Path).GetProperty(registered.Method);
                var requestBody = operation.GetProperty("requestBody");
                var contentType = registered.Path == "/api/v1/system/files" && registered.Method == "post"
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
    public async Task ExportOpenApiAsync_BodylessCommandOperationsDoNotDeclareRequestBody()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"wecms-openapi-bodyless-{Guid.NewGuid():N}.json");
        try
        {
            var handled = await OpenApiExtensions.ExportOpenApiAsync(["--export-openapi", outputPath]);

            Assert.True(handled);
            using var document = JsonDocument.Parse(await File.ReadAllTextAsync(outputPath));
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
                ("/api/v1/system/posts/{id:long}/enable", "post"),
                ("/api/v1/system/posts/{id:long}/disable", "post")
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
            var handled = await OpenApiExtensions.ExportOpenApiAsync(["--export-openapi", outputPath]);

            Assert.True(handled);
            using var document = JsonDocument.Parse(await File.ReadAllTextAsync(outputPath));
            var paths = document.RootElement.GetProperty("paths");
            var expected = new Dictionary<string, string[]>
            {
                ["/api/v1/system/users"] = ["page", "pageSize", "keyword", "status"],
                ["/api/v1/system/roles"] = ["page", "pageSize", "keyword", "status"],
                ["/api/v1/system/posts"] = ["page", "pageSize", "keyword", "status"],
                ["/api/v1/system/dict-types"] = ["page", "pageSize", "keyword", "status"],
                ["/api/v1/system/settings"] = ["page", "pageSize", "keyword", "groupCode"],
                ["/api/v1/system/login-logs"] = ["page", "pageSize", "username", "ip", "result", "from", "to"],
                ["/api/v1/system/audit-logs"] = ["page", "pageSize", "user", "module", "resource", "action", "result", "from", "to"],
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
            { ("/api/v1/auth/logout", "post"), (false, null) },
            { ("/api/v1/auth/me", "get"), (true, null) },
            { ("/api/v1/system/users", "get"), (true, UserPermissions.List) },
            { ("/api/v1/system/users/{id:long}", "get"), (true, UserPermissions.Detail) },
            { ("/api/v1/system/users", "post"), (true, UserPermissions.Create) },
            { ("/api/v1/system/users/{id:long}", "put"), (true, UserPermissions.Update) },
            { ("/api/v1/system/users/{id:long}", "delete"), (true, UserPermissions.Delete) },
            { ("/api/v1/system/users/{id:long}/enable", "post"), (true, UserPermissions.Enable) },
            { ("/api/v1/system/users/{id:long}/disable", "post"), (true, UserPermissions.Disable) },
            { ("/api/v1/system/users/{id:long}/reset-password", "post"), (true, UserPermissions.ResetPassword) },
            { ("/api/v1/system/users/{id:long}/roles", "put"), (true, UserPermissions.AssignRole) },
            { ("/api/v1/system/users/{id:long}/posts", "put"), (true, UserPermissions.AssignPost) },
            { ("/api/v1/system/roles", "get"), (true, RolePermissions.List) },
            { ("/api/v1/system/roles/{id:long}", "get"), (true, RolePermissions.Detail) },
            { ("/api/v1/system/roles", "post"), (true, RolePermissions.Create) },
            { ("/api/v1/system/roles/{id:long}", "put"), (true, RolePermissions.Update) },
            { ("/api/v1/system/roles/{id:long}", "delete"), (true, RolePermissions.Delete) },
            { ("/api/v1/system/roles/{id:long}/enable", "post"), (true, RolePermissions.Enable) },
            { ("/api/v1/system/roles/{id:long}/disable", "post"), (true, RolePermissions.Disable) },
            { ("/api/v1/system/roles/{id:long}/permissions", "put"), (true, RolePermissions.AssignPermission) },
            { ("/api/v1/system/roles/{id:long}/menus", "put"), (true, RolePermissions.AssignMenu) },
            { ("/api/v1/system/menus", "get"), (true, MenuPermissions.List) },
            { ("/api/v1/system/menus/tree", "get"), (true, MenuPermissions.Tree) },
            { ("/api/v1/system/menus/{id:long}", "get"), (true, MenuPermissions.Detail) },
            { ("/api/v1/system/menus", "post"), (true, MenuPermissions.Create) },
            { ("/api/v1/system/menus/{id:long}", "put"), (true, MenuPermissions.Update) },
            { ("/api/v1/system/menus/{id:long}", "delete"), (true, MenuPermissions.Delete) },
            { ("/api/v1/system/menus/{id:long}/enable", "post"), (true, MenuPermissions.Enable) },
            { ("/api/v1/system/menus/{id:long}/disable", "post"), (true, MenuPermissions.Disable) },
            { ("/api/v1/system/permissions", "get"), (true, PermissionManagementPermissions.List) },
            { ("/api/v1/system/permissions/tree", "get"), (true, PermissionManagementPermissions.Tree) },
            { ("/api/v1/system/permissions/{id:long}", "get"), (true, PermissionManagementPermissions.Detail) },
            { ("/api/v1/system/permissions", "post"), (true, PermissionManagementPermissions.Create) },
            { ("/api/v1/system/permissions/{id:long}", "put"), (true, PermissionManagementPermissions.Update) },
            { ("/api/v1/system/permissions/{id:long}", "delete"), (true, PermissionManagementPermissions.Delete) },
            { ("/api/v1/system/permissions/{id:long}/enable", "post"), (true, PermissionManagementPermissions.Enable) },
            { ("/api/v1/system/permissions/{id:long}/disable", "post"), (true, PermissionManagementPermissions.Disable) },
            { ("/api/v1/system/depts", "get"), (true, DepartmentPermissions.List) },
            { ("/api/v1/system/depts/tree", "get"), (true, DepartmentPermissions.Tree) },
            { ("/api/v1/system/depts/{id:long}", "get"), (true, DepartmentPermissions.Detail) },
            { ("/api/v1/system/depts", "post"), (true, DepartmentPermissions.Create) },
            { ("/api/v1/system/depts/{id:long}", "put"), (true, DepartmentPermissions.Update) },
            { ("/api/v1/system/depts/{id:long}", "delete"), (true, DepartmentPermissions.Delete) },
            { ("/api/v1/system/depts/{id:long}/enable", "post"), (true, DepartmentPermissions.Enable) },
            { ("/api/v1/system/depts/{id:long}/disable", "post"), (true, DepartmentPermissions.Disable) },
            { ("/api/v1/system/posts", "get"), (true, PostPermissions.List) },
            { ("/api/v1/system/posts/{id:long}", "get"), (true, PostPermissions.Detail) },
            { ("/api/v1/system/posts", "post"), (true, PostPermissions.Create) },
            { ("/api/v1/system/posts/{id:long}", "put"), (true, PostPermissions.Update) },
            { ("/api/v1/system/posts/{id:long}", "delete"), (true, PostPermissions.Delete) },
            { ("/api/v1/system/posts/{id:long}/enable", "post"), (true, PostPermissions.Enable) },
            { ("/api/v1/system/posts/{id:long}/disable", "post"), (true, PostPermissions.Disable) },
            { ("/api/v1/system/dict-types", "get"), (true, DictPermissions.TypeList) },
            { ("/api/v1/system/dict-types/{id:long}", "get"), (true, DictPermissions.TypeList) },
            { ("/api/v1/system/dict-types", "post"), (true, DictPermissions.TypeCreate) },
            { ("/api/v1/system/dict-types/{id:long}", "put"), (true, DictPermissions.TypeUpdate) },
            { ("/api/v1/system/dict-types/{id:long}", "delete"), (true, DictPermissions.TypeDelete) },
            { ("/api/v1/system/dict-types/{typeCode}/values", "get"), (true, DictPermissions.ValueList) },
            { ("/api/v1/system/dict-types/{typeCode}/values", "post"), (true, DictPermissions.ValueCreate) },
            { ("/api/v1/system/dict-values/{id:long}", "put"), (true, DictPermissions.ValueUpdate) },
            { ("/api/v1/system/dict-values/{id:long}", "delete"), (true, DictPermissions.ValueDelete) },
            { ("/api/v1/system/settings", "get"), (true, SettingPermissions.List) },
            { ("/api/v1/system/settings/{key}", "get"), (true, SettingPermissions.Detail) },
            { ("/api/v1/system/settings/{key}", "put"), (true, SettingPermissions.Update) },
            { ("/api/v1/system/login-logs", "get"), (true, LogPermissions.LoginLogList) },
            { ("/api/v1/system/login-logs/{id:long}", "get"), (true, LogPermissions.LoginLogDetail) },
            { ("/api/v1/system/audit-logs", "get"), (true, LogPermissions.AuditLogList) },
            { ("/api/v1/system/audit-logs/{id:long}", "get"), (true, LogPermissions.AuditLogDetail) },
            { ("/api/v1/system/security-events", "get"), (true, LogPermissions.SecurityEventList) },
            { ("/api/v1/system/security-events/{id:long}", "get"), (true, LogPermissions.SecurityEventDetail) },
            { ("/api/v1/system/files", "get"), (true, FilePermissions.List) },
            { ("/api/v1/system/files/{id:long}", "get"), (true, FilePermissions.Detail) },
            { ("/api/v1/system/files", "post"), (true, FilePermissions.Upload) },
            { ("/api/v1/system/files/{id:long}/download", "get"), (true, FilePermissions.Download) },
            { ("/api/v1/system/files/{id:long}/preview", "get"), (true, FilePermissions.Download) },
            { ("/api/v1/system/files/{id:long}", "delete"), (true, FilePermissions.Delete) }
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

        foreach (var filePath in EnumerateEndpointSourceFiles())
        {
            mappedEndpoints.UnionWith(CollectSourceMappedEndpointsFromFile(filePath));
        }

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
            @"(?<receiver>\w+)\.Map(?<method>Get|Post|Put|Patch|Delete)\(\s*""(?<path>[^""\\]*)""\s*,";

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

    private static IEnumerable<string> EnumerateEndpointSourceFiles()
    {
        return Directory
            .EnumerateFiles(Path.Combine(SourceRoot, "WeCms.Modules.System"), "*.cs", SearchOption.AllDirectories)
            .Where(filePath =>
                filePath.EndsWith("Endpoints.cs", StringComparison.Ordinal)
                || filePath.EndsWith("EndpointExtensions.cs", StringComparison.Ordinal))
            .OrderBy(filePath => filePath, StringComparer.Ordinal);
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
        new RegisteredEndpoint("/api/v1/auth/refresh", "post", null, false, null),
        new RegisteredEndpoint("/api/v1/auth/logout", "post", null, false, null),
        new RegisteredEndpoint("/api/v1/auth/me", "get", null, true, null),
        new RegisteredEndpoint("/api/v1/system/users", "get", UserPermissions.List, true, null),
        new RegisteredEndpoint("/api/v1/system/users/{id:long}", "get", UserPermissions.Detail, true, null),
        new RegisteredEndpoint("/api/v1/system/users", "post", UserPermissions.Create, true, nameof(CreateUserRequest)),
        new RegisteredEndpoint("/api/v1/system/users/{id:long}", "put", UserPermissions.Update, true, nameof(UpdateUserRequest)),
        new RegisteredEndpoint("/api/v1/system/users/{id:long}", "delete", UserPermissions.Delete, true, null),
        new RegisteredEndpoint("/api/v1/system/users/{id:long}/enable", "post", UserPermissions.Enable, true, null),
        new RegisteredEndpoint("/api/v1/system/users/{id:long}/disable", "post", UserPermissions.Disable, true, null),
        new RegisteredEndpoint("/api/v1/system/users/{id:long}/reset-password", "post", UserPermissions.ResetPassword, true, nameof(ResetUserPasswordRequest)),
        new RegisteredEndpoint("/api/v1/system/users/{id:long}/roles", "put", UserPermissions.AssignRole, true, nameof(AssignUserRolesRequest)),
        new RegisteredEndpoint("/api/v1/system/users/{id:long}/posts", "put", UserPermissions.AssignPost, true, nameof(AssignUserPostsRequest)),
        new RegisteredEndpoint("/api/v1/system/roles", "get", RolePermissions.List, true, null),
        new RegisteredEndpoint("/api/v1/system/roles/{id:long}", "get", RolePermissions.Detail, true, null),
        new RegisteredEndpoint("/api/v1/system/roles", "post", RolePermissions.Create, true, nameof(CreateRoleRequest)),
        new RegisteredEndpoint("/api/v1/system/roles/{id:long}", "put", RolePermissions.Update, true, nameof(UpdateRoleRequest)),
        new RegisteredEndpoint("/api/v1/system/roles/{id:long}", "delete", RolePermissions.Delete, true, null),
        new RegisteredEndpoint("/api/v1/system/roles/{id:long}/enable", "post", RolePermissions.Enable, true, null),
        new RegisteredEndpoint("/api/v1/system/roles/{id:long}/disable", "post", RolePermissions.Disable, true, null),
        new RegisteredEndpoint("/api/v1/system/roles/{id:long}/permissions", "put", RolePermissions.AssignPermission, true, nameof(AssignRolePermissionsRequest)),
        new RegisteredEndpoint("/api/v1/system/roles/{id:long}/menus", "put", RolePermissions.AssignMenu, true, nameof(AssignRoleMenusRequest)),
        new RegisteredEndpoint("/api/v1/system/menus", "get", MenuPermissions.List, true, null),
        new RegisteredEndpoint("/api/v1/system/menus/tree", "get", MenuPermissions.Tree, true, null),
        new RegisteredEndpoint("/api/v1/system/menus/{id:long}", "get", MenuPermissions.Detail, true, null),
        new RegisteredEndpoint("/api/v1/system/menus", "post", MenuPermissions.Create, true, nameof(CreateMenuRequest)),
        new RegisteredEndpoint("/api/v1/system/menus/{id:long}", "put", MenuPermissions.Update, true, nameof(UpdateMenuRequest)),
        new RegisteredEndpoint("/api/v1/system/menus/{id:long}", "delete", MenuPermissions.Delete, true, null),
        new RegisteredEndpoint("/api/v1/system/menus/{id:long}/enable", "post", MenuPermissions.Enable, true, null),
        new RegisteredEndpoint("/api/v1/system/menus/{id:long}/disable", "post", MenuPermissions.Disable, true, null),
        new RegisteredEndpoint("/api/v1/system/permissions", "get", PermissionManagementPermissions.List, true, null),
        new RegisteredEndpoint("/api/v1/system/permissions/tree", "get", PermissionManagementPermissions.Tree, true, null),
        new RegisteredEndpoint("/api/v1/system/permissions/{id:long}", "get", PermissionManagementPermissions.Detail, true, null),
        new RegisteredEndpoint("/api/v1/system/permissions", "post", PermissionManagementPermissions.Create, true, nameof(CreatePermissionRequest)),
        new RegisteredEndpoint("/api/v1/system/permissions/{id:long}", "put", PermissionManagementPermissions.Update, true, nameof(UpdatePermissionRequest)),
        new RegisteredEndpoint("/api/v1/system/permissions/{id:long}", "delete", PermissionManagementPermissions.Delete, true, null),
        new RegisteredEndpoint("/api/v1/system/permissions/{id:long}/enable", "post", PermissionManagementPermissions.Enable, true, null),
        new RegisteredEndpoint("/api/v1/system/permissions/{id:long}/disable", "post", PermissionManagementPermissions.Disable, true, null),
        new RegisteredEndpoint("/api/v1/system/depts", "get", DepartmentPermissions.List, true, null),
        new RegisteredEndpoint("/api/v1/system/depts/tree", "get", DepartmentPermissions.Tree, true, null),
        new RegisteredEndpoint("/api/v1/system/depts/{id:long}", "get", DepartmentPermissions.Detail, true, null),
        new RegisteredEndpoint("/api/v1/system/depts", "post", DepartmentPermissions.Create, true, nameof(CreateDepartmentRequest)),
        new RegisteredEndpoint("/api/v1/system/depts/{id:long}", "put", DepartmentPermissions.Update, true, nameof(UpdateDepartmentRequest)),
        new RegisteredEndpoint("/api/v1/system/depts/{id:long}", "delete", DepartmentPermissions.Delete, true, null),
        new RegisteredEndpoint("/api/v1/system/depts/{id:long}/enable", "post", DepartmentPermissions.Enable, true, null),
        new RegisteredEndpoint("/api/v1/system/depts/{id:long}/disable", "post", DepartmentPermissions.Disable, true, null),
        new RegisteredEndpoint("/api/v1/system/posts", "get", PostPermissions.List, true, null),
        new RegisteredEndpoint("/api/v1/system/posts/{id:long}", "get", PostPermissions.Detail, true, null),
        new RegisteredEndpoint("/api/v1/system/posts", "post", PostPermissions.Create, true, nameof(CreatePostRequest)),
        new RegisteredEndpoint("/api/v1/system/posts/{id:long}", "put", PostPermissions.Update, true, nameof(UpdatePostRequest)),
        new RegisteredEndpoint("/api/v1/system/posts/{id:long}", "delete", PostPermissions.Delete, true, null),
        new RegisteredEndpoint("/api/v1/system/posts/{id:long}/enable", "post", PostPermissions.Enable, true, null),
        new RegisteredEndpoint("/api/v1/system/posts/{id:long}/disable", "post", PostPermissions.Disable, true, null),
        new RegisteredEndpoint("/api/v1/system/dict-types", "get", DictPermissions.TypeList, true, null),
        new RegisteredEndpoint("/api/v1/system/dict-types/{id:long}", "get", DictPermissions.TypeList, true, null),
        new RegisteredEndpoint("/api/v1/system/dict-types", "post", DictPermissions.TypeCreate, true, nameof(CreateDictTypeRequest)),
        new RegisteredEndpoint("/api/v1/system/dict-types/{id:long}", "put", DictPermissions.TypeUpdate, true, nameof(UpdateDictTypeRequest)),
        new RegisteredEndpoint("/api/v1/system/dict-types/{id:long}", "delete", DictPermissions.TypeDelete, true, null),
        new RegisteredEndpoint("/api/v1/system/dict-types/{typeCode}/values", "get", DictPermissions.ValueList, true, null),
        new RegisteredEndpoint("/api/v1/system/dict-types/{typeCode}/values", "post", DictPermissions.ValueCreate, true, nameof(CreateDictValueRequest)),
        new RegisteredEndpoint("/api/v1/system/dict-values/{id:long}", "put", DictPermissions.ValueUpdate, true, nameof(UpdateDictValueRequest)),
        new RegisteredEndpoint("/api/v1/system/dict-values/{id:long}", "delete", DictPermissions.ValueDelete, true, null),
        new RegisteredEndpoint("/api/v1/system/settings", "get", SettingPermissions.List, true, null),
        new RegisteredEndpoint("/api/v1/system/settings/{key}", "get", SettingPermissions.Detail, true, null),
        new RegisteredEndpoint("/api/v1/system/settings/{key}", "put", SettingPermissions.Update, true, nameof(UpdateSettingRequest)),
        new RegisteredEndpoint("/api/v1/system/login-logs", "get", LogPermissions.LoginLogList, true, null),
        new RegisteredEndpoint("/api/v1/system/login-logs/{id:long}", "get", LogPermissions.LoginLogDetail, true, null),
        new RegisteredEndpoint("/api/v1/system/audit-logs", "get", LogPermissions.AuditLogList, true, null),
        new RegisteredEndpoint("/api/v1/system/audit-logs/{id:long}", "get", LogPermissions.AuditLogDetail, true, null),
        new RegisteredEndpoint("/api/v1/system/security-events", "get", LogPermissions.SecurityEventList, true, null),
        new RegisteredEndpoint("/api/v1/system/security-events/{id:long}", "get", LogPermissions.SecurityEventDetail, true, null),
        new RegisteredEndpoint("/api/v1/system/files", "get", FilePermissions.List, true, null),
        new RegisteredEndpoint("/api/v1/system/files/{id:long}", "get", FilePermissions.Detail, true, null),
        new RegisteredEndpoint("/api/v1/system/files", "post", FilePermissions.Upload, true, nameof(CreateFileRequest)),
        new RegisteredEndpoint("/api/v1/system/files/{id:long}/download", "get", FilePermissions.Download, true, null),
        new RegisteredEndpoint("/api/v1/system/files/{id:long}/preview", "get", FilePermissions.Download, true, null),
        new RegisteredEndpoint("/api/v1/system/files/{id:long}", "delete", FilePermissions.Delete, true, null)
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
