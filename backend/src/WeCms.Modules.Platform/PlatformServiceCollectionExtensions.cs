using Microsoft.Extensions.DependencyInjection;

namespace WeCms.Modules.Platform;

public static class PlatformServiceCollectionExtensions
{
    public static IServiceCollection AddWeCmsPlatform(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services;
    }
}
