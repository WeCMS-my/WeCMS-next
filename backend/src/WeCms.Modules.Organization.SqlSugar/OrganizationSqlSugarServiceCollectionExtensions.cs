using Microsoft.Extensions.DependencyInjection;
using WeCms.Data.SqlSugar;
using WeCms.Modules.Organization.Departments;
using WeCms.Modules.Organization.Positions;
using WeCms.Modules.Organization.SqlSugar.Entities;
using WeCms.Modules.Organization.SqlSugar.Repositories;

namespace WeCms.Modules.Organization.SqlSugar;

public static class OrganizationSqlSugarServiceCollectionExtensions
{
    public static IServiceCollection AddWeCmsOrganizationSqlSugar(this IServiceCollection services)
    {
        services.AddSingleton<ICodeFirstModelProvider, OrganizationCodeFirstModelProvider>();
        services.AddScoped<IDepartmentRepository, DepartmentRepository>();
        services.AddScoped<IPositionRepository, PositionRepository>();

        return services;
    }

    private sealed class OrganizationCodeFirstModelProvider : ICodeFirstModelProvider
    {
        public IReadOnlyCollection<Type> GetModelTypes()
        {
            return
            [
                typeof(DepartmentEntity),
                typeof(PositionEntity),
                typeof(UserPositionEntity)
            ];
        }
    }
}
