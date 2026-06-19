namespace WeCms.Tests.Unit.Api;

public sealed class SecurityPipelineSourceTests
{
    [Fact]
    public async Task Program_RegistersForwardedHeadersHstsAndCorsInSecurityOrder()
    {
        var source = await File.ReadAllTextAsync(RepoPath("backend", "src", "WeCms.Api", "Program.cs"), TestContext.Current.CancellationToken);

        Assert.Contains("builder.Services.AddWeCmsForwardedHeaders(builder.Configuration);", source, StringComparison.Ordinal);
        Assert.Contains("builder.Services.AddWeCmsCors(builder.Configuration);", source, StringComparison.Ordinal);
        Assert.Contains("app.UseWeCmsForwardedHeaders(builder.Configuration);", source, StringComparison.Ordinal);
        Assert.Contains("app.UseHsts();", source, StringComparison.Ordinal);
        Assert.Contains("app.UseCors(WeCmsCorsPolicyNames.AdminApi);", source, StringComparison.Ordinal);
        Assert.DoesNotContain("UseHttpsRedirection", source, StringComparison.Ordinal);

        var forwardedIndex = source.IndexOf("app.UseWeCmsForwardedHeaders", StringComparison.Ordinal);
        var hstsIndex = source.IndexOf("app.UseHsts", StringComparison.Ordinal);
        var requestIdIndex = source.IndexOf("app.UseMiddleware<RequestIdMiddleware>", StringComparison.Ordinal);
        var corsIndex = source.IndexOf("app.UseCors", StringComparison.Ordinal);
        var authIndex = source.IndexOf("app.UseAuthentication", StringComparison.Ordinal);
        var authorizationIndex = source.IndexOf("app.UseAuthorization", StringComparison.Ordinal);

        Assert.True(forwardedIndex >= 0);
        Assert.True(hstsIndex >= 0);
        Assert.True(requestIdIndex >= 0);
        Assert.True(forwardedIndex < hstsIndex);
        Assert.True(hstsIndex < requestIdIndex);
        Assert.True(corsIndex > requestIdIndex);
        Assert.True(authIndex > corsIndex);
        Assert.True(authorizationIndex > authIndex);
    }

    [Fact]
    public async Task CorsPolicy_UsesExplicitOriginsWithCredentials()
    {
        var source = await File.ReadAllTextAsync(RepoPath("backend", "src", "WeCms.Api", "Security", "WeCmsCorsExtensions.cs"), TestContext.Current.CancellationToken);

        Assert.Contains("Security:AllowedOrigins", source, StringComparison.Ordinal);
        Assert.Contains(".WithOrigins(origins)", source, StringComparison.Ordinal);
        Assert.Contains(".AllowCredentials()", source, StringComparison.Ordinal);
        Assert.DoesNotContain("AllowAnyOrigin", source, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ForwardedHeaders_RequiresExplicitEnableAndKnownProxyConfiguration()
    {
        var source = await File.ReadAllTextAsync(RepoPath("backend", "src", "WeCms.Api", "Security", "WeCmsForwardedHeadersExtensions.cs"), TestContext.Current.CancellationToken);

        Assert.Contains("Security:ForwardedHeaders", source, StringComparison.Ordinal);
        Assert.Contains("XForwardedFor", source, StringComparison.Ordinal);
        Assert.Contains("XForwardedProto", source, StringComparison.Ordinal);
        Assert.Contains("KnownProxies", source, StringComparison.Ordinal);
        Assert.Contains("KnownNetworks", source, StringComparison.Ordinal);
        Assert.Contains("IsEnabled", source, StringComparison.Ordinal);
    }

    private static string RepoPath(params string[] segments)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "backend", "WeCms.slnx")))
            {
                return Path.Combine([directory.FullName, .. segments]);
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
