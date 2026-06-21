using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using WeCms.Api.Extensions;

namespace WeCms.Tests.Unit.OpenApi;

public sealed class OpenApiDocumentationConfigurationTests
{
    [Fact]
    public void Swagger_EnabledInDevelopment()
    {
        var environment = new TestHostEnvironment(Environments.Development);
        var configuration = EmptyConfiguration();

        Assert.True(WeCmsOpenApiDocumentationExtensions.IsOpenApiDocumentationEnabled(environment, configuration));
    }

    [Fact]
    public void Swagger_NotEnabledByDefaultInNonDevelopment()
    {
        var environment = new TestHostEnvironment(Environments.Production);
        var configuration = EmptyConfiguration();

        Assert.False(WeCmsOpenApiDocumentationExtensions.IsOpenApiDocumentationEnabled(environment, configuration));
    }

    [Fact]
    public void Swagger_EnabledOutsideDevelopmentWithExplicitConfiguration()
    {
        var environment = new TestHostEnvironment(Environments.Production);
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["OpenApiDocumentation:Enabled"] = "true"
            })
            .Build();

        Assert.True(WeCmsOpenApiDocumentationExtensions.IsOpenApiDocumentationEnabled(environment, configuration));
    }

    [Fact]
    public async Task SwaggerAndScalar_AreMappedBehindDocumentationGate()
    {
        var source = await File.ReadAllTextAsync(
            Path.Combine(SourceRoot, "WeCms.Api", "Extensions", "WeCmsOpenApiDocumentationExtensions.cs"),
            TestContext.Current.CancellationToken);

        Assert.Contains("IsOpenApiDocumentationEnabled(app.Environment, app.Configuration)", source, StringComparison.Ordinal);
        Assert.Contains("app.UseSwagger()", source, StringComparison.Ordinal);
        Assert.Contains("app.UseSwaggerUI", source, StringComparison.Ordinal);
        Assert.Contains("app.MapScalarApiReference", source, StringComparison.Ordinal);
        Assert.Contains("OpenApiDocumentation", source, StringComparison.Ordinal);
        Assert.DoesNotContain("AddControllers", source, StringComparison.Ordinal);
        Assert.DoesNotContain("MapControllers", source, StringComparison.Ordinal);
    }

    private static IConfiguration EmptyConfiguration()
    {
        return new ConfigurationBuilder().Build();
    }

    private static string SourceRoot
    {
        get
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "backend", "WeCms.slnx")))
                {
                    return Path.Combine(directory.FullName, "backend", "src");
                }

                directory = directory.Parent;
            }

            throw new DirectoryNotFoundException("Could not locate repository root.");
        }
    }

    private sealed class TestHostEnvironment(string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = environmentName;
        public string ApplicationName { get; set; } = "WeCms.Tests";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
