using Microsoft.Extensions.DependencyInjection;
using WeCms.Data.SqlSugar;
using WeCms.Modules.Configuration.Dicts;
using WeCms.Modules.Configuration.I18n;
using WeCms.Modules.Configuration.Settings;
using WeCms.Modules.Configuration.SqlSugar.Entities;
using WeCms.Modules.Configuration.SqlSugar.Repositories;

namespace WeCms.Modules.Configuration.SqlSugar;

public static class ConfigurationSqlSugarServiceCollectionExtensions
{
    public static IServiceCollection AddWeCmsConfigurationSqlSugar(this IServiceCollection services)
    {
        services.AddSingleton<ICodeFirstModelProvider, ConfigurationCodeFirstModelProvider>();
        services.AddScoped<IDictRepository, DictRepository>();
        services.AddScoped<II18nMessageRepository, I18nMessageRepository>();
        services.AddScoped<ISettingRepository, SettingRepository>();

        return services;
    }

    private sealed class ConfigurationCodeFirstModelProvider : ICodeFirstModelProvider
    {
        public IReadOnlyCollection<Type> GetModelTypes()
        {
            return
            [
                typeof(DictTypeEntity),
                typeof(DictValueEntity),
                typeof(I18nMessageEntity),
                typeof(SettingEntity)
            ];
        }
    }
}
