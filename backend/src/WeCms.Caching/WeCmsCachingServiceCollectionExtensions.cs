using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace WeCms.Caching;

public static class WeCmsCachingServiceCollectionExtensions
{
    public static IServiceCollection AddWeCmsCaching(
        this IServiceCollection services,
        Action<CacheOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new CacheOptions();
        configure?.Invoke(options);

        services.AddMemoryCache();
        services.TryAddSingleton(options);
        services.TryAddSingleton<ICacheSerializer, SystemTextJsonCacheSerializer>();
        services.TryAddSingleton<ICacheKeyBuilder, DefaultCacheKeyBuilder>();
        services.TryAddSingleton<ICache, MemoryCacheProvider>();
        services.TryAddSingleton<ICacheInvalidator>(provider => provider.GetRequiredService<ICache>());

        return services;
    }
}
