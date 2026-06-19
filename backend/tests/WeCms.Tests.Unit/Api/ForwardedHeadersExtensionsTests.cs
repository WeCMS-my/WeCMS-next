using Microsoft.Extensions.Configuration;
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

    private static IConfiguration Configuration(IReadOnlyDictionary<string, string?> values)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
    }
}
