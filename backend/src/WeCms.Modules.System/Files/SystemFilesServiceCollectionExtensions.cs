using Microsoft.Extensions.DependencyInjection;
using WeCms.Shared;

namespace WeCms.Modules.System.Files;

public static class SystemFilesServiceCollectionExtensions
{
    public static IServiceCollection AddWeCmsSystemFiles(this IServiceCollection services, Func<IServiceProvider, IFileScanService>? fileScanServiceFactory = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        if (fileScanServiceFactory is null)
        {
            services.AddSingleton<IFileScanService, NoopFileScanService>();
        }
        else
        {
            services.AddSingleton<IFileScanService>(fileScanServiceFactory);
        }

        services.AddSingleton<IFileUploadPolicy, AvatarUploadPolicy>();
        services.AddSingleton<IFileUploadPolicy, ImageUploadPolicy>();
        services.AddSingleton<IFileUploadPolicy, DocumentUploadPolicy>();
        services.AddSingleton<IFileUploadPolicyResolver, FileUploadPolicyResolver>();
        services.AddSingleton<IFileObjectKeyGenerator, FileObjectKeyGenerator>();
        services.AddScoped<IFileService, FileService>();
        return services;
    }
}
