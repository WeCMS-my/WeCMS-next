namespace WeCms.Tests.Architecture;

public sealed class FileApiScanTests
{
    [Fact]
    public void FileEndpoints_AreExplicitAndPermissionProtected()
    {
        var source = File.ReadAllText(Path.Combine(TestPaths.RepoRoot, "backend", "src", "WeCms.Modules.FileCenter", "Files", "FileEndpoints.cs"));

        Assert.Contains("MapGroup(\"/api/v1/system\")", source, StringComparison.Ordinal);
        Assert.Contains(".RequireAuthorization()", source, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/files\"", source, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/files/{id:long}\"", source, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/files/{id:long}/download\"", source, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/files/{id:long}/preview\"", source, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/files\"", source, StringComparison.Ordinal);
        Assert.Contains("MapDelete(\"/files/{id:long}\"", source, StringComparison.Ordinal);
        Assert.Contains("RequireEndpointPermission(FilePermissions.List)", source, StringComparison.Ordinal);
        Assert.Contains("RequireEndpointPermission(FilePermissions.Detail)", source, StringComparison.Ordinal);
        Assert.Contains("RequireEndpointPermission(FilePermissions.Upload)", source, StringComparison.Ordinal);
        Assert.Contains("RequireEndpointPermission(FilePermissions.Download)", source, StringComparison.Ordinal);
        Assert.Contains("RequireEndpointPermission(FilePermissions.Delete)", source, StringComparison.Ordinal);
    }
}
