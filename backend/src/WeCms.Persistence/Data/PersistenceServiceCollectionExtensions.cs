using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SqlSugar;
using WeCms.Modules.System.Auth;
using WeCms.Modules.System.Permissions;
using WeCms.Modules.System.System;
using WeCms.Persistence.Modules.System.Auth;
using WeCms.Persistence.Modules.System.Permissions;
using WeCms.Persistence.Modules.System.System;
using WeCms.Persistence.Migration;
using WeCms.Shared.Data;

namespace WeCms.Persistence.Data;

public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddWeCmsPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var connectionString = configuration.GetConnectionString("Default");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new PersistenceConfigurationException("ConnectionStrings:Default is required for WeCMS persistence.");
        }

        services.AddScoped<ISqlSugarClientFactory>(_ => new SqlSugarClientFactory(connectionString));
        services.AddScoped<ISqlSugarClient>(sp => sp.GetRequiredService<ISqlSugarClientFactory>().Create());
        services.AddScoped<IUnitOfWork, SqlSugarUnitOfWork>();
        services.AddScoped<IAuthRepository, AuthRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<ISystemDatabaseProbe, SystemDatabaseProbe>();
        services.AddScoped<IDbMigrationRunner, DbMigrationRunner>();
        services.AddScoped<ISeedRunner, SeedRunner>();

        return services;
    }
}
