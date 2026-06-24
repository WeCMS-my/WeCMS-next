using WeCms.Caching;
using WeCms.Modules.AccessControl.AccessProfiles;
using WeCms.Modules.AccessControl.Contracts;
using WeCms.Shared.Data;

namespace WeCms.Api.AccessControl;

public sealed class CachingAccessProfileCache : IAccessProfileCache
{
    private const string Module = "access-control";
    private const string Resource = "access-profile";
    private static readonly TimeSpan AbsoluteExpiration = TimeSpan.FromMinutes(5);

    private readonly ICache _cache;
    private readonly ICacheKeyBuilder _keyBuilder;
    private readonly ICacheTenantAccessor _tenantAccessor;

    public CachingAccessProfileCache(
        ICache cache,
        ICacheKeyBuilder keyBuilder,
        ICacheTenantAccessor tenantAccessor)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _keyBuilder = keyBuilder ?? throw new ArgumentNullException(nameof(keyBuilder));
        _tenantAccessor = tenantAccessor ?? throw new ArgumentNullException(nameof(tenantAccessor));
    }

    public ValueTask<AccessProfileDto?> GetAsync(
        long userId,
        long permissionVersion,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return _cache.GetAsync<AccessProfileDto>(BuildCacheKey(_tenantAccessor.GetCurrentTenantId(), userId, permissionVersion), cancellationToken);
    }

    public ValueTask SetAsync(
        long userId,
        AccessProfileDto profile,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentNullException.ThrowIfNull(profile);

        var options = new CacheEntryOptions { AbsoluteExpirationRelativeToNow = AbsoluteExpiration };
        return _cache.SetAsync(BuildCacheKey(_tenantAccessor.GetCurrentTenantId(), userId, profile.PermissionVersion), profile, options, cancellationToken);
    }

    private string BuildCacheKey(string tenantId, long userId, long permissionVersion)
    {
        return _keyBuilder.Build(new CacheKeyParts(tenantId, Module, Resource, $"{userId}-{permissionVersion}"));
    }
}
