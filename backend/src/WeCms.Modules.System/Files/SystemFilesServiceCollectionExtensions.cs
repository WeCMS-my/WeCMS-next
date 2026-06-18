using Microsoft.Extensions.DependencyInjection;

namespace WeCms.Modules.System.Files;

public static class SystemFilesServiceCollectionExtensions
{
    public static IServiceCollection AddWeCmsSystemFiles(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddSingleton<IFileUploadPolicy, AvatarUploadPolicy>();
        services.AddSingleton<IFileUploadPolicy, ImageUploadPolicy>();
        services.AddSingleton<IFileUploadPolicy, DocumentUploadPolicy>();
        services.AddSingleton<IFileUploadPolicyResolver, FileUploadPolicyResolver>();
        services.AddSingleton<IFileObjectKeyGenerator, FileObjectKeyGenerator>();
        services.AddScoped<IFileService, FileService>();
        return services;
    }
}
