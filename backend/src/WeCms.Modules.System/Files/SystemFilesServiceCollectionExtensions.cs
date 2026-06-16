using Microsoft.Extensions.DependencyInjection;

namespace WeCms.Modules.System.Files;

public static class SystemFilesServiceCollectionExtensions
{
    public static IServiceCollection AddWeCmsSystemFiles(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddScoped<IFileService, FileService>();
        return services;
    }
}
