using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using WeCms.Api;
using Xunit;

namespace WeCms.Tests.Integration;

public class HealthEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public HealthEndpointTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.UseSetting("ConnectionStrings:Default", "Server=nonexistent;Database=test;User=test;Password=test;");
            builder.UseSetting("Auth:JwtSecret", "test-secret-at-least-32-chars-long!!");
            builder.UseSetting("Cors:AllowedOrigins:0", "http://localhost:5173");
        });
    }

    [Fact]
    public async Task PingEndpoint_ShouldReturnPong()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/system/ping");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("pong", content);
    }

    [Fact]
    public async Task HealthLive_ShouldReturn200()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/v1/system/health/live");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
