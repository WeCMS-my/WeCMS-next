using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WeCms.Infrastructure.Files;
using WeCms.Shared;

namespace WeCms.Api.Extensions;

public static class FileStorageExtensions
{
    public static IServiceCollection AddWeCmsFileStorage(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        var provider = configuration["FileStorage:Provider"] ?? "local";
        if (!string.Equals(provider, "local", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("FileStorage:Provider must be local.");
        }

        var basePath = configuration["FileStorage:Local:BasePath"];
        if (environment.IsProduction() && string.IsNullOrWhiteSpace(basePath))
        {
            throw new InvalidOperationException("FileStorage:Local:BasePath is required in Production.");
        }

        var resolvedBasePath = string.IsNullOrWhiteSpace(basePath)
            ? Path.Combine(environment.ContentRootPath, "storage", "files")
            : basePath;

        services.AddScoped<IFileStorage>(_ => new LocalFileStorage(resolvedBasePath));
        return services;
    }
}
