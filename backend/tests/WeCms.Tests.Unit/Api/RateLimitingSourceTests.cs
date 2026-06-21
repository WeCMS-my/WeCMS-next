namespace WeCms.Tests.Unit.Api;

public sealed class RateLimitingSourceTests
{
    [Fact]
    public async Task Program_RegistersAndUsesRateLimiterAfterSecurityBan()
    {
        var source = await File.ReadAllTextAsync(RepoPath("backend", "src", "WeCms.Api", "Program.cs"), TestContext.Current.CancellationToken);

        Assert.Contains("builder.Services.AddWeCmsRateLimiting(builder.Configuration);", source, StringComparison.Ordinal);
        Assert.Contains("app.UseRateLimiter();", source, StringComparison.Ordinal);
        Assert.True(
            source.IndexOf("app.UseMiddleware<SecurityBanMiddleware>();", StringComparison.Ordinal)
                < source.IndexOf("app.UseRateLimiter();", StringComparison.Ordinal));
        Assert.True(
            source.IndexOf("app.UseRateLimiter();", StringComparison.Ordinal)
                < source.IndexOf("app.UseAuthorization();", StringComparison.Ordinal));
    }

    [Fact]
    public async Task AuthEndpoints_BindRequiredAuthRateLimitPolicies()
    {
        var source = await File.ReadAllTextAsync(RepoPath("backend", "src", "WeCms.Modules.Identity", "Endpoints", "AuthEndpointDefinition.cs"), TestContext.Current.CancellationToken);

        Assert.Contains(".RequireRateLimiting(IdentityEndpointRateLimitPolicyNames.AuthLogin)", source, StringComparison.Ordinal);
        Assert.Contains(".RequireRateLimiting(IdentityEndpointRateLimitPolicyNames.AuthRefresh)", source, StringComparison.Ordinal);
        Assert.Contains(".RequireRateLimiting(IdentityEndpointRateLimitPolicyNames.AuthTwoFactor)", source, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HighRiskSystemEndpoints_BindSpecificRateLimitPolicies()
    {
        var filesSource = await File.ReadAllTextAsync(RepoPath("backend", "src", "WeCms.Modules.FileCenter", "Files", "FileEndpoints.cs"), TestContext.Current.CancellationToken);
        var securitySource = await File.ReadAllTextAsync(RepoPath("backend", "src", "WeCms.Modules.Security", "SecurityEndpoints.cs"), TestContext.Current.CancellationToken);
        var userSource = await File.ReadAllTextAsync(RepoPath("backend", "src", "WeCms.Modules.Identity", "Endpoints", "UserEndpointDefinition.cs"), TestContext.Current.CancellationToken);
        var menuSource = await File.ReadAllTextAsync(RepoPath("backend", "src", "WeCms.Modules.AccessControl", "Menus", "MenuEndpoints.cs"), TestContext.Current.CancellationToken);
        var dictSource = await File.ReadAllTextAsync(RepoPath("backend", "src", "WeCms.Modules.Configuration", "Dicts", "DictEndpoints.cs"), TestContext.Current.CancellationToken);
        var i18nSource = await File.ReadAllTextAsync(RepoPath("backend", "src", "WeCms.Modules.Configuration", "I18n", "I18nEndpoints.cs"), TestContext.Current.CancellationToken);

        Assert.Contains(".RequireRateLimiting(RateLimitPolicyNames.FileUpload)", filesSource, StringComparison.Ordinal);
        Assert.Contains(".RequireRateLimiting(RateLimitPolicyNames.SecurityUnban)", securitySource, StringComparison.Ordinal);
        Assert.Contains(".RequireRateLimiting(IdentityEndpointRateLimitPolicyNames.AdminWrite)", userSource, StringComparison.Ordinal);
        Assert.Contains(".RequireRateLimiting(RateLimitPolicyNames.AdminWrite)", menuSource, StringComparison.Ordinal);
        Assert.Contains(".RequireRateLimiting(AdminWriteRateLimitPolicy)", dictSource, StringComparison.Ordinal);
        Assert.Contains(".RequireRateLimiting(AdminWriteRateLimitPolicy)", i18nSource, StringComparison.Ordinal);
        var fileReadLines = filesSource.Split(Environment.NewLine)
            .Where(line => line.Contains("MapGet(\"/files", StringComparison.Ordinal))
            .ToArray();
        Assert.NotEmpty(fileReadLines);
        Assert.All(fileReadLines, line => Assert.DoesNotContain("RequireRateLimiting", line, StringComparison.Ordinal));
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
