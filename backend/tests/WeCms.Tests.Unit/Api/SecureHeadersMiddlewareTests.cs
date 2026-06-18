using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using WeCms.Api.Middleware;

namespace WeCms.Tests.Unit.Api;

public sealed class SecureHeadersMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_AddsSecurityHeadersAndReportOnlyCsp()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var middleware = new SecureHeadersMiddleware(
            httpContext => httpContext.Response.WriteAsync("ok"),
            new ConfigurationBuilder().Build(),
            new FakeHostEnvironment("Production"));

        await middleware.InvokeAsync(context);

        Assert.Equal("nosniff", context.Response.Headers["X-Content-Type-Options"]);
        Assert.Equal("DENY", context.Response.Headers["X-Frame-Options"]);
        Assert.Equal("no-referrer", context.Response.Headers["Referrer-Policy"]);
        Assert.Equal("geolocation=(), microphone=(), camera=()", context.Response.Headers["Permissions-Policy"]);
        Assert.Contains("default-src 'self'", context.Response.Headers["Content-Security-Policy-Report-Only"].ToString(), StringComparison.Ordinal);
        Assert.False(context.Response.Headers.ContainsKey("Content-Security-Policy"));
    }

    [Fact]
    public async Task InvokeAsync_DoesNotOverwriteExistingEndpointHeaders()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        context.Response.Headers["X-Content-Type-Options"] = "custom-nosniff";
        var middleware = new SecureHeadersMiddleware(
            httpContext => httpContext.Response.WriteAsync("ok"),
            new ConfigurationBuilder().Build(),
            new FakeHostEnvironment("Production"));

        await middleware.InvokeAsync(context);

        Assert.Equal("custom-nosniff", context.Response.Headers["X-Content-Type-Options"]);
    }

    [Fact]
    public async Task InvokeAsync_UsesDevelopmentVitePolicy()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var middleware = new SecureHeadersMiddleware(
            httpContext => httpContext.Response.WriteAsync("ok"),
            new ConfigurationBuilder().Build(),
            new FakeHostEnvironment("Development"));

        await middleware.InvokeAsync(context);

        var csp = context.Response.Headers["Content-Security-Policy-Report-Only"].ToString();
        Assert.Contains("ws:", csp, StringComparison.Ordinal);
        Assert.Contains("http://localhost:*", csp, StringComparison.Ordinal);
        Assert.Contains("frame-ancestors 'none'", csp, StringComparison.Ordinal);
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
        public string WebRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}
