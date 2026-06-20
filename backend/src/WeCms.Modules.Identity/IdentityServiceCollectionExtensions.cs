using Microsoft.Extensions.DependencyInjection;

namespace WeCms.Modules.Identity;

public static class IdentityServiceCollectionExtensions
{
    public static IServiceCollection AddWeCmsIdentity(this IServiceCollection services)
    {
        return services;
    }
}
