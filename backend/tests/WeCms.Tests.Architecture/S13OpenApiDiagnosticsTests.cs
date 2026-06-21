namespace WeCms.Tests.Architecture;

public sealed class S13OpenApiDiagnosticsTests
{
    [Fact]
    public async Task Swagger_IsNotUsingControllers()
    {
        var apiRoot = Path.Combine(TestPaths.SourceRoot, "WeCms.Api");
        var project = await File.ReadAllTextAsync(
            Path.Combine(apiRoot, "WeCms.Api.csproj"),
            TestContext.Current.CancellationToken);
        var program = await File.ReadAllTextAsync(
            Path.Combine(apiRoot, "Program.cs"),
            TestContext.Current.CancellationToken);

        Assert.Contains("Swashbuckle.AspNetCore", project, StringComparison.Ordinal);
        Assert.Contains("Scalar.AspNetCore.Swashbuckle", project, StringComparison.Ordinal);
        Assert.Contains("MiniProfiler.AspNetCore.Mvc", project, StringComparison.Ordinal);
        Assert.Contains("AddWeCmsOpenApiDocumentation", program, StringComparison.Ordinal);
        Assert.Contains("MapWeCmsOpenApiDocumentation", program, StringComparison.Ordinal);
        Assert.Contains("AddWeCmsDiagnostics", program, StringComparison.Ordinal);
        Assert.Contains("UseWeCmsDiagnostics", program, StringComparison.Ordinal);
        Assert.DoesNotContain("AddControllers", program, StringComparison.Ordinal);
        Assert.DoesNotContain("MapControllers", program, StringComparison.Ordinal);
        Assert.DoesNotContain("ControllerBase", program, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SwaggerAndScalar_AreApiHostOnly()
    {
        var sourceRoot = TestPaths.SourceRoot;
        var moduleProjects = Directory
            .EnumerateFiles(sourceRoot, "WeCms.Modules.*.csproj", SearchOption.AllDirectories)
            .Where(path => !path.Contains(".SqlSugar", StringComparison.Ordinal))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        foreach (var projectPath in moduleProjects)
        {
            var project = await File.ReadAllTextAsync(projectPath, TestContext.Current.CancellationToken);
            Assert.DoesNotContain("Swashbuckle", project, StringComparison.Ordinal);
            Assert.DoesNotContain("Scalar.AspNetCore", project, StringComparison.Ordinal);
            Assert.DoesNotContain("MiniProfiler", project, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task ApiHost_RegistersKestrelWithSlimBuilder()
    {
        var program = await File.ReadAllTextAsync(
            Path.Combine(TestPaths.SourceRoot, "WeCms.Api", "Program.cs"),
            TestContext.Current.CancellationToken);

        Assert.Contains("WebApplication.CreateSlimBuilder(args)", program, StringComparison.Ordinal);
        Assert.Contains("builder.WebHost.UseKestrel();", program, StringComparison.Ordinal);
    }
}
