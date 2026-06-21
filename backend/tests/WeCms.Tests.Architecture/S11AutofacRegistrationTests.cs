namespace WeCms.Tests.Architecture;

public sealed class S11AutofacRegistrationTests
{
    [Fact]
    public async Task AutofacModule_RegistersApplicationServices()
    {
        var module = await ReadAopModuleSourceAsync();
        var apiProject = await File.ReadAllTextAsync(
            Path.Combine(TestPaths.SourceRoot, "WeCms.Api", "WeCms.Api.csproj"),
            TestContext.Current.CancellationToken);
        var program = await File.ReadAllTextAsync(
            Path.Combine(TestPaths.SourceRoot, "WeCms.Api", "Program.cs"),
            TestContext.Current.CancellationToken);

        Assert.Contains("Autofac.Extensions.DependencyInjection", apiProject, StringComparison.Ordinal);
        Assert.Contains("UseServiceProviderFactory(new AutofacServiceProviderFactory())", program, StringComparison.Ordinal);
        Assert.Contains("RegisterModule(new WeCmsAopModule())", program, StringComparison.Ordinal);
        Assert.Contains("EnableInterfaceInterceptors", module, StringComparison.Ordinal);
        Assert.Contains("ApplicationServiceAopInterceptor", module, StringComparison.Ordinal);
        Assert.Contains("IsApplicationService", module, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RepositoryTypes_AreNotIntercepted()
    {
        var module = await ReadAopModuleSourceAsync();

        Assert.Contains("!IsRepositoryType", module, StringComparison.Ordinal);
        Assert.DoesNotContain("EnableClassInterceptors", module, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EndpointHandlers_AreNotIntercepted()
    {
        var module = await ReadAopModuleSourceAsync();

        Assert.Contains("!IsEndpointType", module, StringComparison.Ordinal);
        Assert.DoesNotContain("EndpointFilterInvocationContext", module, StringComparison.Ordinal);
        Assert.DoesNotContain("MapGet", module, StringComparison.Ordinal);
        Assert.DoesNotContain("MapPost", module, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AopRegistration_DoesNotScanEndpointsAtRuntime()
    {
        var module = await ReadAopModuleSourceAsync();

        Assert.DoesNotContain("MapEndpoint", module, StringComparison.Ordinal);
        Assert.DoesNotContain("EndpointDataSource", module, StringComparison.Ordinal);
        Assert.DoesNotContain("RouteEndpoint", module, StringComparison.Ordinal);
    }

    private static Task<string> ReadAopModuleSourceAsync()
    {
        return File.ReadAllTextAsync(
            Path.Combine(TestPaths.SourceRoot, "WeCms.Aop", "WeCmsAopModule.cs"),
            TestContext.Current.CancellationToken);
    }
}
