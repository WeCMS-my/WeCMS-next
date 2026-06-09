 using WeCms.Shared;
 
 namespace WeCms.Modules.System.Files;
 
 public static class FileEndpoints
 {
     public static RouteGroupBuilder MapFileEndpoints(this RouteGroupBuilder group)
     {
         group.MapGet("/system/files", ListAsync).RequirePermission("sys:file:list");
         group.MapPost("/system/files/upload", UploadAsync).RequirePermission("sys:file:upload").DisableAntiforgery();
         group.MapGet("/system/files/{id:long}/download", DownloadAsync).RequirePermission("sys:file:download");
         group.MapDelete("/system/files/{id:long}", DeleteAsync).RequirePermission("sys:file:delete");
         return group;
     }
     private static async Task<IResult> ListAsync(HttpContext ctx, IFileService svc, CancellationToken ct)
     { var p = int.TryParse(ctx.Request.Query["page"], out var pp) ? Math.Max(pp, 1) : 1; var s = int.TryParse(ctx.Request.Query["pageSize"], out var ps) ? Math.Clamp(ps, 1, 100) : 20; var (items, total) = await svc.ListAsync(p, s, ct); return Results.Ok(ApiResult<PagedResult<FileItem>>.Ok(new PagedResult<FileItem>(items, p, s, total))); }
     private static async Task<IResult> UploadAsync(HttpRequest request, IFileService svc, CancellationToken ct)
     { var file = request.Form.Files.FirstOrDefault(); if (file is null) return Results.Ok(ApiResult<UploadResult>.Fail(ApiCodes.ValidationError, "No file")); using var s = file.OpenReadStream(); var r = await svc.UploadAsync(file.FileName, s, file.ContentType, ct); return Results.Ok(ApiResult<UploadResult>.Ok(r)); }
     private static async Task<IResult> DownloadAsync(long id, IFileService svc, CancellationToken ct)
     { var info = await svc.GetDownloadInfoAsync(id, ct); return info.HasValue ? Results.File(info.Value.Path, info.Value.MimeType, info.Value.FileName) : Results.Ok(ApiResult<string>.Fail(ApiCodes.NotFound, "File not found")); }
     private static async Task<IResult> DeleteAsync(long id, IFileService svc, CancellationToken ct)
     { await svc.DeleteAsync(id, ct); return Results.Ok(ApiResult<string>.Ok("deleted")); }
 }
