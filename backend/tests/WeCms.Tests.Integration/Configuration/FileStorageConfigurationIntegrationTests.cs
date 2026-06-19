using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using WeCms.Api.Configuration;
using WeCms.Api.Extensions;
using WeCms.Shared;

namespace WeCms.Tests.Integration.Configuration;

public sealed class FileStorageConfigurationIntegrationTests
{
    [DbFact]
    public void Production_FileStorageBasePath_MustBeConfigured()
    {
        var config = BuildConfiguration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] = ValidConnectionString(),
            ["Auth:AccessTokenSecret"] = new string('a', 32),
            ["Security:TwoFactor:SecretProtectionKey"] = new string('b', 32),
            ["Security:AllowedOrigins:0"] = "https://admin.example.com",
            ["FileStorage:Provider"] = "local",
            ["FileStorage:VirusScanEnabled"] = "false",
            ["Database:SeedAdminPassword"] = "SeedPassword!12345A"
        });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            ProductionConfigurationValidator.Validate(config, ProductionEnvironment()));

        Assert.Equal("FileStorage:Local:BasePath must be configured for Production.", exception.Message);
    }

    [DbFact]
    public async Task Production_ConfiguredBasePath_IsUsedByFileStorageRegistration()
    {
        var storagePath = Path.Combine(Path.GetTempPath(), "wecms-integration-filestorage", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(storagePath);

        try
        {
            var config = BuildConfiguration(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = ValidConnectionString(),
                ["Auth:AccessTokenSecret"] = new string('a', 32),
                ["Security:TwoFactor:SecretProtectionKey"] = new string('b', 32),
                ["Security:AllowedOrigins:0"] = "https://admin.example.com",
                ["FileStorage:Provider"] = "local",
                ["FileStorage:Local:BasePath"] = storagePath,
                ["FileStorage:VirusScanEnabled"] = "false",
                ["Database:SeedAdminPassword"] = "SeedPassword!12345A"
            });

            var services = new ServiceCollection();
            services.AddWeCmsFileStorage(config, ProductionEnvironment());

            await using var provider = services.BuildServiceProvider();
            var storage = provider.GetRequiredService<IFileStorage>();

            await storage.StoreAsync(
                new MemoryStream(Encoding.UTF8.GetBytes("ok")),
                "integration/healthcheck.txt",
                ".txt",
                1024,
                CancellationToken.None);

            Assert.True(File.Exists(Path.Combine(storagePath, "integration", "healthcheck.txt")));
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

    private static string ValidConnectionString()
    {
        return "server=127.0.0.1;database=wecms_dev;uid=wecms_dev;pwd=wecms_dev;SslMode=None;";
    }

    private sealed class FakeHostEnvironment : IWebHostEnvironment
    {
        public FakeHostEnvironment(string environmentName)
        {
            EnvironmentName = environmentName;
        }

        public string EnvironmentName { get; set; }

        public string ApplicationName { get; set; } = "WeCms.Tests.Integration";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();

        public string WebRootPath { get; set; } = string.Empty;

        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}
