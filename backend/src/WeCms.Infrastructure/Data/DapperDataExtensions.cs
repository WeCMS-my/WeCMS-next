using Microsoft.Extensions.DependencyInjection;
using WeCms.Shared.Time;

namespace WeCms.Infrastructure.Data;

public static class DapperDataExtensions
{
    public static IServiceCollection AddWeCmsData(this IServiceCollection services)
    {
        services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }

    public static IServiceCollection AddWeCmsSystemClock(this IServiceCollection services)
    {
        services.AddSingleton<IClock, Time.SystemClock>();
        return services;
    }
}
