using System.Text.Json;
using WeCms.Api.Extensions;

namespace WeCms.Tests.Unit.OpenApi;

public sealed class OpenApiPermissionExtensionTests
{
    [Fact]
    public async Task OpenApi_ContainsPermissionExtensions()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"wecms-openapi-permissions-{Guid.NewGuid():N}.json");
        try
        {
            var handled = await OpenApiExtensions.ExportOpenApiAsync(["--export-openapi", outputPath], TestContext.Current.CancellationToken);

            Assert.True(handled);
            using var document = JsonDocument.Parse(await File.ReadAllTextAsync(outputPath, TestContext.Current.CancellationToken));
            var paths = document.RootElement.GetProperty("paths");
            var healthDependencies = paths.GetProperty("/health/dependencies").GetProperty("get");
            var securePing = paths.GetProperty("/api/v1/system/secure-ping").GetProperty("get");

            Assert.Equal("sys:system:secure-ping", healthDependencies.GetProperty("x-wecms-permission").GetString());
            Assert.Equal("sys:system:secure-ping", securePing.GetProperty("x-wecms-permission").GetString());
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }
}
