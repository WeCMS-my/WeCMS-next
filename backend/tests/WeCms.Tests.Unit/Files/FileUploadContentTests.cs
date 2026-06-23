using Microsoft.AspNetCore.Http;
using WeCms.Modules.FileCenter.Files;

namespace WeCms.Tests.Unit.Files;

public sealed class FileUploadContentTests
{
    [Fact]
    public async Task ReadAsync_UsesMemoryForSmallFiles()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var requestSize = 1024L;
        var content = new byte[1024];

        await using var uploadContent = await FileUploadContent.ReadAsync(
            CreateFormFile("upload.bin", content),
            requestSize,
            cancellationToken);

        Assert.IsType<MemoryStream>(uploadContent.Content);
    }

    [Fact]
    public async Task ReadAsync_UsesTempFileForLargeFilesAndCleansItUp()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var threshold = 4L * 1024 * 1024;
        var content = new byte[threshold + 1];
        Random.Shared.NextBytes(content);

        string? tempPath;
        await using (var uploadContent = await FileUploadContent.ReadAsync(
            CreateFormFile("upload.bin", content),
            threshold + 1,
            cancellationToken))
        {
            var tempStream = Assert.IsType<FileStream>(uploadContent.Content);
            tempPath = tempStream.Name;
            Assert.NotNull(tempPath);
            Assert.True(File.Exists(tempPath));
        }

        Assert.NotNull(tempPath);
        Assert.False(File.Exists(tempPath));
    }

    private static IFormFile CreateFormFile(string fileName, byte[] content) => new FormFile(
        new MemoryStream(content),
        0,
        content.Length,
        "file",
        fileName)
    {
        Headers = new HeaderDictionary()
    };
}
