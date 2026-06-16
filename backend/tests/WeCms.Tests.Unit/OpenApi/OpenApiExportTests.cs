using System.Text.Json;
using WeCms.Api.Extensions;

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

    [Fact]
    public async Task ExportOpenApiAsync_ReturnsFalseWhenArgumentIsMissing()
    {
        var handled = await OpenApiExtensions.ExportOpenApiAsync([]);

        Assert.False(handled);
    }
}
