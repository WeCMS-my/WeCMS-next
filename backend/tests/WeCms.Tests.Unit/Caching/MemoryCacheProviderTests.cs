using Microsoft.Extensions.DependencyInjection;
using WeCms.Caching;

namespace WeCms.Tests.Unit.Caching;

public sealed class MemoryCacheProviderTests
{
    [Fact]
    public async Task MemoryCache_GetSetRemove()
    {
        using var provider = CreateProvider();
        var cache = provider.GetRequiredService<ICache>();
        var cancellationToken = TestContext.Current.CancellationToken;

        await cache.SetAsync("wecms:test:tenant:module:resource:v1:1", new CacheSample("alpha"), cancellationToken: cancellationToken);
        var value = await cache.GetAsync<CacheSample>("wecms:test:tenant:module:resource:v1:1", cancellationToken);

        Assert.Equal(new CacheSample("alpha"), value);

        await cache.RemoveAsync("wecms:test:tenant:module:resource:v1:1", cancellationToken);
        var removed = await cache.GetAsync<CacheSample>("wecms:test:tenant:module:resource:v1:1", cancellationToken);

        Assert.Null(removed);
    }

    [Fact]
    public async Task MemoryCache_ExpiresEntries()
    {
        using var provider = CreateProvider();
        var cache = provider.GetRequiredService<ICache>();
        var cancellationToken = TestContext.Current.CancellationToken;

        await cache.SetAsync(
            "wecms:test:tenant:module:resource:v1:expiring",
            new CacheSample("short"),
            new CacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMilliseconds(20) },
            cancellationToken);

        await Task.Delay(120, cancellationToken);
        var value = await cache.GetAsync<CacheSample>("wecms:test:tenant:module:resource:v1:expiring", cancellationToken);

        Assert.Null(value);
    }

    [Fact]
    public async Task MemoryCache_CachesNull_WhenPolicyAllows()
    {
        using var provider = CreateProvider();
        var cache = provider.GetRequiredService<ICache>();
        var cancellationToken = TestContext.Current.CancellationToken;
        var calls = 0;

        var first = await cache.GetOrCreateAsync<string>(
            "wecms:test:tenant:module:resource:v1:null",
            _ =>
            {
                calls++;
                return ValueTask.FromResult<string?>(null);
            },
            new CacheEntryOptions { CacheNullValues = true },
            cancellationToken);

        var second = await cache.GetOrCreateAsync<string>(
            "wecms:test:tenant:module:resource:v1:null",
            _ =>
            {
                calls++;
                return ValueTask.FromResult<string?>(null);
            },
            new CacheEntryOptions { CacheNullValues = true },
            cancellationToken);

        Assert.Null(first);
        Assert.Null(second);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task MemoryCache_DoesNotCacheNull_WhenPolicyDisallows()
    {
        using var provider = CreateProvider();
        var cache = provider.GetRequiredService<ICache>();
        var cancellationToken = TestContext.Current.CancellationToken;
        var calls = 0;

        _ = await cache.GetOrCreateAsync<string>(
            "wecms:test:tenant:module:resource:v1:null-miss",
            _ =>
            {
                calls++;
                return ValueTask.FromResult<string?>(null);
            },
            cancellationToken: cancellationToken);

        _ = await cache.GetOrCreateAsync<string>(
            "wecms:test:tenant:module:resource:v1:null-miss",
            _ =>
            {
                calls++;
                return ValueTask.FromResult<string?>(null);
            },
            cancellationToken: cancellationToken);

        Assert.Equal(2, calls);
    }

    [Fact]
    public async Task MemoryCache_GetOrCreate_IsSingleFlightPerKey()
    {
        using var provider = CreateProvider();
        var cache = provider.GetRequiredService<ICache>();
        var cancellationToken = TestContext.Current.CancellationToken;
        var calls = 0;

        var tasks = Enumerable.Range(0, 12)
            .Select(_ => cache.GetOrCreateAsync(
                "wecms:test:tenant:module:resource:v1:single-flight",
                async cancellationToken =>
                {
                    Interlocked.Increment(ref calls);
                    await Task.Delay(40, cancellationToken);
                    return new CacheSample("single");
                },
                cancellationToken: cancellationToken).AsTask())
            .ToArray();

        var results = await Task.WhenAll(tasks);

        Assert.All(results, result => Assert.Equal(new CacheSample("single"), result));
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task CacheInvalidator_RemovesByPrefix()
    {
        using var provider = CreateProvider();
        var cache = provider.GetRequiredService<ICache>();
        var cancellationToken = TestContext.Current.CancellationToken;

        await cache.SetAsync("wecms:test:tenant:configuration:settings:v1:a", new CacheSample("a"), cancellationToken: cancellationToken);
        await cache.SetAsync("wecms:test:tenant:configuration:settings:v1:b", new CacheSample("b"), cancellationToken: cancellationToken);
        await cache.SetAsync("wecms:test:tenant:identity:users:v1:c", new CacheSample("c"), cancellationToken: cancellationToken);

        await cache.RemoveByPrefixAsync("wecms:test:tenant:configuration:", cancellationToken);

        Assert.Null(await cache.GetAsync<CacheSample>("wecms:test:tenant:configuration:settings:v1:a", cancellationToken));
        Assert.Null(await cache.GetAsync<CacheSample>("wecms:test:tenant:configuration:settings:v1:b", cancellationToken));
        Assert.Equal(new CacheSample("c"), await cache.GetAsync<CacheSample>("wecms:test:tenant:identity:users:v1:c", cancellationToken));
    }

    [Fact]
    public async Task CacheInvalidator_InvalidatesPrefixWithoutEnumeratingTrackedKeys()
    {
        using var provider = CreateProvider();
        var cache = provider.GetRequiredService<ICache>();
        var cancellationToken = TestContext.Current.CancellationToken;
        var key = "wecms:test:tenant:configuration:settings:v1:a";

        await cache.SetAsync(key, new CacheSample("old"), cancellationToken: cancellationToken);
        await cache.SetAsync("wecms:test:tenant:identity:users:v1:c", new CacheSample("c"), cancellationToken: cancellationToken);

        await cache.RemoveByPrefixAsync("wecms:test:tenant:configuration:", cancellationToken);

        Assert.Null(await cache.GetAsync<CacheSample>(key, cancellationToken));
        Assert.Equal(new CacheSample("c"), await cache.GetAsync<CacheSample>("wecms:test:tenant:identity:users:v1:c", cancellationToken));

        await cache.SetAsync(key, new CacheSample("fresh"), cancellationToken: cancellationToken);

        Assert.Equal(new CacheSample("fresh"), await cache.GetAsync<CacheSample>(key, cancellationToken));

        var source = await File.ReadAllTextAsync(
            RepoPath("backend", "src", "WeCms.Caching", "MemoryCacheProvider.cs"),
            cancellationToken);
        Assert.Contains("prefixInvalidationVersions", source, StringComparison.Ordinal);
        Assert.DoesNotContain("foreach (var key in keys.Keys)", source, StringComparison.Ordinal);
    }

    [Fact]
    public void AddWeCmsCaching_RegistersMemoryCacheProviderAndAbstractions()
    {
        using var provider = CreateProvider(options =>
        {
            options.ApplicationName = "wecms";
            options.EnvironmentName = "test";
            options.Version = "v2";
            options.CacheNullValues = true;
        });

        var cache = provider.GetRequiredService<ICache>();
        var invalidator = provider.GetRequiredService<ICacheInvalidator>();
        var keyBuilder = provider.GetRequiredService<ICacheKeyBuilder>();
        var serializer = provider.GetRequiredService<ICacheSerializer>();
        var options = provider.GetRequiredService<CacheOptions>();

        Assert.IsType<MemoryCacheProvider>(cache);
        Assert.Same(cache, invalidator);
        Assert.IsType<DefaultCacheKeyBuilder>(keyBuilder);
        Assert.IsType<SystemTextJsonCacheSerializer>(serializer);
        Assert.True(options.CacheNullValues);
        Assert.Equal("wecms:test:tenant:configuration:settings:v2:site", keyBuilder.Build(new CacheKeyParts(
            Tenant: "tenant",
            Module: "configuration",
            Resource: "settings",
            Identifier: "site")));
    }

    private static ServiceProvider CreateProvider(Action<CacheOptions>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddWeCmsCaching(configure);

        return services.BuildServiceProvider();
    }

    private static string RepoPath(params string[] segments)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "backend", "WeCms.slnx")))
            {
                return Path.Combine([directory.FullName, .. segments]);
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }

    private sealed record CacheSample(string Value);
}
