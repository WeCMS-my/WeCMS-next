using System.Globalization;
using Microsoft.Extensions.Caching.Memory;
using WeCms.Modules.AccessControl.Contracts;

namespace WeCms.Modules.AccessControl.AccessProfiles;

public interface IAccessProfileCache
{
    ValueTask<AccessProfileDto?> GetAsync(
        long userId,
        long permissionVersion,
        CancellationToken cancellationToken);

        ValueTask SetAsync(
            long userId,
            AccessProfileDto profile,
            CancellationToken cancellationToken);
}

public sealed class AccessProfileCache : IAccessProfileCache
{
    private static readonly TimeSpan AbsoluteExpiration = TimeSpan.FromMinutes(5);
    private readonly IMemoryCache _cache;

    public AccessProfileCache(IMemoryCache cache)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public ValueTask<AccessProfileDto?> GetAsync(
        long userId,
        long permissionVersion,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return new ValueTask<AccessProfileDto?>(_cache.Get<AccessProfileDto>(BuildCacheKey(userId, permissionVersion)));
    }

    public ValueTask SetAsync(
        long userId,
        AccessProfileDto profile,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(profile);

        var options = new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = AbsoluteExpiration };
        _cache.Set(BuildCacheKey(userId, profile.PermissionVersion), profile, options);
        return ValueTask.CompletedTask;
    }

    private static string BuildCacheKey(long userId, long permissionVersion)
    {
        var identifier = string.Create(CultureInfo.InvariantCulture, $"{userId}-{permissionVersion}");
        return string.Join(':', "system", "access-control", "access-profile", identifier, "v1");
    }
}
