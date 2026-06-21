using Microsoft.Extensions.DependencyInjection;
using WeCms.Data.SqlSugar;
using WeCms.Modules.Platform.SqlSugar.Entities;
using WeCms.Modules.Platform.SqlSugar.System;
using WeCms.Modules.Platform.System;

namespace WeCms.Modules.Platform.SqlSugar;

public static class PlatformSqlSugarServiceCollectionExtensions
{
    public static IServiceCollection AddWeCmsPlatformSqlSugar(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<ISystemDatabaseProbe, SystemDatabaseProbe>();
        services.AddScoped<ISystemMigrationProbe, SystemMigrationProbe>();
        services.AddSingleton<ICodeFirstModelProvider, PlatformCodeFirstModelProvider>();

        return services;
    }

    private sealed class PlatformCodeFirstModelProvider : ICodeFirstModelProvider
    {
        public IReadOnlyCollection<Type> GetModelTypes()
        {
            return [typeof(SchemaMigrationEntity)];
        }
    }
}
