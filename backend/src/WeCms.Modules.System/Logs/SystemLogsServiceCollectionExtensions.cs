using Microsoft.Extensions.DependencyInjection;

namespace WeCms.Modules.System.Logs;

public static class SystemLogsServiceCollectionExtensions
{
    public static IServiceCollection AddWeCmsSystemLogs(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<ILogService, LogService>();
        return services;
    }
}
