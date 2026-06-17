using System.Text.Json;
using WeCms.Api.Extensions;
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
            var handled = await OpenApiExtensions.ExportOpenApiAsync(["--export-openapi", outputPath]);

            Assert.True(handled);
            using var document = JsonDocument.Parse(await File.ReadAllTextAsync(outputPath));
            var schemas = document.RootElement.GetProperty("components").GetProperty("schemas");

            AssertSchemaProperties(
                schemas.GetProperty(nameof(CreateUserRequest)),
                required: ["username", "displayName", "password"],
                optional: ["email", "phone", "deptId", "roleIds", "postIds"]);

            AssertSchemaProperties(
                schemas.GetProperty(nameof(CreateRoleRequest)),
                required: ["code", "name"],
                optional: ["permissionIds", "menuIds"]);
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
        Assert.Contains("\"sys:file:download\"", permissionCoverage, StringComparison.Ordinal);
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
