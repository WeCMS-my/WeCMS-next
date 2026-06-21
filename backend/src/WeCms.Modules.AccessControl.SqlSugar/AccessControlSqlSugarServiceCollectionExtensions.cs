using Microsoft.Extensions.DependencyInjection;
using WeCms.Data.SqlSugar;
using WeCms.Modules.AccessControl.Permissions;
using WeCms.Modules.AccessControl.Records;
using WeCms.Modules.AccessControl.Repositories;
using WeCms.Modules.AccessControl.SqlSugar.Entities;
using WeCms.Modules.AccessControl.SqlSugar.Repositories;

namespace WeCms.Modules.AccessControl.SqlSugar;

public static class AccessControlSqlSugarServiceCollectionExtensions
{
    public static IServiceCollection AddWeCmsAccessControlSqlSugar(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IAccessProfileRepository, AccessProfileRepository>();
        services.AddScoped<IMenuRepository, MenuRepository>();
        services.AddScoped<IPermissionRepository, PermissionRepository>();
        services.AddScoped<IPermissionSecurityEventWriter, PermissionSecurityEventRepository>();
        services.AddScoped<IPermissionVersionRepository, PermissionVersionRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddSingleton<ICodeFirstModelProvider, AccessControlCodeFirstModelProvider>();

        return services;
    }

    private sealed class AccessControlCodeFirstModelProvider : ICodeFirstModelProvider
    {
        public IReadOnlyCollection<Type> GetModelTypes()
        {
            return
            [
                typeof(MenuEntity),
                typeof(PermissionEntity),
                typeof(RoleEntity),
                typeof(RoleMenuEntity),
                typeof(RolePermissionEntity),
                typeof(UserRoleEntity)
            ];
        }
    }
}
