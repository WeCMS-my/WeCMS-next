using Microsoft.Extensions.DependencyInjection;

namespace WeCms.Modules.System.Dicts;

public static class SystemDictsServiceCollectionExtensions
{
    public static IServiceCollection AddWeCmsSystemDicts(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<IDictService, DictService>();
        return services;
    }
}
