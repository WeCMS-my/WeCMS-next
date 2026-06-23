using Microsoft.AspNetCore.Http;
using WeCms.Shared;

namespace WeCms.Modules.FileCenter.Files;

public sealed class FileUploadContent : IAsyncDisposable
{
    private readonly MemoryStream _content;

    private FileUploadContent(MemoryStream content)
    {
        _content = content;
    }

    public Stream Content => _content;
    public long SizeBytes => _content.Length;

    public static async Task<FileUploadContent> ReadAsync(IFormFile file, long maxSizeBytes, CancellationToken cancellationToken)
    {
        if (file is null || file.Length <= 0)
        {
            throw Validation("file is required and must not be empty.");
        }

        await using var source = file.OpenReadStream();
        var content = new MemoryStream(file.Length <= int.MaxValue ? (int)file.Length : 0);
        var buffer = new byte[8192];
        var totalBytes = 0L;
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

                await content.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            }

            if (totalBytes <= 0)
            {
                throw Validation("file is required and must not be empty.");
            }

            content.Position = 0;
            return new FileUploadContent(content);
        }
        catch
        {
            await content.DisposeAsync();
            throw;
        }
    }

    public void Rewind()
    {
        _content.Position = 0;
    }

    public ValueTask DisposeAsync()
    {
        return _content.DisposeAsync();
    }

    private static DomainException Validation(string message) => new(ApiCodes.ValidationError, message);
}
