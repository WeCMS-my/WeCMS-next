using Microsoft.Extensions.DependencyInjection;

namespace WeCms.Modules.System.Roles;

public static class SystemRolesServiceCollectionExtensions
{
    public static IServiceCollection AddWeCmsSystemRoles(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IRoleService, RoleService>();

        return services;
    }
}
