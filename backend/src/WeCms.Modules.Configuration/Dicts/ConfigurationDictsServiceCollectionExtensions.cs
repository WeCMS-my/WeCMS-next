using Microsoft.Extensions.DependencyInjection;

namespace WeCms.Modules.Configuration.Dicts;

public static class ConfigurationDictsServiceCollectionExtensions
{
    public static IServiceCollection AddWeCmsConfigurationDicts(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<IDictService, DictService>();
        return services;
    }
}
