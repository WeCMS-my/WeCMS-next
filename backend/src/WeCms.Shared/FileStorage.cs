using System.IO;

namespace WeCms.Shared;

public interface IFileStorage
{
    Task<FileStorageResult> StoreAsync(Stream source, string objectKey, string fileExt, long maxSizeBytes, CancellationToken cancellationToken);
    Task<Stream> OpenReadAsync(string objectKey, CancellationToken cancellationToken);
    Task DeleteAsync(string objectKey, CancellationToken cancellationToken);
    Task<bool> ExistsAsync(string objectKey, CancellationToken cancellationToken);
    Task<FileStorageMetadata> GetMetadataAsync(string objectKey, CancellationToken cancellationToken);
}

public sealed record FileStorageResult(long SizeBytes, string Sha256, string MimeType);

public sealed record FileStorageMetadata(long SizeBytes, string? MimeType, DateTimeOffset? LastModified);

public interface IFileScanService
{
    Task<FileScanResult> ScanAsync(Stream source, FileScanRequest request, CancellationToken cancellationToken);
}

public sealed record FileScanRequest(string OriginalName, string MimeType, long SizeBytes, string PolicyCode);

public sealed record FileScanResult(bool Clean, string? Reason)
{
    public static FileScanResult CleanResult { get; } = new(true, null);
}
