using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.DataProtection;

namespace WeCms.Modules.Configuration.Settings;

public static class ConfigurationSettingsServiceCollectionExtensions
{
    public static IServiceCollection AddWeCmsConfigurationSettings(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddDataProtection();
        services.AddSingleton<ISettingDefinitionProvider, SettingDefinitionProvider>();
        services.AddSingleton<ISettingSecretProtector, DataProtectionSettingSecretProtector>();
        services.AddScoped<ISettingService, SettingService>();
        return services;
    }
}
