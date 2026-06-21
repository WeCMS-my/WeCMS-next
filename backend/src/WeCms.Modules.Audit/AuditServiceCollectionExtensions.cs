using Microsoft.Extensions.DependencyInjection;
using WeCms.Modules.Audit.Logs;

namespace WeCms.Modules.Audit;

public static class AuditServiceCollectionExtensions
{
    public static IServiceCollection AddWeCmsAudit(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<ILogService, LogService>();
        return services;
    }
}
