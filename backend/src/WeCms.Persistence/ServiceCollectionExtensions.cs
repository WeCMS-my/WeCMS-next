using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WeCms.Persistence.Data;
using WeCms.Shared.Data;

namespace WeCms.Persistence;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWeCmsPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = new MySqlPersistenceOptions(
            configuration.GetConnectionString("WeCms") ?? string.Empty).Validate();

        services.AddSingleton(options);
        services.AddSingleton<ISqlSugarClientFactory, SqlSugarClientFactory>();
        services.AddScoped<IUnitOfWork, SqlSugarUnitOfWork>();

        return services;
    }
}
