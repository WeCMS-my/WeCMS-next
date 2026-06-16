using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WeCms.Modules.System.Auth;
using WeCms.Shared;

namespace WeCms.Modules.System.Permissions;

public static class PermissionManagementEndpoints
{
    public static IEndpointRouteBuilder MapPermissionManagementEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/system/permissions")
            .RequireAuthorization();

        group.MapGet("", ListAsync).RequirePermission(PermissionManagementPermissions.List);
        group.MapGet("/tree", TreeAsync).RequirePermission(PermissionManagementPermissions.Tree);
        group.MapGet("/{id:long}", DetailAsync).RequirePermission(PermissionManagementPermissions.Detail);
        group.MapPost("", CreateAsync).RequirePermission(PermissionManagementPermissions.Create);
        group.MapPut("/{id:long}", UpdateAsync).RequirePermission(PermissionManagementPermissions.Update);
        group.MapDelete("/{id:long}", DeleteAsync).RequirePermission(PermissionManagementPermissions.Delete);
        group.MapPost("/{id:long}/enable", EnableAsync).RequirePermission(PermissionManagementPermissions.Enable);
        group.MapPost("/{id:long}/disable", DisableAsync).RequirePermission(PermissionManagementPermissions.Disable);

        return endpoints;
    }

    private static async Task<ApiResult<IReadOnlyList<PermissionSummaryDto>>> ListAsync(IPermissionManagementService service, CancellationToken cancellationToken)
    {
        return ApiResult<IReadOnlyList<PermissionSummaryDto>>.Ok(await service.ListAsync(cancellationToken));
    }

    private static async Task<ApiResult<IReadOnlyList<PermissionTreeDto>>> TreeAsync(IPermissionManagementService service, CancellationToken cancellationToken)
    {
        return ApiResult<IReadOnlyList<PermissionTreeDto>>.Ok(await service.TreeAsync(cancellationToken));
    }

    private static async Task<ApiResult<PermissionDetailDto>> DetailAsync(long id, IPermissionManagementService service, CancellationToken cancellationToken)
    {
        return ApiResult<PermissionDetailDto>.Ok(await service.GetAsync(id, cancellationToken));
    }

    private static async Task<ApiResult<PermissionMutationResponse>> CreateAsync(CreatePermissionRequest request, HttpContext httpContext, IPermissionManagementService service, IAuthClock clock, CancellationToken cancellationToken)
    {
        return ApiResult<PermissionMutationResponse>.Ok(await service.CreateAsync(request, Context(httpContext, clock), cancellationToken));
    }

    private static async Task<ApiResult<PermissionMutationResponse>> UpdateAsync(long id, UpdatePermissionRequest request, HttpContext httpContext, IPermissionManagementService service, IAuthClock clock, CancellationToken cancellationToken)
    {
        return ApiResult<PermissionMutationResponse>.Ok(await service.UpdateAsync(id, request, Context(httpContext, clock), cancellationToken));
    }

    private static async Task<ApiResult<object>> DeleteAsync(long id, HttpContext httpContext, IPermissionManagementService service, IAuthClock clock, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, Context(httpContext, clock), cancellationToken);
        return ApiResult<object>.Ok(new { });
    }

    private static async Task<ApiResult<object>> EnableAsync(long id, HttpContext httpContext, IPermissionManagementService service, IAuthClock clock, CancellationToken cancellationToken)
    {
        await service.EnableAsync(id, Context(httpContext, clock), cancellationToken);
        return ApiResult<object>.Ok(new { });
    }

    private static async Task<ApiResult<object>> DisableAsync(long id, HttpContext httpContext, IPermissionManagementService service, IAuthClock clock, CancellationToken cancellationToken)
    {
        await service.DisableAsync(id, Context(httpContext, clock), cancellationToken);
        return ApiResult<object>.Ok(new { });
    }

    private static PermissionRequestContext Context(HttpContext httpContext, IAuthClock clock)
    {
        var userIdText = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(userIdText, out var userId))
        {
            throw new DomainException(ApiCodes.Unauthorized, "Authentication is required.");
        }

        return new PermissionRequestContext(userId, httpContext.User.Identity?.Name ?? string.Empty, httpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty, httpContext.Request.Headers.UserAgent.ToString(), httpContext.TraceIdentifier, clock.UtcNow);
    }
}
