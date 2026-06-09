 namespace WeCms.Modules.System.Files;
 
 public sealed record FileItem(long Id, string OriginalName, long Size, string MimeType, string Extension, DateTime CreatedAt);
 public sealed record UploadResult(long Id, string OriginalName, long Size);
