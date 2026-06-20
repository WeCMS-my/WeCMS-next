using Microsoft.Extensions.DependencyInjection;

namespace WeCms.Modules.Audit.SqlSugar;

public static class AuditSqlSugarServiceCollectionExtensions
{
    public static IServiceCollection AddWeCmsAuditSqlSugar(this IServiceCollection services)
    {
        return services;
    }
}
