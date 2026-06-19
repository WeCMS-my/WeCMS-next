using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using System.Text;
using WeCms.Api.Extensions;
using WeCms.Shared;

namespace WeCms.Tests.Unit.Configuration;

public sealed class FileStorageExtensionsTests
{
    [Fact]
    public async Task AddWeCmsFileStorage_UsesConfiguredBasePathInProduction()
    {
        var basePath = Path.Combine(Path.GetTempPath(), "wecms-storage-configured", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(basePath);

        try
        {
            var services = new ServiceCollection();
            services.AddWeCmsFileStorage(
                BuildConfiguration(new Dictionary<string, string?>
                {
                    ["FileStorage:Local:BasePath"] = basePath
                }),
                ProductionEnvironment());

            await using var provider = services.BuildServiceProvider();
            var storage = provider.GetRequiredService<IFileStorage>();

            await storage.StoreAsync(new MemoryStream(Encoding.UTF8.GetBytes("ok")), "avatars/configured.txt", ".txt", 1024, CancellationToken.None);

            Assert.True(File.Exists(Path.Combine(basePath, "avatars", "configured.txt")));
        }
        finally
        {
            Directory.Delete(basePath, true);
        }
    }

    [Fact]
    public async Task AddWeCmsFileStorage_UsesDefaultBasePathInDevelopment()
    {
        var contentRootPath = Path.Combine(Path.GetTempPath(), "wecms-storage-default", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(contentRootPath);
        var storagePath = Path.Combine(contentRootPath, "storage", "files");

        try
        {
            var services = new ServiceCollection();
            services.AddWeCmsFileStorage(
                BuildConfiguration(new Dictionary<string, string?>()),
                DevelopmentEnvironment(contentRootPath));

            await using var provider = services.BuildServiceProvider();
            var storage = provider.GetRequiredService<IFileStorage>();

            await storage.StoreAsync(new MemoryStream(Encoding.UTF8.GetBytes("ok")), "avatars/default.txt", ".txt", 1024, CancellationToken.None);

            Assert.True(File.Exists(Path.Combine(storagePath, "avatars", "default.txt")));
        }
        finally
        {
            if (Directory.Exists(storagePath))
            {
                Directory.Delete(storagePath, true);
            }

            Directory.Delete(contentRootPath, true);
        }
    }

    [Fact]
    public void AddWeCmsFileStorage_ThrowsInProductionWhenBasePathIsMissing()
    {
        var services = new ServiceCollection();

        var exception = Assert.Throws<InvalidOperationException>(() =>
            services.AddWeCmsFileStorage(
                BuildConfiguration(new Dictionary<string, string?>
                {
                    ["FileStorage:Provider"] = "local"
                }),
                ProductionEnvironment()));

        Assert.Equal("FileStorage:Local:BasePath is required in Production.", exception.Message);
    }

    private static IConfiguration BuildConfiguration(IReadOnlyDictionary<string, string?> values)
    {
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private static Microsoft.AspNetCore.Hosting.IWebHostEnvironment ProductionEnvironment()
    {
        return new FakeHostEnvironment("Production");
    }

    private static Microsoft.AspNetCore.Hosting.IWebHostEnvironment DevelopmentEnvironment(string contentRootPath)
    {
        return new FakeHostEnvironment("Development")
        {
            ContentRootPath = contentRootPath
        };
    }

    private sealed class FakeHostEnvironment : Microsoft.AspNetCore.Hosting.IWebHostEnvironment
    {
        public FakeHostEnvironment(string environmentName)
        {
            EnvironmentName = environmentName;
        }

        public string EnvironmentName { get; set; }

        public string ApplicationName { get; set; } = "WeCms.Tests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();

        public string WebRootPath { get; set; } = string.Empty;

        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}
