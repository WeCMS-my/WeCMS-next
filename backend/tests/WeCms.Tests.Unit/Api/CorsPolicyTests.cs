using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using WeCms.Api.Security;

namespace WeCms.Tests.Unit.Api;

public sealed class CorsPolicyTests
{
    [Fact]
    public void AddWeCmsCors_UsesExplicitAllowedOriginsWithCredentials()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Security:AllowedOrigins:0"] = " https://admin.example.com ",
            })
            .Build();

        var services = new ServiceCollection();
        services.AddWeCmsCors(configuration);

        var options = services.BuildServiceProvider().GetRequiredService<IOptions<CorsOptions>>().Value;
        var policy = options.GetPolicy(WeCmsCorsPolicyNames.AdminApi);

        Assert.NotNull(policy);
        Assert.False(policy!.AllowAnyOrigin);
        Assert.True(policy.SupportsCredentials);
        Assert.True(policy.AllowAnyHeader);
        foreach (var method in new[] { "GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS" })
        {
            Assert.Contains(method, policy.Methods);
        }

        var allowedOrigins = policy.Origins.OrderBy(origin => origin).ToList();
        Assert.Equal(["https://admin.example.com"], allowedOrigins);
        Assert.DoesNotContain("*", allowedOrigins);
    }

    [Fact]
    public void AddWeCmsCors_DeduplicatesWhitespaceAndTrailingSlashOrigins()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Security:AllowedOrigins:0"] = "https://admin.example.com",
                ["Security:AllowedOrigins:1"] = "https://admin.example.com/ ",
                ["Security:AllowedOrigins:2"] = "  "
            })
            .Build();

        var services = new ServiceCollection();
        services.AddWeCmsCors(configuration);

        var options = services.BuildServiceProvider().GetRequiredService<IOptions<CorsOptions>>().Value;
        var policy = options.GetPolicy(WeCmsCorsPolicyNames.AdminApi);

        Assert.NotNull(policy);
        var allowedOrigins = policy!.Origins.OrderBy(origin => origin).ToList();

        Assert.Equal(["https://admin.example.com"], allowedOrigins);
    }
}
