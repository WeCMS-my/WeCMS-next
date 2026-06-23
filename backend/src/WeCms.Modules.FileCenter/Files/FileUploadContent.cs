using Microsoft.AspNetCore.Http;
using WeCms.Shared;

namespace WeCms.Modules.FileCenter.Files;

public sealed class FileUploadContent : IAsyncDisposable
{
    private const int ChunkSize = 8192;
    private const long MemoryFallbackThresholdBytes = 4L * 1024 * 1024;

    private readonly Stream _content;
    private readonly string? _tempFilePath;

    private FileUploadContent(Stream content, long sizeBytes, string? tempFilePath = null)
    {
        _content = content;
        SizeBytes = sizeBytes;
        _tempFilePath = tempFilePath;
    }

    public Stream Content => _content;
    public long SizeBytes { get; }

    public static async Task<FileUploadContent> ReadAsync(IFormFile file, long maxSizeBytes, CancellationToken cancellationToken)
    {
        if (file is null || file.Length <= 0)
        {
            throw Validation("file is required and must not be empty.");
        }

        await using var source = file.OpenReadStream();
        Stream content = new MemoryStream();
        var totalBytes = 0L;
        var buffer = new byte[ChunkSize];
        string? tempFilePath = null;

        try
        {
            int read;
            while ((read = await source.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
            {
                totalBytes += read;
                if (totalBytes > maxSizeBytes)
                {
                    throw Validation($"sizeBytes must be between 1 and {maxSizeBytes}.");
                }

                if (tempFilePath is null && totalBytes > MemoryFallbackThresholdBytes)
                {
                    tempFilePath = Path.GetTempFileName();
                    var tempFileStream = new FileStream(
                        tempFilePath,
                        FileMode.Create,
                        FileAccess.ReadWrite,
                        FileShare.None,
                        bufferSize: 81920,
                        useAsync: true);

                    content.Position = 0;
                    await content.CopyToAsync(tempFileStream, cancellationToken);
                    content.Dispose();
                    content = tempFileStream;
                }

                await content.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            }

            if (totalBytes <= 0)
            {
                throw Validation("file is required and must not be empty.");
            }

            content.Position = 0;
            if (tempFilePath is null)
            {
                return new FileUploadContent(content, totalBytes);
            }

            if (content is FileStream tempFile)
            {
                await tempFile.FlushAsync(cancellationToken);
            }

            return new FileUploadContent(content, totalBytes, tempFilePath);
        }
        catch
        {
            await content.DisposeAsync();
            if (tempFilePath is not null)
            {
                await DeleteTempFileAsync(tempFilePath);
            }

            throw;
        }
    }

    public void Rewind()
    {
        _content.Position = 0;
    }

    public async ValueTask DisposeAsync()
    {
        await _content.DisposeAsync();
        await DeleteTempFileAsync(_tempFilePath);
    }

    private static async Task DeleteTempFileAsync(string? filePath)
    {
        if (filePath is null)
        {
            return;
        }

        for (var attempt = 0; attempt < 2; attempt++)
        {
            try
            {
                if (File.Exists(filePath))
                {
                    await Task.Run(() => File.Delete(filePath));
                }

                return;
            }
            catch
            {
                if (attempt > 0)
                {
                    throw;
                }

                await Task.Delay(1);
            }
        }
    }

    private static DomainException Validation(string message) => new(ApiCodes.ValidationError, message);
}
