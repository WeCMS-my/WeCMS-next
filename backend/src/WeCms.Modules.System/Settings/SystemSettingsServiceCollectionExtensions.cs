using Microsoft.Extensions.DependencyInjection;

namespace WeCms.Modules.System.Settings;

public static class SystemSettingsServiceCollectionExtensions
{
    public static IServiceCollection AddWeCmsSystemSettings(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<ISettingService, SettingService>();
        return services;
    }
}
