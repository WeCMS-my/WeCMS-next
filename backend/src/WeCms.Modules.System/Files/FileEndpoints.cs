using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WeCms.Modules.System.Auth;
using WeCms.Modules.System.Permissions;
using WeCms.Shared;

namespace WeCms.Modules.System.Files;

public static class FileEndpoints
{
    public static IEndpointRouteBuilder MapFileEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/system")
            .RequireAuthorization();

        group.MapGet("/files", ListAsync).RequirePermission(FilePermissions.List);
        group.MapGet("/files/{id:long}", DetailAsync).RequirePermission(FilePermissions.Detail);
        group.MapPost("/files", CreateAsync).RequirePermission(FilePermissions.Upload);
        group.MapDelete("/files/{id:long}", DeleteAsync).RequirePermission(FilePermissions.Delete);

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

    private static async Task<ApiResult<FileMutationResponse>> CreateAsync(CreateFileRequest request, HttpContext httpContext, IFileService service, IAuthClock clock, CancellationToken cancellationToken)
    {
        return ApiResult<FileMutationResponse>.Ok(await service.CreateAsync(request, Context(httpContext, clock), cancellationToken));
    }

    private static async Task<ApiResult<object>> DeleteAsync(long id, HttpContext httpContext, IFileService service, IAuthClock clock, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, Context(httpContext, clock), cancellationToken);
        return ApiResult<object>.Ok(new { });
    }

    private static FileRequestContext Context(HttpContext httpContext, IAuthClock clock)
    {
        var userIdText = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(userIdText, out var userId))
        {
            throw new DomainException(ApiCodes.Unauthorized, "Authentication is required.");
        }

        return new FileRequestContext(userId, httpContext.User.Identity?.Name ?? string.Empty, httpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty, httpContext.Request.Headers.UserAgent.ToString(), httpContext.TraceIdentifier, clock.UtcNow);
    }
}
