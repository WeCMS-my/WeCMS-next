using Microsoft.Extensions.DependencyInjection;
using WeCms.Modules.FileCenter.Files;
using WeCms.Shared;

namespace WeCms.Modules.FileCenter;

public static class FileCenterServiceCollectionExtensions
{
    public static IServiceCollection AddWeCmsFileCenter(this IServiceCollection services, Func<IServiceProvider, IFileScanService>? fileScanServiceFactory = null)
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

        services.AddSingleton<IFileCenterClock, SystemFileCenterClock>();
        services.AddSingleton<IFileUploadPolicy, AvatarUploadPolicy>();
        services.AddSingleton<IFileUploadPolicy, ImageUploadPolicy>();
        services.AddSingleton<IFileUploadPolicy, DocumentUploadPolicy>();
        services.AddSingleton<IFileUploadPolicyResolver, FileUploadPolicyResolver>();
        services.AddSingleton<IFileObjectKeyGenerator, FileObjectKeyGenerator>();
        services.AddScoped<IFileService, FileService>();
        return services;
    }
}
