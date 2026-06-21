using Microsoft.Extensions.DependencyInjection;
using WeCms.Data.SqlSugar;
using WeCms.Modules.Security;
using WeCms.Modules.Security.Events;
using WeCms.Modules.Security.SqlSugar.Entities;
using WeCms.Modules.Security.SqlSugar.Repositories;

namespace WeCms.Modules.Security.SqlSugar;

public static class SecuritySqlSugarServiceCollectionExtensions
{
    public static IServiceCollection AddWeCmsSecuritySqlSugar(this IServiceCollection services)
    {
        services.AddSingleton<ICodeFirstModelProvider, SecurityCodeFirstModelProvider>();
        services.AddScoped<ISecurityEventRepository, SecurityEventRepository>();
        services.AddScoped<ISecurityBanRepository, SecurityBanRepository>();
        services.AddScoped<IRateLimitSecurityEventRepository, RateLimitSecurityEventRepository>();

        return services;
    }

    private sealed class SecurityCodeFirstModelProvider : ICodeFirstModelProvider
    {
        public IReadOnlyCollection<Type> GetModelTypes()
        {
            return
            [
                typeof(SecurityBanEntity),
                typeof(SecurityEventEntity)
            ];
        }
    }
}
