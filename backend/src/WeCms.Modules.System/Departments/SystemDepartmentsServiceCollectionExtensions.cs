using Microsoft.Extensions.DependencyInjection;

namespace WeCms.Modules.System.Departments;

public static class SystemDepartmentsServiceCollectionExtensions
{
    public static IServiceCollection AddWeCmsSystemDepartments(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<IDepartmentService, DepartmentService>();
        return services;
    }
}
