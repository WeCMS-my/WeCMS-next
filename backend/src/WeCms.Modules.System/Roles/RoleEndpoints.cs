using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WeCms.Modules.System.Auth;
using WeCms.Modules.System.Permissions;
using WeCms.Shared;

namespace WeCms.Modules.System.Roles;

public static class RoleEndpoints
{
    public static IEndpointRouteBuilder MapRoleEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/system/roles")
            .RequireAuthorization();

        group.MapGet("", ListAsync).RequirePermission(RolePermissions.List);
        group.MapGet("/{id:long}", DetailAsync).RequirePermission(RolePermissions.Detail);
        group.MapPost("", CreateAsync).RequirePermission(RolePermissions.Create);
        group.MapPut("/{id:long}", UpdateAsync).RequirePermission(RolePermissions.Update);
        group.MapDelete("/{id:long}", DeleteAsync).RequirePermission(RolePermissions.Delete);
        group.MapPost("/{id:long}/enable", EnableAsync).RequirePermission(RolePermissions.Enable);
        group.MapPost("/{id:long}/disable", DisableAsync).RequirePermission(RolePermissions.Disable);
        group.MapPut("/{id:long}/permissions", AssignPermissionsAsync).RequirePermission(RolePermissions.AssignPermission);
        group.MapPut("/{id:long}/menus", AssignMenusAsync).RequirePermission(RolePermissions.AssignMenu);

        return endpoints;
    }

    private static async Task<ApiResult<PagedResult<RoleSummaryDto>>> ListAsync(
        int page,
        int pageSize,
        string? keyword,
        string? status,
        IRoleService service,
        CancellationToken cancellationToken)
    {
        var result = await service.ListAsync(new RoleListQuery(page, pageSize, keyword, status), cancellationToken);
        return ApiResult<PagedResult<RoleSummaryDto>>.Ok(result);
    }

    private static async Task<ApiResult<RoleDetailDto>> DetailAsync(long id, IRoleService service, CancellationToken cancellationToken)
    {
        return ApiResult<RoleDetailDto>.Ok(await service.GetAsync(id, cancellationToken));
    }

    private static async Task<ApiResult<RoleMutationResponse>> CreateAsync(
        CreateRoleRequest request,
        HttpContext httpContext,
        IRoleService service,
        IAuthClock clock,
        CancellationToken cancellationToken)
    {
        return ApiResult<RoleMutationResponse>.Ok(await service.CreateAsync(request, Context(httpContext, clock), cancellationToken));
    }

    private static async Task<ApiResult<RoleMutationResponse>> UpdateAsync(
        long id,
        UpdateRoleRequest request,
        HttpContext httpContext,
        IRoleService service,
        IAuthClock clock,
        CancellationToken cancellationToken)
    {
        return ApiResult<RoleMutationResponse>.Ok(await service.UpdateAsync(id, request, Context(httpContext, clock), cancellationToken));
    }

    private static async Task<ApiResult<object>> DeleteAsync(long id, HttpContext httpContext, IRoleService service, IAuthClock clock, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, Context(httpContext, clock), cancellationToken);
        return ApiResult<object>.Ok(new { });
    }

    private static async Task<ApiResult<object>> EnableAsync(long id, HttpContext httpContext, IRoleService service, IAuthClock clock, CancellationToken cancellationToken)
    {
        await service.EnableAsync(id, Context(httpContext, clock), cancellationToken);
        return ApiResult<object>.Ok(new { });
    }

    private static async Task<ApiResult<object>> DisableAsync(long id, HttpContext httpContext, IRoleService service, IAuthClock clock, CancellationToken cancellationToken)
    {
        await service.DisableAsync(id, Context(httpContext, clock), cancellationToken);
        return ApiResult<object>.Ok(new { });
    }

    private static async Task<ApiResult<object>> AssignPermissionsAsync(
        long id,
        AssignRolePermissionsRequest request,
        HttpContext httpContext,
        IRoleService service,
        IAuthClock clock,
        CancellationToken cancellationToken)
    {
        await service.AssignPermissionsAsync(id, request, Context(httpContext, clock), cancellationToken);
        return ApiResult<object>.Ok(new { });
    }

    private static async Task<ApiResult<object>> AssignMenusAsync(
        long id,
        AssignRoleMenusRequest request,
        HttpContext httpContext,
        IRoleService service,
        IAuthClock clock,
        CancellationToken cancellationToken)
    {
        await service.AssignMenusAsync(id, request, Context(httpContext, clock), cancellationToken);
        return ApiResult<object>.Ok(new { });
    }

    private static RoleRequestContext Context(HttpContext httpContext, IAuthClock clock)
    {
        var userIdText = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(userIdText, out var userId))
        {
            throw new DomainException(ApiCodes.Unauthorized, "Authentication is required.");
        }

        return new RoleRequestContext(
            userId,
            httpContext.User.Identity?.Name ?? string.Empty,
            httpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
            httpContext.Request.Headers.UserAgent.ToString(),
            httpContext.TraceIdentifier,
            clock.UtcNow);
    }
}
