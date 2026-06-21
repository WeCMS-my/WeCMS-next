using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
using WeCms.Modules.FileCenter;
using WeCms.Shared;
using WeCms.Shared.Endpoints;
using WeCms.Shared.Security;

namespace WeCms.Modules.FileCenter.Files;

public static class FileEndpoints
{
    public static IEndpointRouteBuilder MapFileEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/system")
            .WithEndpointModule("file-center")
            .AuditWriteEndpoints("file-center", "files")
            .RequireAuthorization();

        group.MapPost("/files", CreateAsync)
            .DisableAntiforgery()
            .RequireEndpointPermission(FilePermissions.Upload)
            .RequireRateLimiting(RateLimitPolicyNames.FileUpload);
        group.MapGet("/files", ListAsync).RequireEndpointPermission(FilePermissions.List);
        group.MapGet("/files/{id:long}", DetailAsync).RequireEndpointPermission(FilePermissions.Detail);
        group.MapGet("/files/{id:long}/download", DownloadAsync).RequireEndpointPermission(FilePermissions.Download);
        group.MapGet("/files/{id:long}/preview", PreviewAsync).RequireEndpointPermission(FilePermissions.Download);
        var deleteEndpoint = group.MapDelete("/files/{id:long}", DeleteAsync).RequireEndpointPermission(FilePermissions.Delete);
        deleteEndpoint.RequireRateLimiting(RateLimitPolicyNames.AdminWrite);

        return endpoints;
    }

    private static async Task<ApiResult<PagedResult<FileSummaryDto>>> ListAsync(int page, int pageSize, string? keyword, string? mimeType, string? status, IFileService service, CancellationToken cancellationToken)
    {
        return ApiResult<PagedResult<FileSummaryDto>>.Ok(await service.ListAsync(new FileListQuery(page, pageSize, keyword, mimeType, status), cancellationToken));
    }

    private static async Task<ApiResult<FileDetailDto>> DetailAsync(long id, IFileService service, CancellationToken cancellationToken)
    {
        return ApiResult<FileDetailDto>.Ok(await service.GetAsync(id, cancellationToken));
    }

    private static async Task<ApiResult<FileMutationResponse>> CreateAsync([FromForm] CreateFileRequest request, [FromForm] IFormFile file, HttpContext httpContext, IFileService service, IFileCenterClock clock, CancellationToken cancellationToken)
    {
        return ApiResult<FileMutationResponse>.Ok(await service.CreateAsync(request, file, Context(httpContext, clock), cancellationToken));
    }

    private static async Task<ApiResult<object>> DeleteAsync(long id, HttpContext httpContext, IFileService service, IFileCenterClock clock, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, Context(httpContext, clock), cancellationToken);
        return ApiResult<object>.Ok(new { });
    }

    private static async Task<IResult> DownloadAsync(long id, HttpContext httpContext, IFileService service, IFileCenterClock clock, CancellationToken cancellationToken)
    {
        return await FileTransferAsync(id, false, httpContext, service, clock, cancellationToken);
    }

    private static async Task<IResult> PreviewAsync(long id, HttpContext httpContext, IFileService service, IFileCenterClock clock, CancellationToken cancellationToken)
    {
        return await FileTransferAsync(id, true, httpContext, service, clock, cancellationToken);
    }

    private static async Task<IResult> FileTransferAsync(long id, bool inline, HttpContext httpContext, IFileService service, IFileCenterClock clock, CancellationToken cancellationToken)
    {
        var payload = await service.GetDownloadPayloadAsync(id, inline, Context(httpContext, clock), cancellationToken);
        httpContext.Response.Headers.XContentTypeOptions = "nosniff";
        if (payload.Inline)
        {
            var contentDisposition = new ContentDispositionHeaderValue("inline")
            {
                FileNameStar = payload.FileName
            };
            httpContext.Response.Headers.ContentDisposition = contentDisposition.ToString();
            return Results.File(payload.Content, payload.ContentType, enableRangeProcessing: true);
        }

        return Results.File(payload.Content, payload.ContentType, payload.FileName);
    }

    private static FileRequestContext Context(HttpContext httpContext, IFileCenterClock clock)
    {
        var userIdText = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(userIdText, out var userId))
        {
            throw new DomainException(ApiCodes.Unauthorized, "Authentication is required.");
        }

        return new FileRequestContext(userId, httpContext.User.Identity?.Name ?? string.Empty, httpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty, httpContext.Request.Headers.UserAgent.ToString(), httpContext.TraceIdentifier, clock.UtcNow);
    }
}
