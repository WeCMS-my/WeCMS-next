using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WeCms.Modules.System.Auth;
using WeCms.Modules.System.Permissions;
using WeCms.Shared;

namespace WeCms.Modules.System.Users;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/system/users")
            .RequireAuthorization();

        group.MapGet("", ListAsync).RequirePermission(UserPermissions.List);
        group.MapGet("/{id:long}", DetailAsync).RequirePermission(UserPermissions.Detail);
        group.MapPost("", CreateAsync).RequirePermission(UserPermissions.Create);
        group.MapPut("/{id:long}", UpdateAsync).RequirePermission(UserPermissions.Update);
        group.MapDelete("/{id:long}", DeleteAsync).RequirePermission(UserPermissions.Delete);
        group.MapPost("/{id:long}/enable", EnableAsync).RequirePermission(UserPermissions.Enable);
        group.MapPost("/{id:long}/disable", DisableAsync).RequirePermission(UserPermissions.Disable);
        group.MapPost("/{id:long}/reset-password", ResetPasswordAsync).RequirePermission(UserPermissions.ResetPassword);
        group.MapPut("/{id:long}/roles", AssignRolesAsync).RequirePermission(UserPermissions.AssignRole);
        group.MapPut("/{id:long}/posts", AssignPostsAsync).RequirePermission(UserPermissions.AssignPost);

        return endpoints;
    }

    private static async Task<ApiResult<PagedResult<UserSummaryDto>>> ListAsync(
        int page,
        int pageSize,
        string? keyword,
        string? status,
        long? deptId,
        IUserService service,
        CancellationToken cancellationToken)
    {
        var result = await service.ListAsync(new UserListQuery(page, pageSize, keyword, status, deptId), cancellationToken);
        return ApiResult<PagedResult<UserSummaryDto>>.Ok(result);
    }

    private static async Task<ApiResult<UserDetailDto>> DetailAsync(long id, IUserService service, CancellationToken cancellationToken)
    {
        return ApiResult<UserDetailDto>.Ok(await service.GetAsync(id, cancellationToken));
    }

    private static async Task<ApiResult<UserMutationResponse>> CreateAsync(
        CreateUserRequest request,
        HttpContext httpContext,
        IUserService service,
        IAuthClock clock,
        CancellationToken cancellationToken)
    {
        return ApiResult<UserMutationResponse>.Ok(await service.CreateAsync(request, Context(httpContext, clock), cancellationToken));
    }

    private static async Task<ApiResult<UserMutationResponse>> UpdateAsync(
        long id,
        UpdateUserRequest request,
        HttpContext httpContext,
        IUserService service,
        IAuthClock clock,
        CancellationToken cancellationToken)
    {
        return ApiResult<UserMutationResponse>.Ok(await service.UpdateAsync(id, request, Context(httpContext, clock), cancellationToken));
    }

    private static async Task<ApiResult<object>> DeleteAsync(long id, HttpContext httpContext, IUserService service, IAuthClock clock, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, Context(httpContext, clock), cancellationToken);
        return ApiResult<object>.Ok(new { });
    }

    private static async Task<ApiResult<object>> EnableAsync(long id, HttpContext httpContext, IUserService service, IAuthClock clock, CancellationToken cancellationToken)
    {
        await service.EnableAsync(id, Context(httpContext, clock), cancellationToken);
        return ApiResult<object>.Ok(new { });
    }

    private static async Task<ApiResult<object>> DisableAsync(long id, HttpContext httpContext, IUserService service, IAuthClock clock, CancellationToken cancellationToken)
    {
        await service.DisableAsync(id, Context(httpContext, clock), cancellationToken);
        return ApiResult<object>.Ok(new { });
    }

    private static async Task<ApiResult<object>> ResetPasswordAsync(
        long id,
        ResetUserPasswordRequest request,
        HttpContext httpContext,
        IUserService service,
        IAuthClock clock,
        CancellationToken cancellationToken)
    {
        await service.ResetPasswordAsync(id, request, Context(httpContext, clock), cancellationToken);
        return ApiResult<object>.Ok(new { });
    }

    private static async Task<ApiResult<object>> AssignRolesAsync(
        long id,
        AssignUserRolesRequest request,
        HttpContext httpContext,
        IUserService service,
        IAuthClock clock,
        CancellationToken cancellationToken)
    {
        await service.AssignRolesAsync(id, request, Context(httpContext, clock), cancellationToken);
        return ApiResult<object>.Ok(new { });
    }

    private static async Task<ApiResult<object>> AssignPostsAsync(
        long id,
        AssignUserPostsRequest request,
        HttpContext httpContext,
        IUserService service,
        IAuthClock clock,
        CancellationToken cancellationToken)
    {
        await service.AssignPostsAsync(id, request, Context(httpContext, clock), cancellationToken);
        return ApiResult<object>.Ok(new { });
    }

    private static UserRequestContext Context(HttpContext httpContext, IAuthClock clock)
    {
        var userIdText = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(userIdText, out var userId))
        {
            throw new DomainException(ApiCodes.Unauthorized, "Authentication is required.");
        }

        return new UserRequestContext(
            userId,
            httpContext.User.Identity?.Name ?? string.Empty,
            httpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
            httpContext.Request.Headers.UserAgent.ToString(),
            httpContext.TraceIdentifier,
            clock.UtcNow);
    }
}
