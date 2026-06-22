using Microsoft.Extensions.DependencyInjection;
using WeCms.Data.SqlSugar;
using WeCms.EventBus.SqlSugar.Entities;
using WeCms.EventBus.SqlSugar.Repositories;

namespace WeCms.EventBus.SqlSugar;

public static class WeCmsEventBusSqlSugarServiceCollectionExtensions
{
    public static IServiceCollection AddWeCmsEventBusSqlSugar(this IServiceCollection services)
    {
        services.AddSingleton<ICodeFirstModelProvider, EventBusCodeFirstModelProvider>();
        services.AddScoped<IOutboxMessageRepository, OutboxMessageRepository>();
        services.AddScoped<IOutboxWriter, SqlSugarOutboxWriter>();
        services.AddHostedService<OutboxDispatcherHostedService>();

        return services;
    }

    private sealed class EventBusCodeFirstModelProvider : ICodeFirstModelProvider
    {
        public IReadOnlyCollection<Type> GetModelTypes()
        {
            return [typeof(OutboxMessageEntity)];
        }
    }
}
