using Microsoft.AspNetCore.Http;

namespace WeCms.Modules.Identity.Services;

public sealed record AccountAvatarStoredFile(string ObjectKey, string MimeType, string FileExtension);

public sealed record AccountAvatarDownload(Stream Content, string MimeType, string FileName, long SizeBytes, bool OwnsStream);

public interface IAccountAvatarFileService
{
    Task<AccountAvatarStoredFile> StoreAsync(
        AccountAvatarUploadRequest request,
        IFormFile file,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    Task<AccountAvatarDownload> OpenAsync(
        string objectKey,
        string mimeType,
        string fileExtension,
        CancellationToken cancellationToken);

    Task DeleteAsync(string objectKey, CancellationToken cancellationToken);
}
