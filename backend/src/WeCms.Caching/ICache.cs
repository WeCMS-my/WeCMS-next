namespace WeCms.Caching;

public interface ICache : ICacheInvalidator
{
    ValueTask<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    ValueTask SetAsync<T>(
        string key,
        T? value,
        CacheEntryOptions? options = null,
        CancellationToken cancellationToken = default);

    ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default);

    new ValueTask RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default);

    ValueTask<T?> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, ValueTask<T?>> factory,
        CacheEntryOptions? options = null,
        CancellationToken cancellationToken = default);
}
