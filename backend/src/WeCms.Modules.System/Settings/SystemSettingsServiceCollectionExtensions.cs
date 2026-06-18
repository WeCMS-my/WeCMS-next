using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.DataProtection;

namespace WeCms.Modules.System.Settings;

public static class SystemSettingsServiceCollectionExtensions
{
    public static IServiceCollection AddWeCmsSystemSettings(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddDataProtection();
        services.AddSingleton<ISettingDefinitionProvider, SettingDefinitionProvider>();
        services.AddSingleton<ISettingSecretProtector, DataProtectionSettingSecretProtector>();
        services.AddSingleton<ISettingCache, SettingCache>();
        services.AddScoped<ISettingService, SettingService>();
        return services;
    }
}
