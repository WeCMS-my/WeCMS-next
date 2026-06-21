using WeCms.Caching;

namespace WeCms.Tests.Unit.Caching;

public sealed class RedisCacheReservationTests
{
    [Fact]
    public void RedisCacheOptions_AreDisabledByDefault()
    {
        var options = new RedisCacheOptions();

        Assert.False(options.Enabled);
        Assert.Equal("Redis", options.ConnectionStringName);
        Assert.Equal("wecms", options.InstanceName);
    }

    [Fact]
    public async Task RedisCacheProvider_IsExplicitlyUnsupportedUntilConfigured()
    {
        var provider = new UnsupportedRedisCacheProvider(new RedisCacheOptions());

        var exception = await Assert.ThrowsAsync<NotSupportedException>(async () =>
            await provider.GetAsync<string>("wecms:test:tenant:module:resource:v1:reserved", TestContext.Current.CancellationToken));

        Assert.Contains("Redis cache provider is reserved but not configured", exception.Message, StringComparison.Ordinal);
    }
}
