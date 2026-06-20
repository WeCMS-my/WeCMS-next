using Microsoft.Extensions.DependencyInjection;

namespace WeCms.Modules.Audit;

public static class AuditServiceCollectionExtensions
{
    public static IServiceCollection AddWeCmsAudit(this IServiceCollection services)
    {
        return services;
    }
}
