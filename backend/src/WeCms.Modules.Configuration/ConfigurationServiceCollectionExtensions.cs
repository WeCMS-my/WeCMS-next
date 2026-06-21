using Microsoft.Extensions.DependencyInjection;
using WeCms.EventBus;
using WeCms.Modules.Configuration.Dicts;
using WeCms.Modules.Configuration.Events;
using WeCms.Modules.Configuration.I18n;
using WeCms.Modules.Configuration.Settings;

namespace WeCms.Modules.Configuration;

public static class ConfigurationServiceCollectionExtensions
{
    public static IServiceCollection AddWeCmsConfiguration(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IConfigurationClock, SystemConfigurationClock>();
        services.AddSingleton<IConfigurationCacheInvalidator, NoopConfigurationCacheInvalidator>();
        services.AddWeCmsConfigurationSettings();
        services.AddWeCmsConfigurationDicts();
        services.AddWeCmsConfigurationI18n();
        services
            .AddIntegrationEvent<SettingChangedEvent>(SettingChangedEvent.EventType)
            .AddIntegrationEvent<DictChangedEvent>(DictChangedEvent.EventType)
            .AddIntegrationEvent<I18nChangedEvent>(I18nChangedEvent.EventType)
            .AddEventHandler<SettingChangedEvent, SettingChangedCacheHandler>()
            .AddEventHandler<DictChangedEvent, DictChangedCacheHandler>()
            .AddEventHandler<I18nChangedEvent, I18nChangedCacheHandler>();

        return services;
    }
}
