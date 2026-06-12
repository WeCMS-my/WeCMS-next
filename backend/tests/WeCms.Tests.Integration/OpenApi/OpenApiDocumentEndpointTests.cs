using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace WeCms.Tests.Integration.OpenApi;

public sealed class OpenApiDocumentEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public OpenApiDocumentEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Database:AutoMigrate"] = "false",
                    ["ConnectionStrings:Default"] = "Server=127.0.0.1;Port=1;Database=wecms_dev;User=wecms;Password=wecms-dev-123;Connection Timeout=1;Default Command Timeout=1;"
                });
            });
        }).CreateClient();
    }

    [Fact]
    public async Task OpenApiDocument_ShouldReturnSuccess_AndContainSecurePingPath()
    {
        using var response = await _client.GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        await using var stream = await response.Content.ReadAsStreamAsync();
        using var document = await JsonDocument.ParseAsync(stream);

        var paths = document.RootElement.GetProperty("paths")
            .EnumerateObject()
            .Select(path => path.Name)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Contains("/api/v1/system/secure-ping", paths);
    }
}
