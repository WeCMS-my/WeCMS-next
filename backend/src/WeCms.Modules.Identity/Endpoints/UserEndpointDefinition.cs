using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using WeCms.Modules.Identity.Permissions;
using WeCms.Shared;
using WeCms.Shared.Endpoints;

namespace WeCms.Modules.Identity.Endpoints;

public sealed class UserEndpointDefinition : IEndpointDefinition
{
    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var group = endpoints.MapGroup("/api/v1/system/users")
            .WithEndpointModule("identity")
            .AuditWriteEndpoints("identity", "users")
            .RequireAuthorization();

        group.MapGet("", ListAsync).RequireEndpointPermission(IdentityUserPermissions.List);
        group.MapGet("/{id:long}", DetailAsync).RequireEndpointPermission(IdentityUserPermissions.Detail);
        group.MapPost("", CreateAsync).RequireEndpointPermission(IdentityUserPermissions.Create).RequireRateLimiting(IdentityEndpointRateLimitPolicyNames.AdminWrite);
        group.MapPut("/{id:long}", UpdateAsync).RequireEndpointPermission(IdentityUserPermissions.Update).RequireRateLimiting(IdentityEndpointRateLimitPolicyNames.AdminWrite);
        group.MapDelete("/{id:long}", DeleteAsync).RequireEndpointPermission(IdentityUserPermissions.Delete).RequireRateLimiting(IdentityEndpointRateLimitPolicyNames.AdminWrite);
        group.MapPost("/{id:long}/enable", EnableAsync).RequireEndpointPermission(IdentityUserPermissions.Enable).RequireRateLimiting(IdentityEndpointRateLimitPolicyNames.AdminWrite);
        group.MapPost("/{id:long}/disable", DisableAsync).RequireEndpointPermission(IdentityUserPermissions.Disable).RequireRateLimiting(IdentityEndpointRateLimitPolicyNames.AdminWrite);
        group.MapPost("/{id:long}/reset-password", ResetPasswordAsync).RequireEndpointPermission(IdentityUserPermissions.ResetPassword).RequireRateLimiting(IdentityEndpointRateLimitPolicyNames.AdminWrite);
        group.MapPost("/{id:long}/reset-2fa", ResetTwoFactorAsync).RequireEndpointPermission(IdentityUserPermissions.ResetTwoFactor).RequireRateLimiting(IdentityEndpointRateLimitPolicyNames.AdminWrite);
        group.MapPut("/{id:long}/roles", AssignRolesAsync).RequireEndpointPermission(IdentityUserPermissions.AssignRole).RequireRateLimiting(IdentityEndpointRateLimitPolicyNames.AdminWrite);
        group.MapPut("/{id:long}/positions", AssignPositionsAsync).RequireEndpointPermission(IdentityUserPermissions.AssignPosition).RequireRateLimiting(IdentityEndpointRateLimitPolicyNames.AdminWrite);
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

    private static async Task<ApiResult<object>> ResetTwoFactorAsync(
        long id,
        ResetUserTwoFactorRequest request,
        HttpContext httpContext,
        IUserService service,
        IAuthClock clock,
        CancellationToken cancellationToken)
    {
        await service.ResetTwoFactorAsync(id, request, Context(httpContext, clock), cancellationToken);
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

    private static async Task<ApiResult<object>> AssignPositionsAsync(
        long id,
        AssignUserPositionsRequest request,
        HttpContext httpContext,
        IUserService service,
        IAuthClock clock,
        CancellationToken cancellationToken)
    {
        await service.AssignPositionsAsync(id, request, Context(httpContext, clock), cancellationToken);
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
