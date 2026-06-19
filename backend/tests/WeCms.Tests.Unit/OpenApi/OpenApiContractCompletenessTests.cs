using System.Text.Json;
using WeCms.Api.Extensions;
using WeCms.Modules.System.Departments;
using WeCms.Modules.System.Dicts;
using WeCms.Modules.System.Files;
using WeCms.Modules.System.I18n;
using WeCms.Modules.System.Menus;
using WeCms.Modules.System.Permissions;
using WeCms.Modules.System.Posts;
using WeCms.Modules.System.Roles;
using WeCms.Modules.System.Users;

namespace WeCms.Tests.Unit.OpenApi;

public sealed class OpenApiContractCompletenessTests
{
    [Fact]
    public async Task ExportOpenApiAsync_ExposesOptionalRequestPropertiesWithoutRequiringThem()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"wecms-openapi-schema-completeness-{Guid.NewGuid():N}.json");
        try
        {
            var handled = await OpenApiExtensions.ExportOpenApiAsync(["--export-openapi", outputPath], TestContext.Current.CancellationToken);

            Assert.True(handled);
            using var document = JsonDocument.Parse(await File.ReadAllTextAsync(outputPath, TestContext.Current.CancellationToken));
            var schemas = document.RootElement.GetProperty("components").GetProperty("schemas");

            AssertSchemaProperties(
                schemas.GetProperty(nameof(CreateUserRequest)),
                required: ["username", "displayName", "password"],
                optional: ["email", "phone", "deptId", "roleIds", "postIds"]);

            AssertSchemaProperties(
                schemas.GetProperty(nameof(CreateRoleRequest)),
                required: ["code", "name"],
                optional: ["permissionIds", "menuIds"]);

            AssertSchemaProperties(
                schemas.GetProperty(nameof(UpdateUserRequest)),
                required: ["displayName"],
                optional: ["email", "phone", "deptId"]);

            AssertSchemaProperties(
                schemas.GetProperty(nameof(CreatePermissionRequest)),
                required: ["code", "name", "module"],
                optional: ["description"]);

            AssertSchemaProperties(
                schemas.GetProperty(nameof(UpdatePermissionRequest)),
                required: ["name", "module"],
                optional: ["description"]);

            AssertSchemaProperties(
                schemas.GetProperty(nameof(CreateMenuRequest)),
                required: ["type", "code", "path", "title", "sort", "hidden", "keepAlive", "status"],
                optional: ["parentId", "component", "i18nKey", "icon", "externalUrl", "permissionCode"]);

            AssertSchemaProperties(
                schemas.GetProperty(nameof(UpdateMenuRequest)),
                required: ["type", "path", "title", "sort", "hidden", "keepAlive", "status"],
                optional: ["parentId", "component", "i18nKey", "icon", "externalUrl"]);

            AssertSchemaProperties(
                schemas.GetProperty(nameof(CreateDepartmentRequest)),
                required: ["code", "name", "sortOrder", "status"],
                optional: ["parentId"]);

            AssertSchemaProperties(
                schemas.GetProperty(nameof(UpdateDepartmentRequest)),
                required: ["name", "sortOrder", "status"],
                optional: ["parentId"]);

            AssertSchemaProperties(
                schemas.GetProperty(nameof(CreatePostRequest)),
                required: ["code", "name", "sortOrder", "status"],
                optional: []);

            AssertSchemaProperties(
                schemas.GetProperty(nameof(UpdatePostRequest)),
                required: ["name", "sortOrder", "status"],
                optional: []);

            AssertSchemaProperties(
                schemas.GetProperty(nameof(CreateDictTypeRequest)),
                required: ["code", "name", "sortOrder", "status"],
                optional: ["description"]);

            AssertSchemaProperties(
                schemas.GetProperty(nameof(UpdateDictTypeRequest)),
                required: ["name", "sortOrder", "status"],
                optional: ["description"]);

            AssertSchemaProperties(
                schemas.GetProperty(nameof(CreateDictValueRequest)),
                required: ["label", "value", "sortOrder", "isDefault", "status"],
                optional: ["description"]);

            AssertSchemaProperties(
                schemas.GetProperty(nameof(UpdateDictValueRequest)),
                required: ["label", "value", "sortOrder", "isDefault", "status"],
                optional: ["description"]);

            AssertSchemaProperties(
                schemas.GetProperty(nameof(CreateI18nMessageRequest)),
                required: ["locale", "module", "messageKey", "messageValue", "status"],
                optional: ["remark"]);

            AssertSchemaProperties(
                schemas.GetProperty(nameof(UpdateI18nMessageRequest)),
                required: ["module", "messageValue", "status"],
                optional: ["remark"]);
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
    public async Task ExportOpenApiAsync_FileDownloadAndPreviewUseSamePermissionAndHaveNoRequestBody()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"wecms-openapi-file-access-{Guid.NewGuid():N}.json");
        try
        {
            var handled = await OpenApiExtensions.ExportOpenApiAsync(["--export-openapi", outputPath], TestContext.Current.CancellationToken);

            Assert.True(handled);
            using var document = JsonDocument.Parse(await File.ReadAllTextAsync(outputPath, TestContext.Current.CancellationToken));
            var paths = document.RootElement.GetProperty("paths");

            var download = paths.GetProperty("/api/v1/system/files/{id:long}/download").GetProperty("get");
            var preview = paths.GetProperty("/api/v1/system/files/{id:long}/preview").GetProperty("get");

            Assert.Equal("sys:file:download", download.GetProperty("x-wecms-permission").GetString());
            Assert.Equal("sys:file:download", preview.GetProperty("x-wecms-permission").GetString());
            Assert.False(download.TryGetProperty("requestBody", out _));
            Assert.False(preview.TryGetProperty("requestBody", out _));
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
    public void SystemCoverageScripts_IncludeFileDownloadPreviewAndPermission()
    {
        var repoRoot = RepoRoot;
        var openApiCoverage = File.ReadAllText(Path.Combine(repoRoot, "scripts", "checks", "check-system-openapi-coverage.sh"));
        var permissionCoverage = File.ReadAllText(Path.Combine(repoRoot, "scripts", "checks", "check-system-permission-coverage.sh"));

        Assert.Contains("\"/api/v1/system/files/{id:long}/download\": {\"get\"}", openApiCoverage, StringComparison.Ordinal);
        Assert.Contains("\"/api/v1/system/files/{id:long}/preview\": {\"get\"}", openApiCoverage, StringComparison.Ordinal);
        Assert.Contains("\"/api/v1/system/i18n/messages\": {\"get\", \"post\"}", openApiCoverage, StringComparison.Ordinal);
        Assert.Contains("\"sys:file:download\"", permissionCoverage, StringComparison.Ordinal);
        Assert.Contains("\"sys:i18n:list\"", permissionCoverage, StringComparison.Ordinal);
    }

    [Fact]
    public void SystemCoverageScript_DoesNotRequireRequestBodyForBodylessCommandPosts()
    {
        var openApiCoverage = File.ReadAllText(Path.Combine(RepoRoot, "scripts", "checks", "check-system-openapi-coverage.sh"));

        Assert.DoesNotContain("if method in {\"post\", \"put\"} and \"requestBody\" not in operation", openApiCoverage, StringComparison.Ordinal);
        Assert.Contains("request_body_required = {", openApiCoverage, StringComparison.Ordinal);
        Assert.Contains("(\"/api/v1/system/users/{id:long}/disable\", \"post\")", openApiCoverage, StringComparison.Ordinal);
    }

    private static void AssertSchemaProperties(JsonElement schema, string[] required, string[] optional)
    {
        var actualRequired = schema.GetProperty("required")
            .EnumerateArray()
            .Select(item => item.GetString())
            .ToHashSet();
        var properties = schema.GetProperty("properties");

        Assert.Equal(required.Order(StringComparer.Ordinal), actualRequired.Order(StringComparer.Ordinal));

        foreach (var propertyName in required.Concat(optional))
        {
            Assert.True(properties.TryGetProperty(propertyName, out _), $"{propertyName} is missing.");
        }

        foreach (var propertyName in optional)
        {
            Assert.DoesNotContain(propertyName, actualRequired);
        }
    }

    private static string RepoRoot
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "backend", "WeCms.slnx")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException("Could not locate repository root.");
        }
    }
}
