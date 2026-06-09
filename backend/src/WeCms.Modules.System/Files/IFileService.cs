namespace WeCms.Modules.System.Files;

public interface IFileService
{
    Task<(IReadOnlyList<FileItem> Items, long Total)> ListAsync(int page, int size, CancellationToken ct);
    Task<UploadResult> UploadAsync(string fileName, Stream stream, string contentType, CancellationToken ct);
    Task<FileDownloadInfo?> GetDownloadInfoAsync(long id, CancellationToken ct);
    Task DeleteAsync(long id, CancellationToken ct);
}
