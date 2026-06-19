using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using WeCms.Api.Security;

namespace WeCms.Tests.Unit.Api;

public sealed class ForwardedHeadersExtensionsTests
{
    [Fact]
    public void IsEnabled_ReturnsFalseWhenForwardedHeadersAreDisabled()
    {
        var configuration = Configuration(new Dictionary<string, string?>
        {
            ["Security:ForwardedHeaders:Enabled"] = "false"
        });

        Assert.False(WeCmsForwardedHeadersExtensions.IsEnabled(configuration));
    }

    [Fact]
    public void IsEnabled_ReturnsTrueWhenForwardedHeadersAreEnabled()
    {
        var configuration = Configuration(new Dictionary<string, string?>
        {
            ["Security:ForwardedHeaders:Enabled"] = "true"
        });

        Assert.True(WeCmsForwardedHeadersExtensions.IsEnabled(configuration));
    }

    [Fact]
    public void AddWeCmsForwardedHeaders_ConfiguresKnownValuesAndDefaultForwardLimit()
    {
        var services = new ServiceCollection();
        services.AddOptions();

        services.AddWeCmsForwardedHeaders(Configuration(new Dictionary<string, string?>
        {
            ["Security:ForwardedHeaders:ForwardLimit"] = "3",
            ["Security:ForwardedHeaders:KnownProxies:0"] = "10.0.0.10"
        }));

        var options = services
            .BuildServiceProvider()
            .GetRequiredService<IOptions<ForwardedHeadersOptions>>()
            .Value;

        Assert.Equal(3, options.ForwardLimit);
        Assert.Single(options.KnownProxies);
    }

    [Fact]
    public void AddWeCmsForwardedHeaders_UsesDefaultForwardLimitWhenMissing()
    {
        var services = new ServiceCollection();
        services.AddOptions();
        services.AddWeCmsForwardedHeaders(Configuration(new Dictionary<string, string?>()));

        var options = services
            .BuildServiceProvider()
            .GetRequiredService<IOptions<ForwardedHeadersOptions>>()
            .Value;

        Assert.Equal(1, options.ForwardLimit);
    }

    [Fact]
    public void AddWeCmsForwardedHeaders_RejectsInvalidForwardLimit()
    {
        var services = new ServiceCollection();
        services.AddOptions();
        services.AddWeCmsForwardedHeaders(Configuration(new Dictionary<string, string?>
        {
            ["Security:ForwardedHeaders:ForwardLimit"] = "0"
        }));

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services
                .BuildServiceProvider()
                .GetRequiredService<IOptions<ForwardedHeadersOptions>>()
                .Value);

        Assert.Equal("Security:ForwardedHeaders:ForwardLimit must be an integer between 1 and 32.", exception.Message);
    }

    private static IConfiguration Configuration(IReadOnlyDictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
