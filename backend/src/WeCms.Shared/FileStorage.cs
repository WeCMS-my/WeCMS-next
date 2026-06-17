using System.IO;

namespace WeCms.Shared;

public interface IFileStorage
{
    Task<FileStorageResult> StoreAsync(Stream source, string objectKey, string fileExt, long maxSizeBytes, CancellationToken cancellationToken);
    Task<Stream> OpenReadAsync(string objectKey, CancellationToken cancellationToken);
    Task DeleteAsync(string objectKey, CancellationToken cancellationToken);
}

public sealed record FileStorageResult(long SizeBytes, string Sha256, string MimeType);
