using Microsoft.Extensions.DependencyInjection;

namespace WeCms.Modules.System.Menus;

public static class SystemMenusServiceCollectionExtensions
{
    public static IServiceCollection AddWeCmsSystemMenus(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<IMenuService, MenuService>();
        return services;
    }
}
