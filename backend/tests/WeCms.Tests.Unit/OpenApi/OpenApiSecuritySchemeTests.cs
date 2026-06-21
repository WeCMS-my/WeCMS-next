using System.Text.Json;
using WeCms.Api.Extensions;

namespace WeCms.Tests.Unit.OpenApi;

public sealed class OpenApiSecuritySchemeTests
{
    [Fact]
    public async Task OpenApi_ContainsBearerAuth()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"wecms-openapi-bearer-{Guid.NewGuid():N}.json");
        try
        {
            var handled = await OpenApiExtensions.ExportOpenApiAsync(["--export-openapi", outputPath], TestContext.Current.CancellationToken);

            Assert.True(handled);
            using var document = JsonDocument.Parse(await File.ReadAllTextAsync(outputPath, TestContext.Current.CancellationToken));
            var bearerAuth = document.RootElement
                .GetProperty("components")
                .GetProperty("securitySchemes")
                .GetProperty("bearerAuth");

            Assert.Equal("http", bearerAuth.GetProperty("type").GetString());
            Assert.Equal("bearer", bearerAuth.GetProperty("scheme").GetString());
            Assert.Equal("JWT", bearerAuth.GetProperty("bearerFormat").GetString());
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
