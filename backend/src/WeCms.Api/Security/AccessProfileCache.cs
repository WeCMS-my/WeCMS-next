using WeCms.Caching;
using WeCms.Modules.AccessControl.AccessProfiles;
using WeCms.Modules.AccessControl.Contracts;

namespace WeCms.Api.Security;

public sealed class AccessProfileCache : IAccessProfileCache
{
    private static readonly CacheEntryOptions EntryOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
    };

    private readonly ICache _cache;
    private readonly ICacheKeyBuilder _keyBuilder;

    public AccessProfileCache(ICache cache, ICacheKeyBuilder keyBuilder)
    {
        _cache = cache;
        _keyBuilder = keyBuilder;
    }

    public ValueTask<AccessProfileDto?> GetAsync(
        long userId,
        bool isSuperAdmin,
        long permissionVersion,
        CancellationToken cancellationToken)
    {
        return _cache.GetAsync<AccessProfileDto>(
            BuildCacheKey(userId, isSuperAdmin, permissionVersion),
            cancellationToken);
    }

    public ValueTask SetAsync(
        long userId,
        bool isSuperAdmin,
        AccessProfileDto profile,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(profile);
        return _cache.SetAsync(
            BuildCacheKey(userId, isSuperAdmin, profile.PermissionVersion),
            profile,
            EntryOptions,
            cancellationToken);
    }

    private string BuildCacheKey(long userId, bool isSuperAdmin, long permissionVersion)
    {
        return _keyBuilder.Build(new CacheKeyParts(
            "system",
            "access-control",
            "access-profile",
            $"{userId}-{(isSuperAdmin ? "super" : "normal")}-{permissionVersion}",
            "v1"));
    }
}
