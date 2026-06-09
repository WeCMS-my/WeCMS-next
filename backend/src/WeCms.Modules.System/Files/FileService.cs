 using WeCms.Shared.Contracts;
 
 namespace WeCms.Modules.System.Files;
 
 public sealed class FileService(IDbConnectionFactory db)
 {
     private const long MaxFileSize = 50_000_000;
     private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase) { ".jpg",".jpeg",".png",".gif",".webp",".svg",".pdf",".doc",".docx",".xls",".xlsx",".ppt",".pptx",".txt",".csv",".json",".xml",".zip",".rar",".mp3",".mp4",".avi" };
 
     public async Task<(IReadOnlyList<FileItem> Items, long Total)> ListAsync(int page, int size, CancellationToken ct)
     { await using var c = await db.OpenAsync(ct); var items = await c.QueryAsync<FileItem>(new CommandDefinition("SELECT id, original_name, size, mime_type, extension, created_at FROM sys_file WHERE deleted_at IS NULL ORDER BY id DESC LIMIT @L OFFSET @O", new { L = Math.Min(size,100), O = (page-1)*size }, cancellationToken: ct)); var total = await c.ExecuteScalarAsync<long>(new CommandDefinition("SELECT COUNT(1) FROM sys_file WHERE deleted_at IS NULL", cancellationToken: ct)); return (items.AsList(), total); }
 
     public async Task<UploadResult> UploadAsync(string fileName, Stream stream, string contentType, CancellationToken ct)
     {
         var ext = Path.GetExtension(fileName).ToLowerInvariant();
         if (!AllowedExtensions.Contains(ext)) throw new InvalidOperationException("File type not allowed");
         if (stream.Length > MaxFileSize) throw new InvalidOperationException("File too large (max 50MB)");
         if (!IsMimeMatch(ext, contentType)) throw new InvalidOperationException("MIME type mismatch");
         var storageName = $"{Guid.NewGuid():N}{ext}";
         var dir = Path.Combine("storage", "files", DateTime.UtcNow.ToString("yyyy/MM"));
         Directory.CreateDirectory(dir);
         var path = Path.Combine(dir, storageName);
         await using var fs = File.Create(path); await stream.CopyToAsync(fs, ct);
         await using var c = await db.OpenAsync(ct);
         var id = await c.ExecuteScalarAsync<long>(new CommandDefinition(
             "INSERT INTO sys_file (original_name,storage_name,storage_path,size,mime_type,extension,created_at,updated_at) VALUES (@O,@S,@P,@Z,@M,@E,@Now,@Now); SELECT LAST_INSERT_ID();",
             new { O = fileName, S = storageName, P = path, Z = stream.Length, M = contentType, E = ext, Now = DateTime.UtcNow }, cancellationToken: ct));
         return new UploadResult(id, fileName, stream.Length);
     }
 
     public async Task<(string Path, string MimeType, string FileName)?> GetDownloadInfoAsync(long id, CancellationToken ct)
     { await using var c = await db.OpenAsync(ct); return await c.QueryFirstOrDefaultAsync<(string, string, string)?>(new CommandDefinition("SELECT storage_path, mime_type, original_name FROM sys_file WHERE id=@Id AND deleted_at IS NULL", new { Id = id }, cancellationToken: ct)); }
 
     public async Task DeleteAsync(long id, CancellationToken ct)
     { await using var c = await db.OpenAsync(ct); await c.ExecuteAsync(new CommandDefinition("UPDATE sys_file SET deleted_at=@Now WHERE id=@Id", new { Now = DateTime.UtcNow, Id = id }, cancellationToken: ct)); }
 
     private static bool IsMimeMatch(string ext, string mime) => ext switch
     { ".jpg" or ".jpeg" => mime.StartsWith("image/jpeg"), ".png" => mime == "image/png", ".gif" => mime == "image/gif", ".webp" => mime == "image/webp", ".pdf" => mime == "application/pdf", _ => true };
 }
