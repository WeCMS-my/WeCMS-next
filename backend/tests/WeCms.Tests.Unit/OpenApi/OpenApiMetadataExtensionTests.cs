using System.Text.Json;
using WeCms.Api.Extensions;
using WeCms.Shared.Endpoints;
using WeCms.Shared.Security;

namespace WeCms.Tests.Unit.OpenApi;

public sealed partial class OpenApiExportTests
{
    [Fact]
    public async Task OpenApiExport_IncludesModuleMetadata()
    {
        using var document = await ExportDocumentAsync("metadata-module");
        var paths = document.RootElement.GetProperty("paths");

        Assert.Equal("identity", ExtensionString(paths, "/api/v1/system/users", "get", EndpointOpenApiExtensionNames.Module));
        Assert.Equal("access-control", ExtensionString(paths, "/api/v1/system/roles", "get", EndpointOpenApiExtensionNames.Module));
        Assert.Equal("organization", ExtensionString(paths, "/api/v1/system/depts", "get", EndpointOpenApiExtensionNames.Module));
        Assert.Equal("configuration", ExtensionString(paths, "/api/v1/system/settings", "get", EndpointOpenApiExtensionNames.Module));
        Assert.Equal("file-center", ExtensionString(paths, "/api/v1/system/files", "get", EndpointOpenApiExtensionNames.Module));
    }

    [Fact]
    public async Task OpenApiExport_IncludesAuditMetadataForWrites()
    {
        using var document = await ExportDocumentAsync("metadata-audit");
        var paths = document.RootElement.GetProperty("paths");

        AssertAudit(paths, "/api/v1/system/users", "post", "identity", "users", "create");
        AssertAudit(paths, "/api/v1/system/menus/sort", "put", "access-control", "menus", "sort");
        AssertAudit(paths, "/api/v1/system/settings/reload-cache", "post", "configuration", "settings", "reload-cache");
        AssertAudit(paths, "/api/v1/system/files/{id:long}", "delete", "file-center", "files", "delete");
    }

    [Fact]
    public async Task OpenApiExport_IncludesRateLimitMetadata()
    {
        using var document = await ExportDocumentAsync("metadata-rate-limit");
        var paths = document.RootElement.GetProperty("paths");

        Assert.Equal(RateLimitPolicyNames.AuthLogin, ExtensionString(paths, "/api/v1/auth/login", "post", EndpointOpenApiExtensionNames.RateLimit));
        Assert.Equal(RateLimitPolicyNames.AdminWrite, ExtensionString(paths, "/api/v1/system/users", "post", EndpointOpenApiExtensionNames.RateLimit));
        Assert.Equal(RateLimitPolicyNames.FileUpload, ExtensionString(paths, "/api/v1/system/files", "post", EndpointOpenApiExtensionNames.RateLimit));
        Assert.Equal(RateLimitPolicyNames.SecurityUnban, ExtensionString(paths, "/api/v1/system/security/bans/{id:long}/unban", "post", EndpointOpenApiExtensionNames.RateLimit));
    }

    [Fact]
    public async Task OpenApiExport_PermissionExtensionDoesNotFallbackToStaticDescriptor()
    {
        var source = await File.ReadAllTextAsync(
            RepoPath("backend", "src", "WeCms.Api", "Extensions", "OpenApiExtensions.cs"),
            TestContext.Current.CancellationToken);

        Assert.DoesNotContain("metadata?.Permission ?? permission", source, StringComparison.Ordinal);
        Assert.DoesNotContain("permission: endpoint.Permission", source, StringComparison.Ordinal);
    }

    [Fact]
    public async Task OpenApiExport_StillCoversAllBusinessEndpoints()
    {
        using var document = await ExportDocumentAsync("metadata-coverage");
        var openApiOperations = CollectOpenApiOperations(document.RootElement.GetProperty("paths"));

        AssertOpenApiOperationsMatch(CollectRegisteredEndpointMetadata(), openApiOperations);
    }

    private static async Task<JsonDocument> ExportDocumentAsync(string purpose)
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"wecms-openapi-{purpose}-{Guid.NewGuid():N}.json");
        try
        {
            var handled = await OpenApiExtensions.ExportOpenApiAsync(["--export-openapi", outputPath], TestContext.Current.CancellationToken);

            Assert.True(handled);
            return JsonDocument.Parse(await File.ReadAllTextAsync(outputPath, TestContext.Current.CancellationToken));
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    private static string? ExtensionString(JsonElement paths, string path, string method, string extensionName)
    {
        return paths.GetProperty(path)
            .GetProperty(method)
            .GetProperty(extensionName)
            .GetString();
    }

    private static void AssertAudit(
        JsonElement paths,
        string path,
        string method,
        string module,
        string resource,
        string action)
    {
        var audit = paths.GetProperty(path)
            .GetProperty(method)
            .GetProperty(EndpointOpenApiExtensionNames.Audit);

        Assert.Equal(module, audit.GetProperty("module").GetString());
        Assert.Equal(resource, audit.GetProperty("resource").GetString());
        Assert.Equal(action, audit.GetProperty("action").GetString());
    }

    private static string RepoPath(params string[] segments)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "backend", "WeCms.slnx")))
            {
                return Path.Combine([directory.FullName, .. segments]);
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
