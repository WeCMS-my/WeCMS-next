namespace WeCms.Tests.Unit.Api;

public sealed class RateLimitingSourceTests
{
    [Fact]
    public async Task Program_RegistersAndUsesRateLimiterAfterSecurityBan()
    {
        var source = await File.ReadAllTextAsync(RepoPath("backend", "src", "WeCms.Api", "Program.cs"));

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
        var source = await File.ReadAllTextAsync(RepoPath("backend", "src", "WeCms.Modules.System", "Auth", "AuthEndpoints.cs"));

        Assert.Contains($".RequireRateLimiting(RateLimitPolicyNames.AuthLogin)", source, StringComparison.Ordinal);
        Assert.Contains($".RequireRateLimiting(RateLimitPolicyNames.AuthRefresh)", source, StringComparison.Ordinal);
        Assert.Contains($".RequireRateLimiting(RateLimitPolicyNames.AuthTwoFactor)", source, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HighRiskSystemEndpoints_BindSpecificRateLimitPolicies()
    {
        var filesSource = await File.ReadAllTextAsync(RepoPath("backend", "src", "WeCms.Modules.System", "Files", "FileEndpoints.cs"));
        var securitySource = await File.ReadAllTextAsync(RepoPath("backend", "src", "WeCms.Modules.System", "Security", "SecurityEndpoints.cs"));
        var userSource = await File.ReadAllTextAsync(RepoPath("backend", "src", "WeCms.Modules.System", "Users", "UserEndpoints.cs"));
        var menuSource = await File.ReadAllTextAsync(RepoPath("backend", "src", "WeCms.Modules.System", "Menus", "MenuEndpoints.cs"));

        Assert.Contains(".RequireRateLimiting(RateLimitPolicyNames.FileUpload)", filesSource, StringComparison.Ordinal);
        Assert.Contains(".RequireRateLimiting(RateLimitPolicyNames.SecurityUnban)", securitySource, StringComparison.Ordinal);
        Assert.Contains(".RequireRateLimiting(RateLimitPolicyNames.AdminWrite)", userSource, StringComparison.Ordinal);
        Assert.Contains(".RequireRateLimiting(RateLimitPolicyNames.AdminWrite)", menuSource, StringComparison.Ordinal);
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
