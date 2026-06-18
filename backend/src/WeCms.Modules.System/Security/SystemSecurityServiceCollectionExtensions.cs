using Microsoft.Extensions.DependencyInjection;

namespace WeCms.Modules.System.Security;

public static class SystemSecurityServiceCollectionExtensions
{
    public static IServiceCollection AddWeCmsSystemSecurity(this IServiceCollection services)
    {
        services.AddScoped<ISecurityBanService, SecurityBanService>();
        return services;
    }
}
