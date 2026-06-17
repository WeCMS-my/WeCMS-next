namespace WeCms.Tests.Unit.Auth;

public sealed class AuthEndpointSourceTests
{
    [Fact]
    public async Task AuthEndpoints_ExplicitlyAllowAnonymousForLoginRefreshAndLogoutWhileMeRequiresAuthorization()
    {
        var source = await File.ReadAllTextAsync(RepoPath("backend", "src", "WeCms.Modules.System", "Auth", "AuthEndpoints.cs"));

        Assert.Contains("group.MapPost(\"/login\"", source, StringComparison.Ordinal);
        Assert.Contains(".AllowAnonymous();", source, StringComparison.Ordinal);
        Assert.Contains("group.MapPost(\"/refresh\"", source, StringComparison.Ordinal);
        Assert.Contains("ReadRefreshTokenCookie(context)", source, StringComparison.Ordinal);
        Assert.Contains("authService.RefreshAsync(refreshToken, CreateRequestContext(context), cancellationToken)", source, StringComparison.Ordinal);
        Assert.Contains("group.MapPost(\"/logout\"", source, StringComparison.Ordinal);
        Assert.Contains("authService.LogoutAsync(refreshToken, CreateRequestContext(context), cancellationToken)", source, StringComparison.Ordinal);

        var logoutRouteIndex = source.IndexOf("group.MapPost(\"/logout\"", StringComparison.Ordinal);
        Assert.True(logoutRouteIndex >= 0);
        var logoutAnonymousIndex = source.IndexOf(".AllowAnonymous();", logoutRouteIndex, StringComparison.Ordinal);
        Assert.True(logoutAnonymousIndex > logoutRouteIndex);

        var meRouteIndex = source.IndexOf("group.MapGet(\"/me\"", StringComparison.Ordinal);
        Assert.True(meRouteIndex >= 0);
        var meAuthorizationIndex = source.IndexOf(".RequireAuthorization();", meRouteIndex, StringComparison.Ordinal);
        Assert.True(meAuthorizationIndex > meRouteIndex);

        Assert.DoesNotContain(".RequireAuthorization();", source[logoutRouteIndex..meRouteIndex], StringComparison.Ordinal);
    }

    [Fact]
    public async Task AuthEndpoints_UseSecureHttpOnlyRefreshCookie()
    {
        var source = await File.ReadAllTextAsync(RepoPath("backend", "src", "WeCms.Modules.System", "Auth", "AuthEndpoints.cs"));

        Assert.Contains("RefreshCookieName = \"__Host-wecms_refresh\"", source, StringComparison.Ordinal);
        Assert.Contains("HttpOnly = true", source, StringComparison.Ordinal);
        Assert.Contains("Secure = true", source, StringComparison.Ordinal);
        Assert.Contains("SameSite = SameSiteMode.Strict", source, StringComparison.Ordinal);
        Assert.Contains("Path = \"/\"", source, StringComparison.Ordinal);
        Assert.Contains("context.Response.Cookies.Append(RefreshCookieName", source, StringComparison.Ordinal);
        Assert.Contains("context.Response.Cookies.Delete(RefreshCookieName", source, StringComparison.Ordinal);
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
