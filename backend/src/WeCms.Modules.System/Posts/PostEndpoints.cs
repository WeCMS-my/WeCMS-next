using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WeCms.Modules.System.Auth;
using WeCms.Modules.System.Permissions;
using WeCms.Shared;

namespace WeCms.Modules.System.Posts;

public static class PostEndpoints
{
    public static IEndpointRouteBuilder MapPostEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/system/posts")
            .RequireAuthorization();

        group.MapGet("", ListAsync).RequirePermission(PostPermissions.List);
        group.MapGet("/{id:long}", DetailAsync).RequirePermission(PostPermissions.Detail);
        group.MapPost("", CreateAsync).RequirePermission(PostPermissions.Create);
        group.MapPut("/{id:long}", UpdateAsync).RequirePermission(PostPermissions.Update);
        group.MapDelete("/{id:long}", DeleteAsync).RequirePermission(PostPermissions.Delete);
        group.MapPost("/{id:long}/enable", EnableAsync).RequirePermission(PostPermissions.Enable);
        group.MapPost("/{id:long}/disable", DisableAsync).RequirePermission(PostPermissions.Disable);

        return endpoints;
    }

    private static async Task<ApiResult<PagedResult<PostSummaryDto>>> ListAsync(int page, int pageSize, string? keyword, string? status, IPostService service, CancellationToken cancellationToken)
    {
        return ApiResult<PagedResult<PostSummaryDto>>.Ok(await service.ListAsync(new PostListQuery(page, pageSize, keyword, status), cancellationToken));
    }

    private static async Task<ApiResult<PostDetailDto>> DetailAsync(long id, IPostService service, CancellationToken cancellationToken)
    {
        return ApiResult<PostDetailDto>.Ok(await service.GetAsync(id, cancellationToken));
    }

    private static async Task<ApiResult<PostMutationResponse>> CreateAsync(CreatePostRequest request, HttpContext httpContext, IPostService service, IAuthClock clock, CancellationToken cancellationToken)
    {
        return ApiResult<PostMutationResponse>.Ok(await service.CreateAsync(request, Context(httpContext, clock), cancellationToken));
    }

    private static async Task<ApiResult<PostMutationResponse>> UpdateAsync(long id, UpdatePostRequest request, HttpContext httpContext, IPostService service, IAuthClock clock, CancellationToken cancellationToken)
    {
        return ApiResult<PostMutationResponse>.Ok(await service.UpdateAsync(id, request, Context(httpContext, clock), cancellationToken));
    }

    private static async Task<ApiResult<object>> DeleteAsync(long id, HttpContext httpContext, IPostService service, IAuthClock clock, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, Context(httpContext, clock), cancellationToken);
        return ApiResult<object>.Ok(new { });
    }

    private static async Task<ApiResult<object>> EnableAsync(long id, HttpContext httpContext, IPostService service, IAuthClock clock, CancellationToken cancellationToken)
    {
        await service.EnableAsync(id, Context(httpContext, clock), cancellationToken);
        return ApiResult<object>.Ok(new { });
    }

    private static async Task<ApiResult<object>> DisableAsync(long id, HttpContext httpContext, IPostService service, IAuthClock clock, CancellationToken cancellationToken)
    {
        await service.DisableAsync(id, Context(httpContext, clock), cancellationToken);
        return ApiResult<object>.Ok(new { });
    }

    private static PostRequestContext Context(HttpContext httpContext, IAuthClock clock)
    {
        var userIdText = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(userIdText, out var userId))
        {
            throw new DomainException(ApiCodes.Unauthorized, "Authentication is required.");
        }

        return new PostRequestContext(userId, httpContext.User.Identity?.Name ?? string.Empty, httpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty, httpContext.Request.Headers.UserAgent.ToString(), httpContext.TraceIdentifier, clock.UtcNow);
    }
}
