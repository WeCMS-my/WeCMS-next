using Microsoft.Extensions.DependencyInjection;

namespace WeCms.Modules.Security;

public static class SecurityServiceCollectionExtensions
{
    public static IServiceCollection AddWeCmsSecurity(this IServiceCollection services)
    {
        return services;
    }
}
