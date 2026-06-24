using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using WeCms.Shared;

namespace WeCms.Modules.FileCenter.Files;

public sealed class FileUploadContent : IAsyncDisposable
{
    private const string TempStorageUnavailableMessage = "Upload temporary storage is unavailable.";
    private const string TempFilePrefix = "wecms-upload-";
    private readonly Stream _content;
    private readonly FileUploadOptions _options;
    private readonly string? _tempFilePath;

    private FileUploadContent(Stream content, long sizeBytes, FileUploadOptions options, string? tempFilePath = null)
    {
        _content = content;
        _options = options;
        SizeBytes = sizeBytes;
        _tempFilePath = tempFilePath;
    }

    public Stream Content => _content;
    public long SizeBytes { get; }

    public static async Task<FileUploadContent> ReadAsync(IFormFile file, long maxSizeBytes, CancellationToken cancellationToken)
    {
        return await ReadAsync(file, maxSizeBytes, FileUploadOptions.Default, cancellationToken);
    }

    public static async Task<FileUploadContent> ReadAsync(
        IFormFile file,
        long maxSizeBytes,
        FileUploadOptions options,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length <= 0)
        {
            throw Validation("file is required and must not be empty.");
        }

        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        await using var source = file.OpenReadStream();
        Stream content = new MemoryStream();
        var totalBytes = 0L;
        var buffer = new byte[options.ChunkSizeBytes];
        string? tempFilePath = null;
        string? tempDirectory = null;

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

                if (tempFilePath is null && totalBytes > options.MemoryFallbackThresholdBytes)
                {
                    tempDirectory ??= ResolveTempDirectory(options.TempFilePath);
                    ValidateTempStorageAvailability(tempDirectory, maxSizeBytes);
                    tempFilePath = Path.Combine(tempDirectory, $"{TempFilePrefix}{Path.GetRandomFileName()}");
                    FileStream tempFileStream;
                    try
                    {
                        tempFileStream = new FileStream(
                            tempFilePath,
                            FileMode.Create,
                            FileAccess.ReadWrite,
                            FileShare.None,
                            bufferSize: 81920,
                            useAsync: true);
                    }
                    catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
                    {
                        throw new DomainException(ApiCodes.ServiceUnavailable, $"{TempStorageUnavailableMessage} Unable to create temporary upload file.");
                    }

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
                return new FileUploadContent(content, totalBytes, options);
            }

            if (content is FileStream tempFile)
            {
                await tempFile.FlushAsync(cancellationToken);
            }

            return new FileUploadContent(content, totalBytes, options, tempFilePath);
        }
        catch
        {
            await content.DisposeAsync();
            if (tempFilePath is not null)
            {
                await DeleteTempFileAsync(tempFilePath, options.RetryCount, options.RetryDelayMilliseconds);
            }

            throw;
        }
    }

    public void Rewind()
    {
        _content.Position = 0;
    }

    public static void CleanupExpiredTempFiles(FileUploadOptions options, ILogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        var tempDirectory = ResolveTempDirectory(options.TempFilePath);

        if (!Directory.Exists(tempDirectory))
        {
            return;
        }

        var retentionCutoff = DateTimeOffset.UtcNow.AddHours(-options.TempFileRetentionHours).UtcDateTime;
        foreach (var tempFilePath in Directory.EnumerateFiles(tempDirectory, $"{TempFilePrefix}*"))
        {
            try
            {
                if (File.GetLastWriteTimeUtc(tempFilePath) > retentionCutoff)
                {
                    continue;
                }

                File.Delete(tempFilePath);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                logger?.LogWarning(exception, "Failed to delete stale upload temp file {TempFilePath}.", tempFilePath);
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _content.DisposeAsync();
        await DeleteTempFileAsync(_tempFilePath, _options.RetryCount, _options.RetryDelayMilliseconds);
    }

    private static string ResolveTempDirectory(string? tempFilePath)
    {
        var normalizedPath = string.IsNullOrWhiteSpace(tempFilePath)
            ? Path.Combine(Path.GetTempPath(), "wecms", "uploads")
            : tempFilePath;

        try
        {
            Directory.CreateDirectory(normalizedPath);
        }
        catch (Exception exception) when (IsTempStorageException(exception))
        {
            throw new DomainException(ApiCodes.ServiceUnavailable, $"{TempStorageUnavailableMessage} Unable to access temporary upload directory.");
        }

        return normalizedPath;
    }

    private static void ValidateTempStorageAvailability(string tempDirectory, long requiredBytes)
    {
        if (requiredBytes <= 0)
        {
            return;
        }

        try
        {
            var rootPath = Path.GetPathRoot(Path.GetFullPath(tempDirectory));
            if (string.IsNullOrWhiteSpace(rootPath))
            {
                return;
            }

            var drive = new DriveInfo(rootPath);
            if (!drive.IsReady || drive.AvailableFreeSpace >= requiredBytes)
            {
                return;
            }

            throw new DomainException(
                ApiCodes.ServiceUnavailable,
                $"{TempStorageUnavailableMessage} Insufficient free space in temporary upload directory.");
        }
        catch (DomainException)
        {
            throw;
        }
        catch (Exception exception) when (IsTempStorageException(exception))
        {
            throw new DomainException(ApiCodes.ServiceUnavailable, $"{TempStorageUnavailableMessage} Unable to inspect temporary upload directory.");
        }
    }

    private static bool IsTempStorageException(Exception exception)
    {
        return exception is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException;
    }

    private static async Task DeleteTempFileAsync(string? filePath, int retryCount, int retryDelayMilliseconds)
    {
        if (filePath is null)
        {
            return;
        }

        for (var attempt = 0; attempt <= retryCount; attempt++)
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
                if (attempt >= retryCount)
                {
                    throw;
                }

                if (retryDelayMilliseconds > 0)
                {
                    await Task.Delay(retryDelayMilliseconds);
                }
            }
        }
    }

    private static DomainException Validation(string message) => new(ApiCodes.ValidationError, message);
}
