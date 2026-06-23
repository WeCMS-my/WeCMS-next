using System.Security.Cryptography;
using System.Text;
using WeCms.Caching;
using WeCms.Modules.Security;

namespace WeCms.Api.Security;

public sealed class SecurityBanLookupCache : ISecurityBanLookupCache
{
    private static readonly TimeSpan MaxActiveBanCacheTtl = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan NegativeCacheTtl = TimeSpan.FromSeconds(5);

    private readonly ICache _cache;
    private readonly ICacheKeyBuilder _keyBuilder;

    public SecurityBanLookupCache(ICache cache, ICacheKeyBuilder keyBuilder)
    {
        _cache = cache;
        _keyBuilder = keyBuilder;
    }

    public async ValueTask<SecurityBanLookupCacheResult?> GetAsync(
        string banType,
        string target,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var key = BuildActiveBanCacheKey(banType, target);
        var cached = await _cache.GetAsync<SecurityBanLookupCacheEntry>(key, cancellationToken);
        if (cached is null)
        {
            return null;
        }

        if (cached.Record is null)
        {
            return SecurityBanLookupCacheResult.Miss;
        }

        if (IsActive(cached.Record, now))
        {
            return new SecurityBanLookupCacheResult(cached.Record);
        }

        await _cache.RemoveAsync(key, cancellationToken);
        return null;
    }

    public async ValueTask SetAsync(
        SecurityBanRecord record,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(record);
        if (!IsActive(record, now))
        {
            await RemoveAsync(record.BanType, record.Target, cancellationToken);
            return;
        }

        var ttl = CacheTtl(record, now);
        if (ttl <= TimeSpan.Zero)
        {
            return;
        }

        await _cache.SetAsync(
            BuildActiveBanCacheKey(record.BanType, record.Target),
            new SecurityBanLookupCacheEntry(record),
            new CacheEntryOptions { AbsoluteExpirationRelativeToNow = ttl },
            cancellationToken);
    }

    public ValueTask SetMissAsync(
        string banType,
        string target,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        return _cache.SetAsync(
            BuildActiveBanCacheKey(banType, target),
            new SecurityBanLookupCacheEntry(null),
            new CacheEntryOptions { AbsoluteExpirationRelativeToNow = NegativeCacheTtl },
            cancellationToken);
    }

    public ValueTask RemoveAsync(
        string banType,
        string target,
        CancellationToken cancellationToken)
    {
        return _cache.RemoveAsync(BuildActiveBanCacheKey(banType, target), cancellationToken);
    }

    private string BuildActiveBanCacheKey(string banType, string target)
    {
        return _keyBuilder.Build(new CacheKeyParts(
            "system",
            "security",
            "active-ban",
            $"{banType}-{HashTarget(target)}",
            "v1"));
    }

    private static bool IsActive(SecurityBanRecord record, DateTimeOffset now)
    {
        return record.RevokedAt is null && (record.ExpiresAt is null || record.ExpiresAt > now);
    }

    private static TimeSpan CacheTtl(SecurityBanRecord record, DateTimeOffset now)
    {
        if (record.ExpiresAt is null)
        {
            return MaxActiveBanCacheTtl;
        }

        var remaining = record.ExpiresAt.Value - now;
        return remaining < MaxActiveBanCacheTtl ? remaining : MaxActiveBanCacheTtl;
    }

    private static string HashTarget(string target)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(target));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private sealed record SecurityBanLookupCacheEntry(SecurityBanRecord? Record);
}
