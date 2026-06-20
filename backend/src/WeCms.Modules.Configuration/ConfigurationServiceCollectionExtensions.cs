using Microsoft.Extensions.DependencyInjection;

namespace WeCms.Modules.Configuration;

public static class ConfigurationServiceCollectionExtensions
{
    public static IServiceCollection AddWeCmsConfiguration(this IServiceCollection services)
    {
        return services;
    }
}
