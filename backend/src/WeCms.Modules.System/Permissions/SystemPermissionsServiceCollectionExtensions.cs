using Microsoft.Extensions.DependencyInjection;

namespace WeCms.Modules.System.Permissions;

public static class SystemPermissionsServiceCollectionExtensions
{
    public static IServiceCollection AddWeCmsSystemPermissions(this IServiceCollection services)
    {
        services.AddScoped<IPermissionChecker, PermissionChecker>();

        return services;
    }
}
