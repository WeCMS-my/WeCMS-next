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
            ["Security:SecureHeaders:CspReportOnly"] = "default-src 'self'; object-src 'none'; base-uri 'self'; frame-ancestors 'none'"
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
