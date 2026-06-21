using Microsoft.Extensions.DependencyInjection;
using WeCms.Modules.Organization.Departments;
using WeCms.Modules.Organization.Positions;

namespace WeCms.Modules.Organization;

public static class OrganizationServiceCollectionExtensions
{
    public static IServiceCollection AddWeCmsOrganization(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IOrganizationClock, SystemOrganizationClock>();
        services.AddScoped<IOrganizationLookupService, OrganizationLookupService>();
        services.AddScoped<IDepartmentService, DepartmentService>();
        services.AddScoped<IPositionService, PositionService>();
        return services;
    }
}
