using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using WeCms.Api.Configuration;

namespace WeCms.Tests.Unit.Configuration;

public sealed class ProductionConfigurationValidatorTests
{
    [Fact]
    public void Validate_ProductionThrowsWhenConnectionStringIsMissing()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => ProductionConfigurationValidator.Validate(Configuration(new Dictionary<string, string?>
            {
                ["Auth:AccessTokenSecret"] = ValidSecret(),
                ["Security:TwoFactor:SecretProtectionKey"] = ValidSecret(),
                ["Security:AllowedOrigins:0"] = "https://admin.example.com",
                ["Database:SeedAdminPassword"] = ValidSeedPassword()
            }), Environment("Production")));

        Assert.Equal("ConnectionStrings:Default must be configured for Production.", exception.Message);
    }

    [Fact]
    public void Validate_ProductionThrowsWhenConnectionStringContainsPlaceholder()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => ProductionConfigurationValidator.Validate(Configuration(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "server=mysql.internal;database=wecms;uid=wecms_app;pwd=__SET_BY_ENV__;charset=utf8mb4;",
                ["Auth:AccessTokenSecret"] = ValidSecret(),
                ["Security:TwoFactor:SecretProtectionKey"] = ValidSecret(),
                ["Security:AllowedOrigins:0"] = "https://admin.example.com",
                ["Database:SeedAdminPassword"] = ValidSeedPassword()
            }), Environment("Production")));

        Assert.Equal("ConnectionStrings:Default must be configured for Production.", exception.Message);
    }

    [Fact]
    public void Validate_ProductionThrowsWhenAuthSecretIsMissing()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => ProductionConfigurationValidator.Validate(Configuration(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = ValidConnectionString(),
                ["Security:TwoFactor:SecretProtectionKey"] = ValidSecret(),
                ["Security:AllowedOrigins:0"] = "https://admin.example.com",
                ["Database:SeedAdminPassword"] = ValidSeedPassword()
            }), Environment("Production")));

        Assert.Equal("Auth:AccessTokenSecret must be configured for Production.", exception.Message);
    }

    [Fact]
    public void Validate_ProductionThrowsWhenTwoFactorKeyIsMissing()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => ProductionConfigurationValidator.Validate(Configuration(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = ValidConnectionString(),
                ["Auth:AccessTokenSecret"] = ValidSecret(),
                ["Security:AllowedOrigins:0"] = "https://admin.example.com",
                ["Database:SeedAdminPassword"] = ValidSeedPassword()
            }), Environment("Production")));

        Assert.Equal("Security:TwoFactor:SecretProtectionKey must be configured for Production.", exception.Message);
    }

    [Fact]
    public void Validate_ProductionThrowsWhenSeedPasswordUsesDevelopmentDefault()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => ProductionConfigurationValidator.Validate(Configuration(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = ValidConnectionString(),
                ["Auth:AccessTokenSecret"] = ValidSecret(),
                ["Security:TwoFactor:SecretProtectionKey"] = ValidSecret(),
                ["Security:AllowedOrigins:0"] = "https://admin.example.com",
                ["Database:SeedAdminPassword"] = "Admin@123"
            }), Environment("Production")));

        Assert.Equal("Database:SeedAdminPassword must not use the Development default in Production.", exception.Message);
    }

    [Fact]
    public void Validate_DevelopmentAllowsMissingProductionRequiredValues()
    {
        ProductionConfigurationValidator.Validate(Configuration(new Dictionary<string, string?>()), Environment("Development"));
    }

    [Fact]
    public void Validate_ProductionRejectsMissingFileStorageBasePath()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => ProductionConfigurationValidator.Validate(Configuration(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = ValidConnectionString(),
                ["Auth:AccessTokenSecret"] = ValidSecret(),
                ["Security:TwoFactor:SecretProtectionKey"] = ValidSecret(),
                ["Security:AllowedOrigins:0"] = "https://admin.example.com",
                ["FileStorage:Local:BasePath"] = "",
                ["Database:SeedAdminPassword"] = ValidSeedPassword()
            }), Environment("Production")));

        Assert.Equal("FileStorage:Local:BasePath must be configured for Production.", exception.Message);
    }

    [Fact]
    public void Validate_ProductionRejectsRelativeFileStorageBasePath()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => ProductionConfigurationValidator.Validate(Configuration(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = ValidConnectionString(),
                ["Auth:AccessTokenSecret"] = ValidSecret(),
                ["Security:TwoFactor:SecretProtectionKey"] = ValidSecret(),
                ["Security:AllowedOrigins:0"] = "https://admin.example.com",
                ["FileStorage:Local:BasePath"] = "storage/files",
                ["Database:SeedAdminPassword"] = ValidSeedPassword()
            }), Environment("Production")));

        Assert.Equal("FileStorage:Local:BasePath must be an absolute path in Production.", exception.Message);
    }

    [Fact]
    public void Validate_ProductionRejectsNegativeUploadRetryDelay()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => ProductionConfigurationValidator.Validate(Configuration(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = ValidConnectionString(),
                ["Auth:AccessTokenSecret"] = ValidSecret(),
                ["Security:TwoFactor:SecretProtectionKey"] = ValidSecret(),
                ["Security:AllowedOrigins:0"] = "https://admin.example.com",
                ["Database:SeedAdminPassword"] = ValidSeedPassword(),
                ["FileStorage:Upload:RetryDelayMilliseconds"] = "-1"
            }), Environment("Production")));

        Assert.Equal("FileStorage:Upload:RetryDelayMilliseconds must be an integer between 0 and 60000.", exception.Message);
    }

    [Fact]
    public void Validate_ProductionRejectsNegativeUploadTempFileRetentionHours()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => ProductionConfigurationValidator.Validate(Configuration(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = ValidConnectionString(),
                ["Auth:AccessTokenSecret"] = ValidSecret(),
                ["Security:TwoFactor:SecretProtectionKey"] = ValidSecret(),
                ["Security:AllowedOrigins:0"] = "https://admin.example.com",
                ["Database:SeedAdminPassword"] = ValidSeedPassword(),
                ["FileStorage:Upload:TempFileRetentionHours"] = "0"
            }), Environment("Production")));

        Assert.Equal("FileStorage:Upload:TempFileRetentionHours must be an integer between 1 and 720.", exception.Message);
    }

    [Fact]
    public void Validate_ProductionRejectsZeroLargeFileUploadConcurrency()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => ProductionConfigurationValidator.Validate(Configuration(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = ValidConnectionString(),
                ["Auth:AccessTokenSecret"] = ValidSecret(),
                ["Security:TwoFactor:SecretProtectionKey"] = ValidSecret(),
                ["Security:AllowedOrigins:0"] = "https://admin.example.com",
                ["Database:SeedAdminPassword"] = ValidSeedPassword(),
                ["FileStorage:Upload:MaxConcurrentLargeFileUploads"] = "0"
            }), Environment("Production")));

        Assert.Equal("FileStorage:Upload:MaxConcurrentLargeFileUploads must be an integer between 1 and 128.", exception.Message);
    }

    [Fact]
    public void Validate_ProductionRejectsRelativeUploadTempPath()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => ProductionConfigurationValidator.Validate(Configuration(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = ValidConnectionString(),
                ["Auth:AccessTokenSecret"] = ValidSecret(),
                ["Security:TwoFactor:SecretProtectionKey"] = ValidSecret(),
                ["Security:AllowedOrigins:0"] = "https://admin.example.com",
                ["Database:SeedAdminPassword"] = ValidSeedPassword(),
                ["FileStorage:Upload:TempFilePath"] = "relative\\wecms\\upload"
            }), Environment("Production")));

        Assert.Equal("FileStorage:Upload:TempFilePath must be an absolute path in Production.", exception.Message);
    }

    [Fact]
    public void Validate_ProductionRejectsMissingFileStorageDirectory()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => ProductionConfigurationValidator.Validate(Configuration(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = ValidConnectionString(),
                ["Auth:AccessTokenSecret"] = ValidSecret(),
                ["Security:TwoFactor:SecretProtectionKey"] = ValidSecret(),
                ["Security:AllowedOrigins:0"] = "https://admin.example.com",
                ["FileStorage:Local:BasePath"] = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")),
                ["Database:SeedAdminPassword"] = ValidSeedPassword()
            }), Environment("Production")));

        Assert.Equal("FileStorage:Local:BasePath must exist in Production.", exception.Message);
    }

    [Fact]
    public void Validate_ProductionRejectsFileStorageUnderWebRoot()
    {
        var environment = Environment("Production");
        var webRootStorage = Path.Combine(environment.ContentRootPath, "wwwroot", "uploads");
        Directory.CreateDirectory(webRootStorage);

        var exception = Assert.Throws<InvalidOperationException>(
            () => ProductionConfigurationValidator.Validate(Configuration(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = ValidConnectionString(),
                ["Auth:AccessTokenSecret"] = ValidSecret(),
                ["Security:TwoFactor:SecretProtectionKey"] = ValidSecret(),
                ["Security:AllowedOrigins:0"] = "https://admin.example.com",
                ["FileStorage:Local:BasePath"] = webRootStorage,
                ["Database:SeedAdminPassword"] = ValidSeedPassword()
            }), environment));

        Assert.Equal("FileStorage:Local:BasePath must not be under the web root in Production.", exception.Message);
    }

    [Fact]
    public void Validate_ProductionRejectsFileStorageDirectoryWithoutWritePermission()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        var path = Path.Combine(Path.GetTempPath(), "wecms-ph4-unwritable", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        var directory = new DirectoryInfo(path);

        try
        {
            directory.UnixFileMode = UnixFileMode.UserRead | UnixFileMode.UserExecute |
                UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                UnixFileMode.OtherRead | UnixFileMode.OtherExecute;

            var exception = Assert.Throws<InvalidOperationException>(
                () => ProductionConfigurationValidator.Validate(Configuration(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Default"] = ValidConnectionString(),
                    ["Auth:AccessTokenSecret"] = ValidSecret(),
                    ["Security:TwoFactor:SecretProtectionKey"] = ValidSecret(),
                    ["Security:AllowedOrigins:0"] = "https://admin.example.com",
                    ["FileStorage:Local:BasePath"] = path,
                    ["FileStorage:VirusScanEnabled"] = "true",
                    ["FileStorage:VirusScan:Provider"] = "clamav-tcp",
                    ["FileStorage:VirusScan:Host"] = "scanner.internal",
                    ["FileStorage:Provider"] = "local",
                    ["Database:SeedAdminPassword"] = ValidSeedPassword()
                }), Environment("Production")));

            Assert.Equal("FileStorage:Local:BasePath must be writable in Production.", exception.Message);
        }
        finally
        {
            directory.UnixFileMode = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute |
                UnixFileMode.OtherRead | UnixFileMode.OtherExecute;
            Directory.Delete(path, true);
        }
    }

    [Fact]
    public void Validate_ProductionRejectsVirusScanEnabledWithNoopScanner()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => ProductionConfigurationValidator.Validate(Configuration(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = ValidConnectionString(),
                ["Auth:AccessTokenSecret"] = ValidSecret(),
                ["Security:TwoFactor:SecretProtectionKey"] = ValidSecret(),
                ["Security:AllowedOrigins:0"] = "https://admin.example.com",
                ["FileStorage:VirusScanEnabled"] = "true",
                ["FileStorage:VirusScan:Provider"] = "none",
                ["Database:SeedAdminPassword"] = ValidSeedPassword()
            }), Environment("Production")));

        Assert.Equal("FileStorage:VirusScan:Provider must be clamav-tcp when virus scanning is enabled in Production.", exception.Message);
    }

    [Fact]
    public void Validate_ProductionAllowsVirusScanEnabledWithClamAvProvider()
    {
        ProductionConfigurationValidator.Validate(Configuration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] = ValidConnectionString(),
            ["Auth:AccessTokenSecret"] = ValidSecret(),
            ["Security:TwoFactor:SecretProtectionKey"] = ValidSecret(),
            ["Security:AllowedOrigins:0"] = "https://admin.example.com",
            ["FileStorage:VirusScanEnabled"] = "true",
            ["FileStorage:VirusScan:Provider"] = "clamav-tcp",
            ["FileStorage:VirusScan:Host"] = "scanner.internal",
            ["FileStorage:VirusScan:Port"] = "3310",
            ["FileStorage:VirusScan:TimeoutSeconds"] = "5",
            ["Database:SeedAdminPassword"] = ValidSeedPassword()
        }), Environment("Production"));
    }

    [Fact]
    public void Validate_ProductionRejectsVirusScanEnabledWithClamAvProviderMissingHost()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => ProductionConfigurationValidator.Validate(Configuration(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = ValidConnectionString(),
                ["Auth:AccessTokenSecret"] = ValidSecret(),
                ["Security:TwoFactor:SecretProtectionKey"] = ValidSecret(),
                ["Security:AllowedOrigins:0"] = "https://admin.example.com",
                ["FileStorage:VirusScanEnabled"] = "true",
                ["FileStorage:VirusScan:Provider"] = "clamav-tcp",
                ["FileStorage:VirusScan:Host"] = "",
                ["FileStorage:VirusScan:Port"] = "3310",
                ["FileStorage:VirusScan:TimeoutSeconds"] = "5",
                ["Database:SeedAdminPassword"] = ValidSeedPassword()
            }), Environment("Production")));

        Assert.Equal("FileStorage:VirusScan:Host must be configured when virus scanning is enabled in Production.", exception.Message);
    }

    [Fact]
    public void Validate_ProductionRejectsDisabledVirusScan()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => ProductionConfigurationValidator.Validate(Configuration(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = ValidConnectionString(),
                ["Auth:AccessTokenSecret"] = ValidSecret(),
                ["Security:TwoFactor:SecretProtectionKey"] = ValidSecret(),
                ["Security:AllowedOrigins:0"] = "https://admin.example.com",
                ["FileStorage:VirusScanEnabled"] = "false",
                ["Database:SeedAdminPassword"] = ValidSeedPassword()
            }), Environment("Production")));

        Assert.Equal("FileStorage:VirusScanEnabled must be true in Production.", exception.Message);
    }

    [Fact]
    public void Validate_ProductionRejectsVirusScanEnabledWithClamAvProviderPlaceholderHost()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => ProductionConfigurationValidator.Validate(Configuration(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = ValidConnectionString(),
                ["Auth:AccessTokenSecret"] = ValidSecret(),
                ["Security:TwoFactor:SecretProtectionKey"] = ValidSecret(),
                ["Security:AllowedOrigins:0"] = "https://admin.example.com",
                ["FileStorage:VirusScanEnabled"] = "true",
                ["FileStorage:VirusScan:Provider"] = "clamav-tcp",
                ["FileStorage:VirusScan:Host"] = "__SET_BY_ENV__",
                ["FileStorage:VirusScan:Port"] = "3310",
                ["FileStorage:VirusScan:TimeoutSeconds"] = "5",
                ["Database:SeedAdminPassword"] = ValidSeedPassword()
            }), Environment("Production")));

        Assert.Equal("FileStorage:VirusScan:Host must be configured when virus scanning is enabled in Production.", exception.Message);
    }

    [Fact]
    public void Validate_ProductionRejectsMissingAllowedOrigins()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => ProductionConfigurationValidator.Validate(Configuration(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = ValidConnectionString(),
                ["Auth:AccessTokenSecret"] = ValidSecret(),
                ["Security:TwoFactor:SecretProtectionKey"] = ValidSecret(),
                ["Database:SeedAdminPassword"] = ValidSeedPassword()
            }), Environment("Production")));

        Assert.Equal("Security:AllowedOrigins must contain at least one origin for Production.", exception.Message);
    }

    [Fact]
    public void Validate_ProductionRejectsWildcardAllowedOrigins()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => ProductionConfigurationValidator.Validate(Configuration(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = ValidConnectionString(),
                ["Auth:AccessTokenSecret"] = ValidSecret(),
                ["Security:TwoFactor:SecretProtectionKey"] = ValidSecret(),
                ["Security:AllowedOrigins:0"] = "*",
                ["Database:SeedAdminPassword"] = ValidSeedPassword()
            }), Environment("Production")));

        Assert.Equal("Security:AllowedOrigins must not contain wildcard origins in Production.", exception.Message);
    }

    [Fact]
    public void Validate_ProductionRejectsHttpAllowedOrigins()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => ProductionConfigurationValidator.Validate(Configuration(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = ValidConnectionString(),
                ["Auth:AccessTokenSecret"] = ValidSecret(),
                ["Security:TwoFactor:SecretProtectionKey"] = ValidSecret(),
                ["Security:AllowedOrigins:0"] = "http://admin.example.com",
                ["Database:SeedAdminPassword"] = ValidSeedPassword()
            }), Environment("Production")));

        Assert.Equal("Security:AllowedOrigins must use HTTPS origins in Production.", exception.Message);
    }

    [Fact]
    public void Validate_ProductionRejectsPlaceholderSecrets()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => ProductionConfigurationValidator.Validate(Configuration(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = ValidConnectionString(),
                ["Auth:AccessTokenSecret"] = "__SET_BY_ENV__",
                ["Security:TwoFactor:SecretProtectionKey"] = ValidSecret(),
                ["Security:AllowedOrigins:0"] = "https://admin.example.com",
                ["Database:SeedAdminPassword"] = ValidSeedPassword()
            }), Environment("Production")));

        Assert.Equal("Auth:AccessTokenSecret must be configured for Production.", exception.Message);
    }

    [Fact]
    public void Validate_ProductionRejectsLocalhostOrigins()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => ProductionConfigurationValidator.Validate(Configuration(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = ValidConnectionString(),
                ["Auth:AccessTokenSecret"] = ValidSecret(),
                ["Security:TwoFactor:SecretProtectionKey"] = ValidSecret(),
                ["Security:AllowedOrigins:0"] = "https://localhost:5173",
                ["Database:SeedAdminPassword"] = ValidSeedPassword()
            }), Environment("Production")));

        Assert.Equal("Security:AllowedOrigins must not contain localhost origins in Production.", exception.Message);
    }

    [Fact]
    public void Validate_ProductionRejectsOriginWithPath()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => ProductionConfigurationValidator.Validate(Configuration(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = ValidConnectionString(),
                ["Auth:AccessTokenSecret"] = ValidSecret(),
                ["Security:TwoFactor:SecretProtectionKey"] = ValidSecret(),
                ["Security:AllowedOrigins:0"] = "https://admin.example.com/app",
                ["Database:SeedAdminPassword"] = ValidSeedPassword()
            }), Environment("Production")));

        Assert.Equal("Security:AllowedOrigins must contain origins only, without path, query, or fragment.", exception.Message);
    }

    [Fact]
    public void Validate_ProductionRejectsForwardedHeadersEnabledWithoutKnownProxy()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => ProductionConfigurationValidator.Validate(Configuration(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = ValidConnectionString(),
                ["Auth:AccessTokenSecret"] = ValidSecret(),
                ["Security:TwoFactor:SecretProtectionKey"] = ValidSecret(),
                ["Security:AllowedOrigins:0"] = "https://admin.example.com",
                ["Security:ForwardedHeaders:Enabled"] = "true",
                ["Database:SeedAdminPassword"] = ValidSeedPassword()
            }), Environment("Production")));

        Assert.Equal("Security:ForwardedHeaders requires KnownProxies or KnownNetworks when enabled in Production.", exception.Message);
    }

    [Fact]
    public void Validate_ProductionRejectsInvalidKnownProxy()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => ProductionConfigurationValidator.Validate(Configuration(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = ValidConnectionString(),
                ["Auth:AccessTokenSecret"] = ValidSecret(),
                ["Security:TwoFactor:SecretProtectionKey"] = ValidSecret(),
                ["Security:AllowedOrigins:0"] = "https://admin.example.com",
                ["Security:ForwardedHeaders:Enabled"] = "true",
                ["Security:ForwardedHeaders:KnownProxies:0"] = "not-an-ip",
                ["Database:SeedAdminPassword"] = ValidSeedPassword()
            }), Environment("Production")));

        Assert.Equal("Security:ForwardedHeaders:KnownProxies contains invalid IP address 'not-an-ip'.", exception.Message);
    }

    [Fact]
    public void Validate_ProductionRejectsInvalidForwardedHeaderForwardLimit()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => ProductionConfigurationValidator.Validate(Configuration(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = ValidConnectionString(),
                ["Auth:AccessTokenSecret"] = ValidSecret(),
                ["Security:TwoFactor:SecretProtectionKey"] = ValidSecret(),
                ["Security:AllowedOrigins:0"] = "https://admin.example.com",
                ["Security:ForwardedHeaders:Enabled"] = "true",
                ["Security:ForwardedHeaders:KnownProxies:0"] = "10.0.0.10",
                ["Security:ForwardedHeaders:ForwardLimit"] = "0",
                ["Database:SeedAdminPassword"] = ValidSeedPassword()
            }), Environment("Production")));

        Assert.Equal("Security:ForwardedHeaders:ForwardLimit must be an integer between 1 and 32.", exception.Message);
    }

    [Fact]
    public void Validate_ProductionAllowsForwardedHeadersWithForwardLimit()
    {
        ProductionConfigurationValidator.Validate(Configuration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] = ValidConnectionString(),
            ["Auth:AccessTokenSecret"] = ValidSecret(),
            ["Security:TwoFactor:SecretProtectionKey"] = ValidSecret(),
            ["Security:AllowedOrigins:0"] = "https://admin.example.com",
            ["Security:ForwardedHeaders:Enabled"] = "true",
            ["Security:ForwardedHeaders:KnownNetworks:0"] = "10.0.0.0/24",
            ["Security:ForwardedHeaders:ForwardLimit"] = "2",
            ["Database:SeedAdminPassword"] = ValidSeedPassword()
        }), Environment("Production"));
    }

    [Fact]
    public void Validate_ProductionRejectsCspMissingObjectSrcNone()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => ProductionConfigurationValidator.Validate(Configuration(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = ValidConnectionString(),
                ["Auth:AccessTokenSecret"] = ValidSecret(),
                ["Security:TwoFactor:SecretProtectionKey"] = ValidSecret(),
                ["Security:AllowedOrigins:0"] = "https://admin.example.com",
                ["Security:SecureHeaders:CspReportOnly"] = "default-src 'self'; frame-ancestors 'none'",
                ["Database:SeedAdminPassword"] = ValidSeedPassword()
            }), Environment("Production")));

        Assert.Equal("Security:SecureHeaders:CspReportOnly must include object-src 'none' in Production.", exception.Message);
    }

    [Fact]
    public void Validate_ProductionRejectsReportOnlyCspWithoutEnforceCsp()
    {
        var exception = Assert.Throws<InvalidOperationException>(
            () => ProductionConfigurationValidator.Validate(Configuration(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = ValidConnectionString(),
                ["Auth:AccessTokenSecret"] = ValidSecret(),
                ["Security:TwoFactor:SecretProtectionKey"] = ValidSecret(),
                ["Security:AllowedOrigins:0"] = "https://admin.example.com",
                ["Security:SecureHeaders:CspEnabled"] = "false",
                ["Security:SecureHeaders:CspReportOnlyEnabled"] = "true",
                ["Database:SeedAdminPassword"] = ValidSeedPassword()
            }), Environment("Production")));

        Assert.Equal("Security:SecureHeaders:CspEnabled must be true in Production.", exception.Message);
    }

    [Fact]
    public void Validate_ProductionAllowsForwardedHeadersWithKnownNetwork()
    {
        ProductionConfigurationValidator.Validate(Configuration(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Default"] = ValidConnectionString(),
            ["Auth:AccessTokenSecret"] = ValidSecret(),
            ["Security:TwoFactor:SecretProtectionKey"] = ValidSecret(),
            ["Security:AllowedOrigins:0"] = "https://admin.example.com",
            ["Security:ForwardedHeaders:Enabled"] = "true",
            ["Security:ForwardedHeaders:KnownNetworks:0"] = "10.0.0.0/24",
            ["Database:SeedAdminPassword"] = ValidSeedPassword()
        }), Environment("Production"));
    }

    private static IConfiguration Configuration(IReadOnlyDictionary<string, string?> values)
    {
        var merged = new Dictionary<string, string?>
        {
            ["Security:SecureHeaders:CspReportOnlyEnabled"] = "true",
            ["Security:SecureHeaders:CspEnabled"] = "true",
            ["Security:SecureHeaders:Csp"] = "default-src 'self'; object-src 'none'; base-uri 'self'; frame-ancestors 'none'",
            ["Security:SecureHeaders:CspReportOnly"] = "default-src 'self'; object-src 'none'; base-uri 'self'; frame-ancestors 'none'",
            ["FileStorage:Provider"] = "local",
            ["FileStorage:Local:BasePath"] = ValidStorageBasePath(),
            ["FileStorage:VirusScanEnabled"] = "true",
            ["FileStorage:VirusScan:Provider"] = "clamav-tcp",
            ["FileStorage:VirusScan:Host"] = "scanner.internal",
            ["FileStorage:VirusScan:Port"] = "3310",
            ["FileStorage:VirusScan:TimeoutSeconds"] = "5"
        };

        foreach (var (key, value) in values)
        {
            merged[key] = value;
        }

        return new ConfigurationBuilder()
            .AddInMemoryCollection(merged)
            .Build();
    }

    private static string ValidConnectionString()
    {
        return "server=mysql.internal;port=3306;database=wecms;uid=wecms_app;pwd=not-a-real-password;charset=utf8mb4;";
    }

    private static string ValidSecret()
    {
        return "unit-test-secret-with-more-than-32-characters";
    }

    private static string ValidSeedPassword()
    {
        return "UnitTest!SeedPassword2026";
    }

    private static string ValidStorageBasePath()
    {
        var path = Path.Combine(Path.GetTempPath(), "wecms-ph4-file-storage-validator");
        Directory.CreateDirectory(path);
        return path;
    }

    private static IHostEnvironment Environment(string environmentName)
    {
        return new FakeHostEnvironment(environmentName);
    }

    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public FakeHostEnvironment(string environmentName)
        {
            EnvironmentName = environmentName;
        }

        public string EnvironmentName { get; set; }

        public string ApplicationName { get; set; } = "WeCms.Tests";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
