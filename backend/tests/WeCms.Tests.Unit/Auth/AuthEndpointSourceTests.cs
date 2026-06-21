namespace WeCms.Tests.Unit.Auth;

public sealed class AuthEndpointSourceTests
{
    [Fact]
    public async Task AuthEndpoints_ExplicitlyAllowAnonymousForLoginRefreshAndLogoutWhileMeRequiresAuthorization()
    {
        var source = await File.ReadAllTextAsync(RepoPath("backend", "src", "WeCms.Modules.Identity", "Endpoints", "AuthEndpointDefinition.cs"), TestContext.Current.CancellationToken);

        Assert.Contains("group.MapPost(\"/login\"", source, StringComparison.Ordinal);
        Assert.Contains(".AllowAnonymous();", source, StringComparison.Ordinal);
        Assert.Contains("group.MapPost(\"/refresh\"", source, StringComparison.Ordinal);
        Assert.Contains("IIdentityCookieAuthOriginValidator cookieAuthOriginValidator", source, StringComparison.Ordinal);
        Assert.Contains("await cookieAuthOriginValidator.ValidateAsync(context, IdentityCookieAuthOriginEndpoints.Refresh, CreateRequestContext(context), cancellationToken)", source, StringComparison.Ordinal);
        Assert.Contains("ReadRefreshTokenCookie(context)", source, StringComparison.Ordinal);
        Assert.Contains("authService.RefreshAsync(refreshToken, CreateRequestContext(context), cancellationToken)", source, StringComparison.Ordinal);
        Assert.Contains("group.MapPost(\"/logout\"", source, StringComparison.Ordinal);
        Assert.Contains("await cookieAuthOriginValidator.ValidateAsync(context, IdentityCookieAuthOriginEndpoints.Logout, CreateRequestContext(context), cancellationToken)", source, StringComparison.Ordinal);
        Assert.Contains("authService.LogoutAsync(refreshToken, CreateRequestContext(context), cancellationToken)", source, StringComparison.Ordinal);
        Assert.Contains("group.MapPost(\"/2fa/verify\"", source, StringComparison.Ordinal);
        Assert.Contains("IdentityCookieAuthOriginEndpoints.TwoFactorVerify", source, StringComparison.Ordinal);
        Assert.Contains("authService.VerifyTwoFactorAsync(request, requestContext, cancellationToken)", source, StringComparison.Ordinal);
        Assert.Contains("group.MapPost(\"/2fa/recovery-code\"", source, StringComparison.Ordinal);
        Assert.Contains("IdentityCookieAuthOriginEndpoints.TwoFactorRecoveryCode", source, StringComparison.Ordinal);
        Assert.Contains("authService.VerifyTwoFactorRecoveryCodeAsync(request, requestContext, cancellationToken)", source, StringComparison.Ordinal);

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
        var source = await File.ReadAllTextAsync(RepoPath("backend", "src", "WeCms.Modules.Identity", "Endpoints", "AuthEndpointDefinition.cs"), TestContext.Current.CancellationToken);

        Assert.Contains("RefreshCookieName = \"__Host-wecms_refresh\"", source, StringComparison.Ordinal);
        Assert.Contains("HttpOnly = true", source, StringComparison.Ordinal);
        Assert.Contains("Secure = true", source, StringComparison.Ordinal);
        Assert.Contains("SameSite = SameSiteMode.Strict", source, StringComparison.Ordinal);
        Assert.Contains("Path = \"/\"", source, StringComparison.Ordinal);
        Assert.Contains("context.Response.Cookies.Append(RefreshCookieName", source, StringComparison.Ordinal);
        Assert.Contains("context.Response.Cookies.Delete(RefreshCookieName", source, StringComparison.Ordinal);
        Assert.Contains("RefreshCookieOptionsFactory.CreateAppendOptions(session)", source, StringComparison.Ordinal);
        Assert.Contains("RefreshCookieOptionsFactory.CreateDeleteOptions()", source, StringComparison.Ordinal);
        Assert.Contains("CreateBaseOptions()", source, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AccountTwoFactorEndpoints_AreExplicitlyAuthenticatedSelfServiceRoutes()
    {
        var endpointSource = await File.ReadAllTextAsync(RepoPath("backend", "src", "WeCms.Modules.Identity", "Endpoints", "AccountTwoFactorEndpointDefinition.cs"), TestContext.Current.CancellationToken);
        var endpointMapSource = await File.ReadAllTextAsync(RepoPath("backend", "src", "WeCms.Api", "Endpoints", "WeCmsApiEndpointRouteBuilderExtensions.cs"), TestContext.Current.CancellationToken);

        Assert.Contains("registry.Add(new AccountTwoFactorEndpointDefinition());", endpointMapSource, StringComparison.Ordinal);
        Assert.Contains("MapGroup(\"/api/v1/account/2fa\")", endpointSource, StringComparison.Ordinal);
        Assert.Contains(".RequireAuthorization();", endpointSource, StringComparison.Ordinal);
        Assert.Contains("group.MapGet(\"/status\"", endpointSource, StringComparison.Ordinal);
        Assert.Contains("group.MapPost(\"/setup\"", endpointSource, StringComparison.Ordinal);
        Assert.Contains("group.MapPost(\"/confirm\"", endpointSource, StringComparison.Ordinal);
        Assert.Contains("group.MapPost(\"/disable\"", endpointSource, StringComparison.Ordinal);
        Assert.Contains("group.MapPost(\"/recovery-codes/regenerate\"", endpointSource, StringComparison.Ordinal);
        Assert.DoesNotContain("AllowAnonymous", endpointSource, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AccountProfileEndpoints_AreExplicitlyAuthenticatedSelfServiceRoutes()
    {
        var endpointSource = await File.ReadAllTextAsync(RepoPath("backend", "src", "WeCms.Modules.Identity", "Endpoints", "AccountProfileEndpointDefinition.cs"), TestContext.Current.CancellationToken);
        var endpointMapSource = await File.ReadAllTextAsync(RepoPath("backend", "src", "WeCms.Api", "Endpoints", "WeCmsApiEndpointRouteBuilderExtensions.cs"), TestContext.Current.CancellationToken);

        Assert.Contains("registry.Add(new AccountProfileEndpointDefinition());", endpointMapSource, StringComparison.Ordinal);
        Assert.Contains("MapGroup(\"/api/v1/account\")", endpointSource, StringComparison.Ordinal);
        Assert.Contains(".RequireAuthorization();", endpointSource, StringComparison.Ordinal);
        Assert.Contains("group.MapGet(\"/profile\"", endpointSource, StringComparison.Ordinal);
        Assert.Contains("group.MapPut(\"/profile\"", endpointSource, StringComparison.Ordinal);
        Assert.Contains("group.MapPut(\"/password\"", endpointSource, StringComparison.Ordinal);
        Assert.Contains("group.MapPost(\"/avatar\"", endpointSource, StringComparison.Ordinal);
        Assert.Contains("group.MapGet(\"/avatar/content\"", endpointSource, StringComparison.Ordinal);
        Assert.Contains("group.MapGet(\"/security\"", endpointSource, StringComparison.Ordinal);
        Assert.DoesNotContain("AllowAnonymous", endpointSource, StringComparison.Ordinal);
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
