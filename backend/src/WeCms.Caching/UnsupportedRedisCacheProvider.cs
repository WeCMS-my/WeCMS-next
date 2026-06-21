namespace WeCms.Caching;

public sealed class UnsupportedRedisCacheProvider : ICache
{
    private readonly RedisCacheOptions options;

    public UnsupportedRedisCacheProvider(RedisCacheOptions options)
    {
        this.options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public ValueTask<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        throw NotConfigured();
    }

    public ValueTask SetAsync<T>(
        string key,
        T? value,
        CacheEntryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        throw NotConfigured();
    }

    public ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        throw NotConfigured();
    }

    public ValueTask RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        throw NotConfigured();
    }

    public ValueTask<T?> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, ValueTask<T?>> factory,
        CacheEntryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        throw NotConfigured();
    }

    private NotSupportedException NotConfigured()
    {
        var suffix = options.Enabled
            ? $" Connection string name: '{options.ConnectionStringName}'."
            : " Enable it through explicit Redis configuration first.";

        return new NotSupportedException("Redis cache provider is reserved but not configured." + suffix);
    }
}
