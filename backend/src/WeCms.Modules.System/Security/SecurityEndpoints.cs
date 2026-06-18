using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using WeCms.Modules.System.Auth;
using WeCms.Modules.System.Permissions;
using WeCms.Shared;

namespace WeCms.Modules.System.Security;

public static class SecurityEndpoints
{
    public static IEndpointRouteBuilder MapSecurityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/system/security")
            .RequireAuthorization();

        group.MapGet("/status", StatusAsync).RequirePermission(SecurityPermissions.Status);
        group.MapGet("/bans", ListBansAsync).RequirePermission(SecurityPermissions.BanList);
        group.MapGet("/bans/{id:long}", GetBanAsync).RequirePermission(SecurityPermissions.BanDetail);
        group.MapPost("/bans/{id:long}/unban", UnbanAsync).RequirePermission(SecurityPermissions.BanUnban).RequireRateLimiting(RateLimitPolicyNames.SecurityUnban);
        group.MapPost("/bans/batch-unban", BatchUnbanAsync).RequirePermission(SecurityPermissions.BanBatchUnban).RequireRateLimiting(RateLimitPolicyNames.SecurityUnban);

        return endpoints;
    }

    private static async Task<ApiResult<SecurityStatusDto>> StatusAsync(
        ISecurityBanService service,
        IAuthClock clock,
        CancellationToken cancellationToken)
    {
        return ApiResult<SecurityStatusDto>.Ok(await service.GetStatusAsync(clock.UtcNow, cancellationToken));
    }

    private static async Task<ApiResult<PagedResult<SecurityBanSummaryDto>>> ListBansAsync(
        int page,
        int pageSize,
        string? banType,
        string? target,
        string? severity,
        string? source,
        bool? activeOnly,
        ISecurityBanService service,
        CancellationToken cancellationToken)
    {
        var result = await service.ListAsync(new SecurityBanListQuery(page, pageSize, banType, target, severity, source, activeOnly ?? true), cancellationToken);
        return ApiResult<PagedResult<SecurityBanSummaryDto>>.Ok(result);
    }

    private static async Task<ApiResult<SecurityBanDetailDto>> GetBanAsync(
        long id,
        ISecurityBanService service,
        CancellationToken cancellationToken)
    {
        return ApiResult<SecurityBanDetailDto>.Ok(await service.GetAsync(id, cancellationToken));
    }

    private static async Task<ApiResult<SecurityBanMutationResponse>> UnbanAsync(
        long id,
        UnbanSecurityBanRequest request,
        HttpContext httpContext,
        ISecurityBanService service,
        IAuthClock clock,
        CancellationToken cancellationToken)
    {
        return ApiResult<SecurityBanMutationResponse>.Ok(await service.UnbanAsync(id, request, Context(httpContext, clock), cancellationToken));
    }

    private static async Task<ApiResult<BatchUnbanSecurityBansResponse>> BatchUnbanAsync(
        BatchUnbanSecurityBansRequest request,
        HttpContext httpContext,
        ISecurityBanService service,
        IAuthClock clock,
        CancellationToken cancellationToken)
    {
        return ApiResult<BatchUnbanSecurityBansResponse>.Ok(await service.BatchUnbanAsync(request, Context(httpContext, clock), cancellationToken));
    }

    private static SecurityBanRequestContext Context(HttpContext httpContext, IAuthClock clock)
    {
        var userIdText = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(userIdText, out var userId))
        {
            throw new DomainException(ApiCodes.Unauthorized, "Authentication is required.");
        }

        return new SecurityBanRequestContext(
            userId,
            httpContext.User.Identity?.Name ?? string.Empty,
            httpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty,
            httpContext.Request.Headers.UserAgent.ToString(),
            httpContext.TraceIdentifier,
            clock.UtcNow);
    }
}
