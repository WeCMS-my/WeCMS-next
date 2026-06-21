using Microsoft.Extensions.DependencyInjection;

namespace WeCms.Modules.Configuration.I18n;

public static class ConfigurationI18nServiceCollectionExtensions
{
    public static IServiceCollection AddWeCmsConfigurationI18n(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<II18nMessageService, I18nMessageService>();
        return services;
    }
}
