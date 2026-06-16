namespace WeCms.Tests.Unit.Auth;

public sealed class AuthEndpointSourceTests
{
    [Fact]
    public async Task AuthEndpoints_ExplicitlyRequireAuthorizationForMeAndAllowAnonymousForLogin()
    {
        var source = await File.ReadAllTextAsync(RepoPath("backend", "src", "WeCms.Modules.System", "Auth", "AuthEndpoints.cs"));

        Assert.Contains("group.MapPost(\"/login\"", source, StringComparison.Ordinal);
        Assert.Contains(".AllowAnonymous();", source, StringComparison.Ordinal);
        Assert.Contains("group.MapPost(\"/refresh\"", source, StringComparison.Ordinal);
        Assert.Contains("authService.RefreshAsync(request, CreateRequestContext(context), cancellationToken)", source, StringComparison.Ordinal);
        Assert.Contains("group.MapPost(\"/logout\"", source, StringComparison.Ordinal);
        Assert.Contains("authService.LogoutAsync(request, CreateRequestContext(context), cancellationToken)", source, StringComparison.Ordinal);

        var logoutRouteIndex = source.IndexOf("group.MapPost(\"/logout\"", StringComparison.Ordinal);
        Assert.True(logoutRouteIndex >= 0);
        var logoutAuthorizationIndex = source.IndexOf(".RequireAuthorization();", logoutRouteIndex, StringComparison.Ordinal);
        Assert.True(logoutAuthorizationIndex > logoutRouteIndex);

        Assert.Contains("group.MapGet(\"/me\"", source, StringComparison.Ordinal);
        Assert.Contains(".RequireAuthorization();", source, StringComparison.Ordinal);
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
