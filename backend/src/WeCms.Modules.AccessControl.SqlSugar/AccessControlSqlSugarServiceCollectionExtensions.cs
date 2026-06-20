using Microsoft.Extensions.DependencyInjection;

namespace WeCms.Modules.AccessControl.SqlSugar;

public static class AccessControlSqlSugarServiceCollectionExtensions
{
    public static IServiceCollection AddWeCmsAccessControlSqlSugar(this IServiceCollection services)
    {
        return services;
    }
}
