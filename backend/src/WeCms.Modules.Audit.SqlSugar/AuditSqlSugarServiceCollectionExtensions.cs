using Microsoft.Extensions.DependencyInjection;
using WeCms.Data.SqlSugar;
using WeCms.Modules.Audit.Logs;
using WeCms.Modules.Audit.SqlSugar.Entities;
using WeCms.Modules.Audit.SqlSugar.Repositories;

namespace WeCms.Modules.Audit.SqlSugar;

public static class AuditSqlSugarServiceCollectionExtensions
{
    public static IServiceCollection AddWeCmsAuditSqlSugar(this IServiceCollection services)
    {
        services.AddSingleton<ICodeFirstModelProvider, AuditCodeFirstModelProvider>();
        services.AddScoped<ILogRepository, LogRepository>();

        return services;
    }

    private sealed class AuditCodeFirstModelProvider : ICodeFirstModelProvider
    {
        public IReadOnlyCollection<Type> GetModelTypes()
        {
            return
            [
                typeof(AuditLogEntity),
                typeof(LoginLogEntity)
            ];
        }
    }
}
