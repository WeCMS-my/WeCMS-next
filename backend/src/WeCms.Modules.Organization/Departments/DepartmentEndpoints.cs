using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using WeCms.Shared;
using WeCms.Shared.Endpoints;

namespace WeCms.Modules.Organization.Departments;

public static class DepartmentEndpoints
{
    private const string AdminWriteRateLimitPolicy = "admin_write_policy";

    public static IEndpointRouteBuilder MapDepartmentEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/system/depts")
            .WithEndpointModule("organization")
            .AuditWriteEndpoints("organization", "depts")
            .RequireAuthorization();

        group.MapGet("", ListAsync).RequireEndpointPermission(DepartmentPermissions.List);
        group.MapGet("/tree", TreeAsync).RequireEndpointPermission(DepartmentPermissions.Tree);
        group.MapGet("/{id:long}", DetailAsync).RequireEndpointPermission(DepartmentPermissions.Detail);
        group.MapPost("", CreateAsync).RequireEndpointPermission(DepartmentPermissions.Create).RequireRateLimiting(AdminWriteRateLimitPolicy);
        group.MapPut("/{id:long}", UpdateAsync).RequireEndpointPermission(DepartmentPermissions.Update).RequireRateLimiting(AdminWriteRateLimitPolicy);
        group.MapDelete("/{id:long}", DeleteAsync).RequireEndpointPermission(DepartmentPermissions.Delete).RequireRateLimiting(AdminWriteRateLimitPolicy);
        group.MapPost("/{id:long}/enable", EnableAsync).RequireEndpointPermission(DepartmentPermissions.Enable).RequireRateLimiting(AdminWriteRateLimitPolicy);
        group.MapPost("/{id:long}/disable", DisableAsync).RequireEndpointPermission(DepartmentPermissions.Disable).RequireRateLimiting(AdminWriteRateLimitPolicy);

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

    private static async Task<ApiResult<DepartmentMutationResponse>> CreateAsync(CreateDepartmentRequest request, HttpContext httpContext, IDepartmentService service, IOrganizationClock clock, CancellationToken cancellationToken)
    {
        return ApiResult<DepartmentMutationResponse>.Ok(await service.CreateAsync(request, Context(httpContext, clock), cancellationToken));
    }

    private static async Task<ApiResult<DepartmentMutationResponse>> UpdateAsync(long id, UpdateDepartmentRequest request, HttpContext httpContext, IDepartmentService service, IOrganizationClock clock, CancellationToken cancellationToken)
    {
        return ApiResult<DepartmentMutationResponse>.Ok(await service.UpdateAsync(id, request, Context(httpContext, clock), cancellationToken));
    }

    private static async Task<ApiResult<object>> DeleteAsync(long id, HttpContext httpContext, IDepartmentService service, IOrganizationClock clock, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, Context(httpContext, clock), cancellationToken);
        return ApiResult<object>.Ok(new { });
    }

    private static async Task<ApiResult<object>> EnableAsync(long id, HttpContext httpContext, IDepartmentService service, IOrganizationClock clock, CancellationToken cancellationToken)
    {
        await service.EnableAsync(id, Context(httpContext, clock), cancellationToken);
        return ApiResult<object>.Ok(new { });
    }

    private static async Task<ApiResult<object>> DisableAsync(long id, HttpContext httpContext, IDepartmentService service, IOrganizationClock clock, CancellationToken cancellationToken)
    {
        await service.DisableAsync(id, Context(httpContext, clock), cancellationToken);
        return ApiResult<object>.Ok(new { });
    }

    private static DepartmentRequestContext Context(HttpContext httpContext, IOrganizationClock clock)
    {
        var userIdText = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(userIdText, out var userId))
        {
            throw new DomainException(ApiCodes.Unauthorized, "Authentication is required.");
        }

        return new DepartmentRequestContext(userId, httpContext.User.Identity?.Name ?? string.Empty, httpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty, httpContext.Request.Headers.UserAgent.ToString(), httpContext.TraceIdentifier, clock.UtcNow);
    }
}
