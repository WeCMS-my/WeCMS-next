using System.Globalization;
using WeCms.Caching;
using WeCms.Modules.AccessControl.AccessProfiles;
using WeCms.Modules.AccessControl.Contracts;
using WeCms.Modules.AccessControl.Repositories;

namespace WeCms.Api.AccessProfiles;

public sealed class CachedAccessProfileService : IAccessProfileService
{
    private const string DefaultTenant = "default";
    private static readonly CacheEntryOptions CacheOptions = new()
    {
        AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
    };

    private readonly AccessProfileService _inner;
    private readonly IAccessProfileRepository _repository;
    private readonly ICache _cache;
    private readonly ICacheKeyBuilder _cacheKeyBuilder;

    public CachedAccessProfileService(
        AccessProfileService inner,
        IAccessProfileRepository repository,
        ICache cache,
        ICacheKeyBuilder cacheKeyBuilder)
    {
        _inner = inner;
        _repository = repository;
        _cache = cache;
        _cacheKeyBuilder = cacheKeyBuilder;
    }

    public async Task<AccessProfileDto> GetAsync(long userId, bool isSuperAdmin, CancellationToken cancellationToken)
    {
        var permissionVersion = await _repository.GetPermissionVersionAsync(userId, cancellationToken);
        var cacheKey = BuildCacheKey(userId, isSuperAdmin, permissionVersion);

        return await _cache.GetOrCreateAsync<AccessProfileDto>(
            cacheKey,
            async token => await _inner.GetAsync(userId, isSuperAdmin, permissionVersion, token),
            CacheOptions,
            cancellationToken)
            ?? throw new InvalidOperationException("Access profile cache factory returned null.");
    }

    private string BuildCacheKey(long userId, bool isSuperAdmin, long permissionVersion)
    {
        var identifier = string.Create(
            CultureInfo.InvariantCulture,
            $"{userId}-{(isSuperAdmin ? "super-admin" : "regular")}");

        return _cacheKeyBuilder.Build(new CacheKeyParts(
            DefaultTenant,
            "access-control",
            "access-profile",
            identifier,
            $"pv{permissionVersion.ToString(CultureInfo.InvariantCulture)}"));
    }
}
