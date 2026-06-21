namespace WeCms.Tests.Unit.Security;

public sealed class SecurityEndpointSourceTests
{
    [Fact]
    public void SecurityEndpoints_ArePermissionProtectedAndUseExpectedMethods()
    {
        var source = File.ReadAllText(Path.Combine(RepoRoot(), "backend", "src", "WeCms.Modules.Security", "SecurityEndpoints.cs"));

        Assert.Contains("MapGroup(\"/api/v1/system/security\")", source, StringComparison.Ordinal);
        Assert.Contains(".RequireAuthorization()", source, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/status\", StatusAsync).RequireEndpointPermission(SecurityPermissions.Status)", source, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/bans\", ListBansAsync).RequireEndpointPermission(SecurityPermissions.BanList)", source, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/bans/{id:long}\", GetBanAsync).RequireEndpointPermission(SecurityPermissions.BanDetail)", source, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/bans/{id:long}/unban\", UnbanAsync).RequireEndpointPermission(SecurityPermissions.BanUnban)", source, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/bans/batch-unban\", BatchUnbanAsync).RequireEndpointPermission(SecurityPermissions.BanBatchUnban)", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Program_MapsSecurityEndpoints()
    {
        var source = File.ReadAllText(Path.Combine(RepoRoot(), "backend", "src", "WeCms.Api", "Endpoints", "WeCmsApiEndpointRouteBuilderExtensions.cs"));

        Assert.Contains("endpoints.MapSecurityEndpoints();", source, StringComparison.Ordinal);
    }

    private static string RepoRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null && !File.Exists(Path.Combine(current.FullName, "AGENTS.md")))
        {
            current = current.Parent;
        }

        return current?.FullName ?? throw new InvalidOperationException("Repository root was not found.");
    }
}
