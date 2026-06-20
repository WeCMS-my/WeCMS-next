using Microsoft.Extensions.DependencyInjection;

namespace WeCms.Modules.AccessControl;

public static class AccessControlServiceCollectionExtensions
{
    public static IServiceCollection AddWeCmsAccessControl(this IServiceCollection services)
    {
        return services;
    }
}
