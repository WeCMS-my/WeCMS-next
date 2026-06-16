namespace WeCms.Tests.Architecture;

public sealed class FileApiScanTests
{
    [Fact]
    public void FileEndpoints_AreExplicitAndPermissionProtected()
    {
        var source = File.ReadAllText(Path.Combine(TestPaths.RepoRoot, "backend", "src", "WeCms.Modules.System", "Files", "FileEndpoints.cs"));

        Assert.Contains("MapGroup(\"/api/v1/system\")", source, StringComparison.Ordinal);
        Assert.Contains(".RequireAuthorization()", source, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/files\"", source, StringComparison.Ordinal);
        Assert.Contains("MapGet(\"/files/{id:long}\"", source, StringComparison.Ordinal);
        Assert.Contains("MapPost(\"/files\"", source, StringComparison.Ordinal);
        Assert.Contains("MapDelete(\"/files/{id:long}\"", source, StringComparison.Ordinal);
        Assert.Contains("RequirePermission(FilePermissions.List)", source, StringComparison.Ordinal);
        Assert.Contains("RequirePermission(FilePermissions.Detail)", source, StringComparison.Ordinal);
        Assert.Contains("RequirePermission(FilePermissions.Upload)", source, StringComparison.Ordinal);
        Assert.Contains("RequirePermission(FilePermissions.Delete)", source, StringComparison.Ordinal);
    }
}
