using Microsoft.Extensions.DependencyInjection;
using WeCms.Aop;
using WeCms.Caching;

namespace WeCms.Tests.Unit.Aop;

public sealed class CacheInterceptorTests
{
    [Fact]
    public async Task CacheInterceptor_ReturnsCachedValue()
    {
        using var provider = CreateProvider();
        var interceptor = provider.GetRequiredService<CacheInterceptor>();
        var cache = provider.GetRequiredService<ICache>();
        var context = new CacheInvocationContext("tenant-a", ["alice"]);
        var attribute = new CacheableAttribute("identity:users:list");
        var key = interceptor.BuildKey(attribute, context);

        await cache.SetAsync(key, new CacheSample("cached"), cancellationToken: TestContext.Current.CancellationToken);
        var calls = 0;

        var result = await interceptor.InvokeCacheableAsync(
            attribute,
            context,
            _ =>
            {
                calls++;
                return Task.FromResult<CacheSample?>(new CacheSample("fresh"));
            },
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(new CacheSample("cached"), result);
        Assert.Equal(0, calls);
    }

    [Fact]
    public async Task CacheInterceptor_WritesOnMiss()
    {
        using var provider = CreateProvider();
        var interceptor = provider.GetRequiredService<CacheInterceptor>();
        var cache = provider.GetRequiredService<ICache>();
        var context = new CacheInvocationContext("tenant-a", [1, "enabled"]);
        var attribute = new CacheableAttribute("configuration:dicts:list");
        var calls = 0;

        var first = await interceptor.InvokeCacheableAsync(
            attribute,
            context,
            _ =>
            {
                calls++;
                return Task.FromResult<CacheSample?>(new CacheSample("fresh"));
            },
            cancellationToken: TestContext.Current.CancellationToken);
        var second = await cache.GetAsync<CacheSample>(interceptor.BuildKey(attribute, context), TestContext.Current.CancellationToken);

        Assert.Equal(new CacheSample("fresh"), first);
        Assert.Equal(new CacheSample("fresh"), second);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task CacheInterceptor_EvictsAfterMutation()
    {
        using var provider = CreateProvider();
        var interceptor = provider.GetRequiredService<CacheInterceptor>();
        var cache = provider.GetRequiredService<ICache>();
        var context = new CacheInvocationContext("tenant-a", [7]);
        var attribute = new CacheEvictAttribute("configuration:settings:detail");
        var key = interceptor.BuildKey(attribute, context);
        var mutationCalled = false;

        await cache.SetAsync(key, new CacheSample("old"), cancellationToken: TestContext.Current.CancellationToken);

        await interceptor.InvokeEvictAsync(
            attribute,
            context,
            _ =>
            {
                mutationCalled = true;
                return Task.CompletedTask;
            },
            TestContext.Current.CancellationToken);

        Assert.True(mutationCalled);
        Assert.Null(await cache.GetAsync<CacheSample>(key, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CacheInterceptor_EvictsByPrefixAfterMutation()
    {
        using var provider = CreateProvider();
        var interceptor = provider.GetRequiredService<CacheInterceptor>();
        var cache = provider.GetRequiredService<ICache>();
        var attribute = new CacheEvictAttribute("configuration:settings:detail", CacheEvictionMode.Prefix);
        var firstContext = new CacheInvocationContext("tenant-a", [1]);
        var secondContext = new CacheInvocationContext("tenant-a", [2]);
        var otherTenantContext = new CacheInvocationContext("tenant-b", [1]);
        var firstKey = interceptor.BuildKey(attribute, firstContext);
        var secondKey = interceptor.BuildKey(attribute, secondContext);
        var otherTenantKey = interceptor.BuildKey(attribute, otherTenantContext);

        await cache.SetAsync(firstKey, new CacheSample("first"), cancellationToken: TestContext.Current.CancellationToken);
        await cache.SetAsync(secondKey, new CacheSample("second"), cancellationToken: TestContext.Current.CancellationToken);
        await cache.SetAsync(otherTenantKey, new CacheSample("other"), cancellationToken: TestContext.Current.CancellationToken);

        await interceptor.InvokeEvictAsync(
            attribute,
            firstContext,
            _ => Task.CompletedTask,
            TestContext.Current.CancellationToken);

        Assert.Null(await cache.GetAsync<CacheSample>(firstKey, TestContext.Current.CancellationToken));
        Assert.Null(await cache.GetAsync<CacheSample>(secondKey, TestContext.Current.CancellationToken));
        Assert.Equal(new CacheSample("other"), await cache.GetAsync<CacheSample>(otherTenantKey, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CacheInterceptor_DoesNotCacheException()
    {
        using var provider = CreateProvider();
        var interceptor = provider.GetRequiredService<CacheInterceptor>();
        var context = new CacheInvocationContext("tenant-a", ["boom"]);
        var attribute = new CacheableAttribute("identity:users:detail");
        var calls = 0;

        await Assert.ThrowsAsync<InvalidOperationException>(() => interceptor.InvokeCacheableAsync<CacheSample>(
            attribute,
            context,
            _ =>
            {
                calls++;
                throw new InvalidOperationException("cache source failed");
            },
            cancellationToken: TestContext.Current.CancellationToken));

        var value = await interceptor.InvokeCacheableAsync(
            attribute,
            context,
            _ =>
            {
                calls++;
                return Task.FromResult<CacheSample?>(new CacheSample("recovered"));
            },
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(new CacheSample("recovered"), value);
        Assert.Equal(2, calls);
    }

    [Fact]
    public void CacheInterceptor_UsesTenantAwareKey()
    {
        using var provider = CreateProvider();
        var interceptor = provider.GetRequiredService<CacheInterceptor>();
        var attribute = new CacheableAttribute("identity:users:list");

        var tenantAKey = interceptor.BuildKey(attribute, new CacheInvocationContext("tenant-a", [1, "enabled"]));
        var tenantBKey = interceptor.BuildKey(attribute, new CacheInvocationContext("tenant-b", [1, "enabled"]));
        var differentParametersKey = interceptor.BuildKey(attribute, new CacheInvocationContext("tenant-a", [2, "enabled"]));

        Assert.StartsWith("wecms:test:tenant-a:identity:users:v1:list-", tenantAKey, StringComparison.Ordinal);
        Assert.StartsWith("wecms:test:tenant-b:identity:users:v1:list-", tenantBKey, StringComparison.Ordinal);
        Assert.NotEqual(tenantAKey, tenantBKey);
        Assert.NotEqual(tenantAKey, differentParametersKey);
    }

    [Fact]
    public async Task CacheInterceptor_HonorsNullCachePolicy()
    {
        using var provider = CreateProvider(options => options.CacheNullValues = true);
        var interceptor = provider.GetRequiredService<CacheInterceptor>();
        var context = new CacheInvocationContext("tenant-a", ["missing"]);
        var attribute = new CacheableAttribute("identity:users:detail");
        var calls = 0;

        var first = await interceptor.InvokeCacheableAsync<CacheSample>(
            attribute,
            context,
            _ =>
            {
                calls++;
                return Task.FromResult<CacheSample?>(null);
            },
            cancellationToken: TestContext.Current.CancellationToken);
        var second = await interceptor.InvokeCacheableAsync<CacheSample>(
            attribute,
            context,
            _ =>
            {
                calls++;
                return Task.FromResult<CacheSample?>(new CacheSample("unexpected"));
            },
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Null(first);
        Assert.Null(second);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task CacheInterceptor_DoesNotCacheNull_WhenPolicyDisallows()
    {
        using var provider = CreateProvider();
        var interceptor = provider.GetRequiredService<CacheInterceptor>();
        var context = new CacheInvocationContext("tenant-a", ["missing"]);
        var attribute = new CacheableAttribute("identity:users:detail");
        var calls = 0;

        _ = await interceptor.InvokeCacheableAsync<CacheSample>(
            attribute,
            context,
            _ =>
            {
                calls++;
                return Task.FromResult<CacheSample?>(null);
            },
            cancellationToken: TestContext.Current.CancellationToken);
        var second = await interceptor.InvokeCacheableAsync<CacheSample>(
            attribute,
            context,
            _ =>
            {
                calls++;
                return Task.FromResult<CacheSample?>(new CacheSample("loaded"));
            },
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(new CacheSample("loaded"), second);
        Assert.Equal(2, calls);
    }

    private static ServiceProvider CreateProvider(Action<CacheOptions>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddWeCmsCaching(options =>
        {
            options.ApplicationName = "wecms";
            options.EnvironmentName = "test";
            options.Version = "v1";
            configure?.Invoke(options);
        });
        services.AddSingleton<CacheInterceptor>();

        return services.BuildServiceProvider();
    }

    private sealed record CacheSample(string Value);
}
