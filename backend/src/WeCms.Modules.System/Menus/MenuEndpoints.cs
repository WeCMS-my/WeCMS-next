using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using WeCms.Modules.System.Auth;
using WeCms.Modules.System.Permissions;
using WeCms.Modules.System.Security;
using WeCms.Shared;

namespace WeCms.Modules.System.Menus;

public static class MenuEndpoints
{
    public static IEndpointRouteBuilder MapMenuEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/system/menus")
            .RequireAuthorization();

        group.MapGet("", ListAsync).RequirePermission(MenuPermissions.List);
        group.MapGet("/tree", TreeAsync).RequirePermission(MenuPermissions.Tree);
        group.MapGet("/{id:long}", DetailAsync).RequirePermission(MenuPermissions.Detail);
        group.MapPost("", CreateAsync).RequirePermission(MenuPermissions.Create).RequireRateLimiting(RateLimitPolicyNames.AdminWrite);
        group.MapPut("/{id:long}", UpdateAsync).RequirePermission(MenuPermissions.Update).RequireRateLimiting(RateLimitPolicyNames.AdminWrite);
        group.MapPut("/sort", SortAsync).RequirePermission(MenuPermissions.Sort).RequireRateLimiting(RateLimitPolicyNames.AdminWrite);
        group.MapDelete("/{id:long}", DeleteAsync).RequirePermission(MenuPermissions.Delete).RequireRateLimiting(RateLimitPolicyNames.AdminWrite);
        group.MapPost("/{id:long}/enable", EnableAsync).RequirePermission(MenuPermissions.Enable).RequireRateLimiting(RateLimitPolicyNames.AdminWrite);
        group.MapPost("/{id:long}/disable", DisableAsync).RequirePermission(MenuPermissions.Disable).RequireRateLimiting(RateLimitPolicyNames.AdminWrite);

        return endpoints;
    }

    private static async Task<ApiResult<IReadOnlyList<MenuSummaryDto>>> ListAsync(IMenuService service, CancellationToken cancellationToken)
    {
        return ApiResult<IReadOnlyList<MenuSummaryDto>>.Ok(await service.ListAsync(cancellationToken));
    }

    private static async Task<ApiResult<IReadOnlyList<MenuTreeDto>>> TreeAsync(IMenuService service, CancellationToken cancellationToken)
    {
        return ApiResult<IReadOnlyList<MenuTreeDto>>.Ok(await service.TreeAsync(cancellationToken));
    }

    private static async Task<ApiResult<MenuDetailDto>> DetailAsync(long id, IMenuService service, CancellationToken cancellationToken)
    {
        return ApiResult<MenuDetailDto>.Ok(await service.GetAsync(id, cancellationToken));
    }

    private static async Task<ApiResult<MenuMutationResponse>> CreateAsync(CreateMenuRequest request, HttpContext httpContext, IMenuService service, IAuthClock clock, CancellationToken cancellationToken)
    {
        return ApiResult<MenuMutationResponse>.Ok(await service.CreateAsync(request, Context(httpContext, clock), cancellationToken));
    }

    private static async Task<ApiResult<MenuMutationResponse>> UpdateAsync(long id, UpdateMenuRequest request, HttpContext httpContext, IMenuService service, IAuthClock clock, CancellationToken cancellationToken)
    {
        return ApiResult<MenuMutationResponse>.Ok(await service.UpdateAsync(id, request, Context(httpContext, clock), cancellationToken));
    }

    private static async Task<ApiResult<object>> SortAsync(SortMenusRequest request, HttpContext httpContext, IMenuService service, IAuthClock clock, CancellationToken cancellationToken)
    {
        await service.SortAsync(request, Context(httpContext, clock), cancellationToken);
        return ApiResult<object>.Ok(new { });
    }

    private static async Task<ApiResult<object>> DeleteAsync(long id, HttpContext httpContext, IMenuService service, IAuthClock clock, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, Context(httpContext, clock), cancellationToken);
        return ApiResult<object>.Ok(new { });
    }

    private static async Task<ApiResult<object>> EnableAsync(long id, HttpContext httpContext, IMenuService service, IAuthClock clock, CancellationToken cancellationToken)
    {
        await service.EnableAsync(id, Context(httpContext, clock), cancellationToken);
        return ApiResult<object>.Ok(new { });
    }

    private static async Task<ApiResult<object>> DisableAsync(long id, HttpContext httpContext, IMenuService service, IAuthClock clock, CancellationToken cancellationToken)
    {
        await service.DisableAsync(id, Context(httpContext, clock), cancellationToken);
        return ApiResult<object>.Ok(new { });
    }

    private static MenuRequestContext Context(HttpContext httpContext, IAuthClock clock)
    {
        var userIdText = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(userIdText, out var userId))
        {
            throw new DomainException(ApiCodes.Unauthorized, "Authentication is required.");
        }

        return new MenuRequestContext(userId, httpContext.User.Identity?.Name ?? string.Empty, httpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty, httpContext.Request.Headers.UserAgent.ToString(), httpContext.TraceIdentifier, clock.UtcNow);
    }
}
