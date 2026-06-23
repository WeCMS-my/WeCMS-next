using System.Collections.Concurrent;
using Microsoft.Extensions.Caching.Memory;

namespace WeCms.Caching;

public sealed class MemoryCacheProvider : ICache
{
    private readonly IMemoryCache memoryCache;
    private readonly ICacheSerializer serializer;
    private readonly CacheOptions cacheOptions;
    private readonly object _gate = new();
    private readonly ConcurrentDictionary<string, byte> keys = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, long> prefixInvalidationVersions = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, SemaphoreSlim> locks = new(StringComparer.Ordinal);
    private const int MaxPrefixInvalidationVersions = 256;
    private long cacheVersion;
    private long prefixInvalidationVersionFloor = -1;

    public MemoryCacheProvider(
        IMemoryCache memoryCache,
        ICacheSerializer serializer,
        CacheOptions cacheOptions)
    {
        this.memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        this.serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        this.cacheOptions = cacheOptions ?? throw new ArgumentNullException(nameof(cacheOptions));
    }

    public ValueTask<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateKey(key);

        return ValueTask.FromResult(TryGetValue<T>(key, out var value) ? value : default);
    }

    public ValueTask SetAsync<T>(
        string key,
        T? value,
        CacheEntryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateKey(key);

        if (value is null && !ShouldCacheNull(options))
        {
            return RemoveAsync(key, cancellationToken);
        }

        var payload = new MemoryCachePayload(serializer.Serialize(value), Volatile.Read(ref cacheVersion));
        var entryOptions = BuildEntryOptions(key, options);

        memoryCache.Set(key, payload, entryOptions);
        keys[key] = 0;

        return ValueTask.CompletedTask;
    }

    public ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateKey(key);

        memoryCache.Remove(key);
        keys.TryRemove(key, out _);

        return ValueTask.CompletedTask;
    }

    public async ValueTask<T?> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, ValueTask<T?>> factory,
        CacheEntryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateKey(key);
        ArgumentNullException.ThrowIfNull(factory);

        if (TryGetValue<T>(key, out var cached))
        {
            return cached;
        }

        var gate = locks.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);

        try
        {
            if (TryGetValue<T>(key, out cached))
            {
                return cached;
            }

            var created = await factory(cancellationToken);
            await SetAsync(key, created, options, cancellationToken);

            return created;
        }
        finally
        {
            gate.Release();
            if (gate.CurrentCount == 1)
            {
                locks.TryRemove(key, out _);
            }
        }
    }

    public ValueTask RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ValidateKey(prefix);

        var version = Interlocked.Increment(ref cacheVersion);
        lock (_gate)
        {
            prefixInvalidationVersions.AddOrUpdate(prefix, version, (_, current) => Math.Max(current, version));
            if (prefixInvalidationVersions.Count > MaxPrefixInvalidationVersions)
            {
                CompactPrefixInvalidationsLocked();
            }
        }

        return ValueTask.CompletedTask;
    }

    private bool TryGetValue<T>(string key, out T? value)
    {
        if (memoryCache.TryGetValue(key, out MemoryCachePayload? payload) && payload is not null)
        {
            if (IsInvalidated(key, payload.Version))
            {
                memoryCache.Remove(key);
                keys.TryRemove(key, out _);
                value = default;
                return false;
            }

            value = serializer.Deserialize<T>(payload.Value);
            return true;
        }

        value = default;
        return false;
    }

    private bool IsInvalidated(string key, long payloadVersion)
    {
        if (payloadVersion <= Volatile.Read(ref prefixInvalidationVersionFloor))
        {
            return true;
        }

        foreach (var invalidation in prefixInvalidationVersions)
        {
            if (invalidation.Value > payloadVersion && key.StartsWith(invalidation.Key, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private void CompactPrefixInvalidationsLocked()
    {
        if (prefixInvalidationVersions.Count <= MaxPrefixInvalidationVersions)
        {
            return;
        }

        var ordered = prefixInvalidationVersions.OrderBy(entry => entry.Value).ToArray();
        if (ordered.Length == 0)
        {
            return;
        }

        var droppedVersions = new List<long>();
        var keepPrefixes = new HashSet<string>(StringComparer.Ordinal);
        for (var index = ordered.Length - 1; index >= 0; index--)
        {
            var candidate = ordered[index].Key;
            var coveredByKept = false;
            foreach (var kept in keepPrefixes)
            {
                if (candidate.StartsWith(kept, StringComparison.Ordinal))
                {
                    coveredByKept = true;
                    break;
                }
            }

            if (!coveredByKept)
            {
                keepPrefixes.Add(candidate);
                if (keepPrefixes.Count == MaxPrefixInvalidationVersions)
                {
                    break;
                }
            }
        }

        foreach (var invalidation in ordered)
        {
            if (!keepPrefixes.Contains(invalidation.Key))
            {
                droppedVersions.Add(invalidation.Value);
                prefixInvalidationVersions.TryRemove(invalidation.Key, out _);
            }
        }

        if (droppedVersions.Count > 0)
        {
            var newFloor = droppedVersions.Min();
            var currentFloor = Volatile.Read(ref prefixInvalidationVersionFloor);
            if (newFloor > currentFloor)
            {
                Interlocked.Exchange(ref prefixInvalidationVersionFloor, newFloor);
            }
        }
    }

    private MemoryCacheEntryOptions BuildEntryOptions(string key, CacheEntryOptions? options)
    {
        var entryOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = options?.AbsoluteExpirationRelativeToNow,
            SlidingExpiration = options?.SlidingExpiration
        };

        entryOptions.RegisterPostEvictionCallback(static (evictedKey, _, _, state) =>
        {
            if (evictedKey is string keyValue && state is ConcurrentDictionary<string, byte> keys)
            {
                keys.TryRemove(keyValue, out _);
            }
        }, keys);

        return entryOptions;
    }

    private bool ShouldCacheNull(CacheEntryOptions? options)
    {
        return options?.CacheNullValues ?? cacheOptions.CacheNullValues;
    }

    private static void ValidateKey(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Cache key is required.", nameof(key));
        }
    }

    private sealed record MemoryCachePayload(byte[] Value, long Version);
}
