using Microsoft.AspNetCore.Http;
using WeCms.Shared;
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

    [Fact]
    public async Task ReadAsync_UsesConfiguredTempPath()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var tempDirectory = Path.Combine(Path.GetTempPath(), "wecms-fileupload-options-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);
        string? tempPath = null;
        try
        {
            var content = new byte[3000];
            Random.Shared.NextBytes(content);

            await using (var uploadContent = await FileUploadContent.ReadAsync(
                CreateFormFile("upload.bin", content),
                4000,
                new FileUploadOptions
                {
                    MemoryFallbackThresholdBytes = 1024,
                    TempFilePath = tempDirectory
                },
                cancellationToken))
            {
                var tempStream = Assert.IsType<FileStream>(uploadContent.Content);
                tempPath = tempStream.Name;
                Assert.StartsWith(Path.GetFullPath(tempDirectory), tempStream.Name, StringComparison.Ordinal);
                Assert.True(File.Exists(tempStream.Name));
                Assert.Equal(3000, uploadContent.SizeBytes);
            }

            Assert.NotNull(tempPath);
        }
        finally
        {
            if (tempPath is not null)
            {
                Assert.False(File.Exists(tempPath));
            }

            Directory.Delete(tempDirectory, recursive: true);
        }
    }

    [Fact]
    public async Task ReadAsync_WhenTempDirectoryCannotBeCreatedReturnsServiceUnavailable()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        var conflictPath = Path.Combine(Path.GetTempPath(), $"wecms-fileupload-conflict-{Guid.NewGuid():N}");
        await File.WriteAllTextAsync(conflictPath, "conflict", cancellationToken);

        try
        {
            var content = new byte[3000];
            Random.Shared.NextBytes(content);

            var exception = await Assert.ThrowsAsync<DomainException>(() =>
                FileUploadContent.ReadAsync(
                    CreateFormFile("upload.bin", content),
                    4000,
                    new FileUploadOptions
                    {
                        MemoryFallbackThresholdBytes = 1024,
                        TempFilePath = conflictPath
                    },
                    cancellationToken));

            Assert.Equal(ApiCodes.ServiceUnavailable, exception.Code);
            Assert.Contains("Upload temporary storage is unavailable", exception.Message, StringComparison.Ordinal);
        }
        finally
        {
            if (File.Exists(conflictPath))
            {
                File.Delete(conflictPath);
            }
        }
    }

    [Fact]
    public void CleanupExpiredTempFiles_RemovesOnlyStaleUploadFiles()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "wecms-fileupload-cleanup-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);

        var staleFile = Path.Combine(tempDirectory, "wecms-upload-stale.bin");
        var freshFile = Path.Combine(tempDirectory, "wecms-upload-fresh.bin");
        var unrelatedFile = Path.Combine(tempDirectory, "other-tmp.bin");
        File.WriteAllText(staleFile, "stale");
        File.WriteAllText(freshFile, "fresh");
        File.WriteAllText(unrelatedFile, "other");
        File.SetLastWriteTimeUtc(staleFile, DateTimeOffset.UtcNow.AddHours(-25).UtcDateTime);
        File.SetLastWriteTimeUtc(freshFile, DateTimeOffset.UtcNow.AddMinutes(-30).UtcDateTime);

        FileUploadContent.CleanupExpiredTempFiles(new FileUploadOptions
        {
            TempFilePath = tempDirectory,
            TempFileRetentionHours = 24
        });

        Assert.False(File.Exists(staleFile));
        Assert.True(File.Exists(freshFile));
        Assert.True(File.Exists(unrelatedFile));

        Directory.Delete(tempDirectory, recursive: true);
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
