using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WeCms.Modules.System.Auth;
using WeCms.Modules.System.Permissions;
using WeCms.Shared;

namespace WeCms.Modules.System.Departments;

public static class DepartmentEndpoints
{
    public static IEndpointRouteBuilder MapDepartmentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/system/depts")
            .RequireAuthorization();

        group.MapGet("", ListAsync).RequirePermission(DepartmentPermissions.List);
        group.MapGet("/tree", TreeAsync).RequirePermission(DepartmentPermissions.Tree);
        group.MapGet("/{id:long}", DetailAsync).RequirePermission(DepartmentPermissions.Detail);
        group.MapPost("", CreateAsync).RequirePermission(DepartmentPermissions.Create);
        group.MapPut("/{id:long}", UpdateAsync).RequirePermission(DepartmentPermissions.Update);
        group.MapDelete("/{id:long}", DeleteAsync).RequirePermission(DepartmentPermissions.Delete);
        group.MapPost("/{id:long}/enable", EnableAsync).RequirePermission(DepartmentPermissions.Enable);
        group.MapPost("/{id:long}/disable", DisableAsync).RequirePermission(DepartmentPermissions.Disable);

        return endpoints;
    }

    private static async Task<ApiResult<IReadOnlyList<DepartmentSummaryDto>>> ListAsync(IDepartmentService service, CancellationToken cancellationToken)
    {
        return ApiResult<IReadOnlyList<DepartmentSummaryDto>>.Ok(await service.ListAsync(cancellationToken));
    }

    private static async Task<ApiResult<IReadOnlyList<DepartmentTreeDto>>> TreeAsync(IDepartmentService service, CancellationToken cancellationToken)
    {
        return ApiResult<IReadOnlyList<DepartmentTreeDto>>.Ok(await service.TreeAsync(cancellationToken));
    }

    private static async Task<ApiResult<DepartmentDetailDto>> DetailAsync(long id, IDepartmentService service, CancellationToken cancellationToken)
    {
        return ApiResult<DepartmentDetailDto>.Ok(await service.GetAsync(id, cancellationToken));
    }

    private static async Task<ApiResult<DepartmentMutationResponse>> CreateAsync(CreateDepartmentRequest request, HttpContext httpContext, IDepartmentService service, IAuthClock clock, CancellationToken cancellationToken)
    {
        return ApiResult<DepartmentMutationResponse>.Ok(await service.CreateAsync(request, Context(httpContext, clock), cancellationToken));
    }

    private static async Task<ApiResult<DepartmentMutationResponse>> UpdateAsync(long id, UpdateDepartmentRequest request, HttpContext httpContext, IDepartmentService service, IAuthClock clock, CancellationToken cancellationToken)
    {
        return ApiResult<DepartmentMutationResponse>.Ok(await service.UpdateAsync(id, request, Context(httpContext, clock), cancellationToken));
    }

    private static async Task<ApiResult<object>> DeleteAsync(long id, HttpContext httpContext, IDepartmentService service, IAuthClock clock, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, Context(httpContext, clock), cancellationToken);
        return ApiResult<object>.Ok(new { });
    }

    private static async Task<ApiResult<object>> EnableAsync(long id, HttpContext httpContext, IDepartmentService service, IAuthClock clock, CancellationToken cancellationToken)
    {
        await service.EnableAsync(id, Context(httpContext, clock), cancellationToken);
        return ApiResult<object>.Ok(new { });
    }

    private static async Task<ApiResult<object>> DisableAsync(long id, HttpContext httpContext, IDepartmentService service, IAuthClock clock, CancellationToken cancellationToken)
    {
        await service.DisableAsync(id, Context(httpContext, clock), cancellationToken);
        return ApiResult<object>.Ok(new { });
    }

    private static DepartmentRequestContext Context(HttpContext httpContext, IAuthClock clock)
    {
        var userIdText = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(userIdText, out var userId))
        {
            throw new DomainException(ApiCodes.Unauthorized, "Authentication is required.");
        }

        return new DepartmentRequestContext(userId, httpContext.User.Identity?.Name ?? string.Empty, httpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty, httpContext.Request.Headers.UserAgent.ToString(), httpContext.TraceIdentifier, clock.UtcNow);
    }
}
