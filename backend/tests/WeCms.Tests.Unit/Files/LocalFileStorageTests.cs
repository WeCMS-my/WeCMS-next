using System.Security.Cryptography;
using System.Text;
using WeCms.Infrastructure.Files;
using WeCms.Shared;

namespace WeCms.Tests.Unit.Files;

public sealed class LocalFileStorageTests
{
    [Fact]
    public async Task StoreAsync_UsesConfiguredBasePathAndExposesMetadata()
    {
        var basePath = TempPath();
        var storage = new LocalFileStorage(basePath);
        var content = Encoding.UTF8.GetBytes("hello");

        var result = await storage.StoreAsync(new MemoryStream(content), "documents/a.txt", ".txt", 1024, CancellationToken.None);

        Assert.Equal(content.Length, result.SizeBytes);
        Assert.Equal(ComputeSha256(content), result.Sha256);
        Assert.Equal("text/plain", result.MimeType);
        Assert.True(await storage.ExistsAsync("documents/a.txt", CancellationToken.None));
        var metadata = await storage.GetMetadataAsync("documents/a.txt", CancellationToken.None);
        Assert.Equal(content.Length, metadata.SizeBytes);
        Assert.True(File.Exists(Path.Combine(basePath, "documents", "a.txt")));
    }

    [Fact]
    public async Task StoreAsync_RejectsExistingObjectKeyWithoutOverwritingContent()
    {
        var basePath = TempPath();
        var storage = new LocalFileStorage(basePath);
        var original = Encoding.UTF8.GetBytes("original");
        var replacement = Encoding.UTF8.GetBytes("replacement");
        const string objectKey = "documents/a.txt";

        await storage.StoreAsync(new MemoryStream(original), objectKey, ".txt", 1024, CancellationToken.None);

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            storage.StoreAsync(new MemoryStream(replacement), objectKey, ".txt", 1024, CancellationToken.None));

        Assert.Equal(ApiCodes.Conflict, exception.Code);
        Assert.Equal(original, await File.ReadAllBytesAsync(Path.Combine(basePath, "documents", "a.txt"), CancellationToken.None));
    }

    [Theory]
    [InlineData("../outside.txt")]
    [InlineData("/tmp/outside.txt")]
    public async Task StoreAsync_RejectsPathTraversal(string objectKey)
    {
        var storage = new LocalFileStorage(TempPath());

        var exception = await Assert.ThrowsAsync<DomainException>(() =>
            storage.StoreAsync(new MemoryStream([1, 2, 3]), objectKey, ".txt", 1024, CancellationToken.None));

        Assert.Equal(ApiCodes.ValidationError, exception.Code);
    }

    private static string TempPath()
    {
        return Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), "wecms-local-storage-tests", Guid.NewGuid().ToString("N"))).FullName;
    }

    private static string ComputeSha256(byte[] bytes)
    {
        using var sha = SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(bytes)).ToLowerInvariant();
    }
}
