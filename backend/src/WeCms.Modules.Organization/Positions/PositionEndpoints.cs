using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using WeCms.Shared;
using WeCms.Shared.Endpoints;

namespace WeCms.Modules.Organization.Positions;

public static class PositionEndpoints
{
    private const string AdminWriteRateLimitPolicy = "admin_write_policy";

    public static IEndpointRouteBuilder MapPositionEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/system/positions")
            .WithEndpointModule("organization")
            .AuditWriteEndpoints("organization", "positions")
            .RequireAuthorization();

        group.MapGet("", ListAsync).RequireEndpointPermission(PositionPermissions.List);
        group.MapGet("/{id:long}", DetailAsync).RequireEndpointPermission(PositionPermissions.Detail);
        group.MapPost("", CreateAsync).RequireEndpointPermission(PositionPermissions.Create).RequireRateLimiting(AdminWriteRateLimitPolicy);
        group.MapPut("/{id:long}", UpdateAsync).RequireEndpointPermission(PositionPermissions.Update).RequireRateLimiting(AdminWriteRateLimitPolicy);
        group.MapDelete("/{id:long}", DeleteAsync).RequireEndpointPermission(PositionPermissions.Delete).RequireRateLimiting(AdminWriteRateLimitPolicy);
        group.MapPost("/{id:long}/enable", EnableAsync).RequireEndpointPermission(PositionPermissions.Enable).RequireRateLimiting(AdminWriteRateLimitPolicy);
        group.MapPost("/{id:long}/disable", DisableAsync).RequireEndpointPermission(PositionPermissions.Disable).RequireRateLimiting(AdminWriteRateLimitPolicy);

        return endpoints;
    }

    private static async Task<ApiResult<PagedResult<PositionSummaryDto>>> ListAsync(int page, int pageSize, string? keyword, string? status, IPositionService service, CancellationToken cancellationToken)
    {
        return ApiResult<PagedResult<PositionSummaryDto>>.Ok(await service.ListAsync(new PositionListQuery(page, pageSize, keyword, status), cancellationToken));
    }

    private static async Task<ApiResult<PositionDetailDto>> DetailAsync(long id, IPositionService service, CancellationToken cancellationToken)
    {
        return ApiResult<PositionDetailDto>.Ok(await service.GetAsync(id, cancellationToken));
    }

    private static async Task<ApiResult<PositionMutationResponse>> CreateAsync(CreatePositionRequest request, HttpContext httpContext, IPositionService service, IOrganizationClock clock, CancellationToken cancellationToken)
    {
        return ApiResult<PositionMutationResponse>.Ok(await service.CreateAsync(request, Context(httpContext, clock), cancellationToken));
    }

    private static async Task<ApiResult<PositionMutationResponse>> UpdateAsync(long id, UpdatePositionRequest request, HttpContext httpContext, IPositionService service, IOrganizationClock clock, CancellationToken cancellationToken)
    {
        return ApiResult<PositionMutationResponse>.Ok(await service.UpdateAsync(id, request, Context(httpContext, clock), cancellationToken));
    }

    private static async Task<ApiResult<object>> DeleteAsync(long id, HttpContext httpContext, IPositionService service, IOrganizationClock clock, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, Context(httpContext, clock), cancellationToken);
        return ApiResult<object>.Ok(new { });
    }

    private static async Task<ApiResult<object>> EnableAsync(long id, HttpContext httpContext, IPositionService service, IOrganizationClock clock, CancellationToken cancellationToken)
    {
        await service.EnableAsync(id, Context(httpContext, clock), cancellationToken);
        return ApiResult<object>.Ok(new { });
    }

    private static async Task<ApiResult<object>> DisableAsync(long id, HttpContext httpContext, IPositionService service, IOrganizationClock clock, CancellationToken cancellationToken)
    {
        await service.DisableAsync(id, Context(httpContext, clock), cancellationToken);
        return ApiResult<object>.Ok(new { });
    }

    private static PositionRequestContext Context(HttpContext httpContext, IOrganizationClock clock)
    {
        var userIdText = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(userIdText, out var userId))
        {
            throw new DomainException(ApiCodes.Unauthorized, "Authentication is required.");
        }

        return new PositionRequestContext(userId, httpContext.User.Identity?.Name ?? string.Empty, httpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty, httpContext.Request.Headers.UserAgent.ToString(), httpContext.TraceIdentifier, clock.UtcNow);
    }
}
