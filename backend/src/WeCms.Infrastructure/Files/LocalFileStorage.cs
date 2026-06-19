using System.Security.Cryptography;
using WeCms.Shared;

namespace WeCms.Infrastructure.Files;

public sealed class LocalFileStorage : IFileStorage
{
    private readonly string _basePath;

    public LocalFileStorage()
        : this(Path.Combine(AppContext.BaseDirectory, "storage", "files"))
    {
    }

    public LocalFileStorage(string basePath)
    {
        if (string.IsNullOrWhiteSpace(basePath))
        {
            throw new InvalidOperationException("FileStorage:Local:BasePath must be configured.");
        }

        _basePath = Path.GetFullPath(basePath);
        Directory.CreateDirectory(_basePath);
    }

    public async Task<FileStorageResult> StoreAsync(Stream source, string objectKey, string fileExt, long maxSizeBytes, CancellationToken cancellationToken)
    {
        var relativePath = ValidateObjectKey(objectKey);
        var fullPath = Path.GetFullPath(Path.Combine(_basePath, relativePath));
        if (!IsUnderBasePath(fullPath))
        {
            throw new DomainException(ApiCodes.ValidationError, "file object key is invalid.");
        }

        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var sha256 = SHA256.Create();
        var header = new byte[16];
        var headerLength = 0;
        var totalBytes = 0L;
        var isTextCandidate = true;
        await using var fileStream = File.Create(fullPath);

        var buffer = new byte[8192];
        try
        {
            int read;
            while ((read = await source.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken)) > 0)
            {
                totalBytes += read;
                if (totalBytes > maxSizeBytes)
                {
                    throw new DomainException(ApiCodes.ValidationError, $"file size must not exceed {maxSizeBytes} bytes.");
                }

                await fileStream.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
                sha256.TransformBlock(buffer, 0, read, null, 0);
                if (headerLength < header.Length)
                {
                    var headCount = Math.Min(read, header.Length - headerLength);
                    Buffer.BlockCopy(buffer, 0, header, headerLength, headCount);
                    headerLength += headCount;
                }

                if (isTextCandidate)
                {
                    isTextCandidate = LooksLikeText(buffer, read);
                }
            }

            sha256.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        }
        catch
        {
            fileStream.Close();
            File.Delete(fullPath);
            throw;
        }

        var mimeType = DetectMimeType(header, headerLength, fileExt, isTextCandidate);
        var hash = ToHex(sha256.Hash ?? []);

        return new FileStorageResult(totalBytes, hash, mimeType);
    }

    public Task<Stream> OpenReadAsync(string objectKey, CancellationToken cancellationToken)
    {
        var relativePath = ValidateObjectKey(objectKey);
        var fullPath = Path.GetFullPath(Path.Combine(_basePath, relativePath));
        if (!IsUnderBasePath(fullPath))
        {
            throw new DomainException(ApiCodes.ValidationError, "file object key is invalid.");
        }

        if (!File.Exists(fullPath))
        {
            throw new DomainException(ApiCodes.NotFound, "file was not found.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult<Stream>(File.OpenRead(fullPath));
    }

    public Task DeleteAsync(string objectKey, CancellationToken cancellationToken)
    {
        var relativePath = ValidateObjectKey(objectKey);
        var fullPath = Path.GetFullPath(Path.Combine(_basePath, relativePath));
        if (!IsUnderBasePath(fullPath))
        {
            return Task.FromException(new DomainException(ApiCodes.ValidationError, "file object key is invalid."));
        }

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string objectKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var fullPath = FullPath(objectKey);
        return Task.FromResult(File.Exists(fullPath));
    }

    public Task<FileStorageMetadata> GetMetadataAsync(string objectKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var fullPath = FullPath(objectKey);
        if (!File.Exists(fullPath))
        {
            throw new DomainException(ApiCodes.NotFound, "file was not found.");
        }

        var info = new FileInfo(fullPath);
        return Task.FromResult(new FileStorageMetadata(info.Length, null, info.LastWriteTimeUtc));
    }

    private string FullPath(string objectKey)
    {
        var relativePath = ValidateObjectKey(objectKey);
        var fullPath = Path.GetFullPath(Path.Combine(_basePath, relativePath));
        if (!IsUnderBasePath(fullPath))
        {
            throw new DomainException(ApiCodes.ValidationError, "file object key is invalid.");
        }

        return fullPath;
    }

    private static string ValidateObjectKey(string objectKey)
    {
        if (string.IsNullOrWhiteSpace(objectKey))
        {
            throw new DomainException(ApiCodes.ValidationError, "file object key is required.");
        }

        if (objectKey.Contains("..", StringComparison.Ordinal))
        {
            throw new DomainException(ApiCodes.ValidationError, "file object key is invalid.");
        }

        return objectKey.Replace('/', Path.DirectorySeparatorChar);
    }

    private static string DetectMimeType(byte[] header, int headerLength, string fileExt, bool isTextCandidate)
    {
        if (headerLength >= 3 &&
            header[0] == 0xFF &&
            header[1] == 0xD8 &&
            header[2] == 0xFF)
        {
            return "image/jpeg";
        }

        if (headerLength >= 8 &&
            header[0] == 0x89 &&
            header[1] == 0x50 &&
            header[2] == 0x4E &&
            header[3] == 0x47 &&
            header[4] == 0x0D &&
            header[5] == 0x0A &&
            header[6] == 0x1A &&
            header[7] == 0x0A)
        {
            return "image/png";
        }

        if (headerLength >= 12 &&
            header[0] == 0x52 &&
            header[1] == 0x49 &&
            header[2] == 0x46 &&
            header[3] == 0x46 &&
            header[8] == 0x57 &&
            header[9] == 0x45 &&
            header[10] == 0x42 &&
            header[11] == 0x50)
        {
            return "image/webp";
        }

        if (headerLength >= 5 && header[0] == 0x25 && header[1] == 0x50 && header[2] == 0x44 && header[3] == 0x46 && header[4] == 0x2D)
        {
            return "application/pdf";
        }

        if (string.Equals(fileExt, ".txt", StringComparison.OrdinalIgnoreCase) && isTextCandidate)
        {
            return "text/plain";
        }

        return "application/octet-stream";
    }

    private static bool LooksLikeText(byte[] buffer, int count)
    {
        for (var index = 0; index < count; index++)
        {
            var value = buffer[index];
            if (value < 0x20 && value is not (0x09 or 0x0A or 0x0D))
            {
                return false;
            }
        }

        return true;
    }

    private bool IsUnderBasePath(string fullPath)
    {
        var basePathWithSeparator = _basePath.EndsWith(Path.DirectorySeparatorChar)
            ? _basePath
            : _basePath + Path.DirectorySeparatorChar;

        return fullPath.StartsWith(basePathWithSeparator, StringComparison.OrdinalIgnoreCase);
    }

    private static string ToHex(byte[] bytes)
    {
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}

public sealed class NoopFileScanService : IFileScanService
{
    public Task<FileScanResult> ScanAsync(Stream source, FileScanRequest request, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(FileScanResult.CleanResult);
    }
}
