using WeCms.Modules.FileCenter.Files;

namespace WeCms.Tests.Unit.Files;

public sealed class FileEndpointHttpTests
{
    [Fact]
    public void FileEndpoints_SourceDeclaresExpectedRoutesPermissionsAndAntiforgeryMetadata()
    {
        var source = File.ReadAllText(RepoPath("backend", "src", "WeCms.Modules.FileCenter", "Files", "FileEndpoints.cs"));

        Assert.Contains("group.MapGet(\"/files\", ListAsync).RequireEndpointPermission(FilePermissions.List);", source, StringComparison.Ordinal);
        Assert.Contains("group.MapGet(\"/files/{id:long}\", DetailAsync).RequireEndpointPermission(FilePermissions.Detail);", source, StringComparison.Ordinal);
        Assert.Contains("group.MapGet(\"/files/{id:long}/download\", DownloadAsync).RequireEndpointPermission(FilePermissions.Download);", source, StringComparison.Ordinal);
        Assert.Contains("group.MapGet(\"/files/{id:long}/preview\", PreviewAsync).RequireEndpointPermission(FilePermissions.Download);", source, StringComparison.Ordinal);
        Assert.Contains("group.MapDelete(\"/files/{id:long}\", DeleteAsync).RequireEndpointPermission(FilePermissions.Delete);", source, StringComparison.Ordinal);
        Assert.Contains(".DisableAntiforgery()", source, StringComparison.Ordinal);
        Assert.Contains(".RequireEndpointPermission(FilePermissions.Upload)", source, StringComparison.Ordinal);
    }

    [Fact]
    public void FilePermissions_SourceDeclaresDownloadPermission()
    {
        Assert.Equal("sys:file:download", FilePermissions.Download);
    }

    [Fact]
    public void FileEndpoints_SourceUsesSafeContentDispositionBuilderForPreview()
    {
        var source = File.ReadAllText(RepoPath("backend", "src", "WeCms.Modules.FileCenter", "Files", "FileEndpoints.cs"));

        Assert.Contains("ContentDispositionHeaderValue", source, StringComparison.Ordinal);
        Assert.Contains("FileNameStar = payload.FileName", source, StringComparison.Ordinal);
        Assert.DoesNotContain("httpContext.Response.Headers.ContentDisposition = $\"inline; filename=\\\"{payload.FileName}\\\"\";", source, StringComparison.Ordinal);
    }

    private static string RepoPath(params string[] segments)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "backend"))
                && File.Exists(Path.Combine(directory.FullName, "backend", "src", "WeCms.Api", "WeCms.Api.csproj")))
            {
                return Path.Combine([directory.FullName, .. segments]);
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
