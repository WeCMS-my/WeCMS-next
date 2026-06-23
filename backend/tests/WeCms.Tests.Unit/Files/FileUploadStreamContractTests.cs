using System.Text.RegularExpressions;

namespace WeCms.Tests.Unit.Files;

public sealed class FileUploadStreamContractTests
{
    [Fact]
    public void FileUploadPolicyValidation_UsesBufferedStreamContract()
    {
        var source = File.ReadAllText(RepoPath("backend", "src", "WeCms.Modules.FileCenter", "Files", "FileUploadPolicies.cs"));

        Assert.Contains("Task ValidateContentAsync(Stream content", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ValidateContentAsync(IFormFile", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ToArray(", source, StringComparison.Ordinal);
        Assert.DoesNotContain("new MemoryStream", source, StringComparison.Ordinal);
    }

    [Fact]
    public void UploadServices_DelegateIncomingFormFileReadToSingleBufferingBoundary()
    {
        var serviceSource = File.ReadAllText(RepoPath("backend", "src", "WeCms.Modules.FileCenter", "Files", "FileService.cs"));
        var avatarServiceSource = File.ReadAllText(RepoPath("backend", "src", "WeCms.Api", "Files", "AccountAvatarFileService.cs"));
        var bufferSource = File.ReadAllText(RepoPath("backend", "src", "WeCms.Modules.FileCenter", "Files", "FileUploadContent.cs"));

        Assert.DoesNotContain("file.OpenReadStream(", serviceSource, StringComparison.Ordinal);
        Assert.DoesNotContain("file.OpenReadStream(", avatarServiceSource, StringComparison.Ordinal);
        Assert.Single(Regex.Matches(bufferSource, "file\\.OpenReadStream\\("));
        Assert.DoesNotContain("ScanAsync(IFormFile", serviceSource, StringComparison.Ordinal);
        Assert.DoesNotContain("ScanAvatarAsync(IFormFile", avatarServiceSource, StringComparison.Ordinal);
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
