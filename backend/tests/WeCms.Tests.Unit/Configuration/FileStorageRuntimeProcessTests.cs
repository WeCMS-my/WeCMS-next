using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using WeCms.Api.Configuration;
using WeCms.Api.Extensions;
using WeCms.Shared;

namespace WeCms.Tests.Unit.Configuration;

[CollectionDefinition("FileStorageRuntimeProcessCollection", DisableParallelization = true)]
public sealed class FileStorageRuntimeProcessCollection;

[Collection("FileStorageRuntimeProcessCollection")]
public sealed class FileStorageRuntimeProcessTests
{
    [Fact]
    public void ApiHostFailsToStartInProductionWhenFileStorageBasePathMissing()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            ProductionConfigurationValidator.Validate(
                BuildConfiguration(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Default"] = "server=127.0.0.1;database=wecms_integration;uid=wecms_dev;pwd=wecms_dev;SslMode=None;",
                    ["Auth:AccessTokenSecret"] = new string('a', 32),
                    ["Security:TwoFactor:SecretProtectionKey"] = new string('b', 32),
                    ["Security:AllowedOrigins:0"] = "https://admin.example.com",
                    ["Security:SecureHeaders:CspReportOnly"] = "default-src 'none'; object-src 'none'; frame-ancestors 'none'; base-uri 'self';",
                    ["FileStorage:Provider"] = "local",
                    ["Database:SeedAdminPassword"] = "SeedPassword!12345A",
                    ["FileStorage:VirusScanEnabled"] = "false"
                }),
                ProductionEnvironment()));

        Assert.Equal("FileStorage:Local:BasePath must be configured for Production.", exception.Message);
    }

    [Fact]
    public async Task ApiHostStartsInProductionWhenFileStorageBasePathIsConfigured()
    {
        var storagePath = Path.Combine(Path.GetTempPath(), "wecms-integration-storage-base", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(storagePath);

        try
        {
            var services = new ServiceCollection();
            services.AddWeCmsFileStorage(
                BuildConfiguration(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Default"] = "server=127.0.0.1;database=wecms_integration;uid=wecms_dev;pwd=wecms_dev;SslMode=None;",
                    ["Auth:AccessTokenSecret"] = new string('a', 32),
                    ["Security:TwoFactor:SecretProtectionKey"] = new string('b', 32),
                    ["Security:AllowedOrigins:0"] = "https://admin.example.com",
                    ["FileStorage:Provider"] = "local",
                    ["FileStorage:Local:BasePath"] = storagePath,
                    ["Database:SeedAdminPassword"] = "SeedPassword!12345A",
                    ["FileStorage:VirusScanEnabled"] = "false"
                }),
                ProductionEnvironment());

            await using var provider = services.BuildServiceProvider();
            var storage = provider.GetRequiredService<IFileStorage>();

            await storage.StoreAsync(new MemoryStream(Encoding.UTF8.GetBytes("ok")), "healthcheck.txt", ".txt", 2, CancellationToken.None);

            Assert.True(File.Exists(Path.Combine(storagePath, "healthcheck.txt")));
        }
        finally
        {
            if (Directory.Exists(storagePath))
            {
                Directory.Delete(storagePath, true);
            }
        }
    }

    private static IConfiguration BuildConfiguration(IReadOnlyDictionary<string, string?> values)
    {
        return new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    }

    private static IWebHostEnvironment ProductionEnvironment()
    {
        return new FakeHostEnvironment("Production");
    }

    private sealed class FakeHostEnvironment : IWebHostEnvironment
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
