using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using WeCms.Shared;
using WeCms.Shared.Endpoints;

namespace WeCms.Modules.Configuration.Settings;

public static class SettingEndpoints
{
    private const string AdminWriteRateLimitPolicy = "admin_write_policy";

    public static IEndpointRouteBuilder MapSettingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/system")
            .WithEndpointModule("configuration")
            .AuditWriteEndpoints("configuration", "settings")
            .RequireAuthorization();

        group.MapGet("/settings", ListAsync).RequireEndpointPermission(SettingPermissions.List);
        group.MapGet("/settings/{key}", DetailAsync).RequireEndpointPermission(SettingPermissions.Detail);
        group.MapPut("/settings/{key}", UpdateAsync).RequireEndpointPermission(SettingPermissions.Update).RequireRateLimiting(AdminWriteRateLimitPolicy);
        group.MapPost("/settings/validate-ip-rules", ValidateIpRules).RequireEndpointPermission(SettingPermissions.ValidateIpRules).RequireRateLimiting(AdminWriteRateLimitPolicy);
        group.MapPost("/settings/reload-cache", ReloadCacheAsync).RequireEndpointPermission(SettingPermissions.ReloadCache).RequireRateLimiting(AdminWriteRateLimitPolicy);

        return endpoints;
    }

    private static async Task<ApiResult<PagedResult<SettingSummaryDto>>> ListAsync(int page, int pageSize, string? keyword, string? groupCode, ISettingService service, CancellationToken cancellationToken)
    {
        return ApiResult<PagedResult<SettingSummaryDto>>.Ok(await service.ListAsync(new SettingListQuery(page, pageSize, keyword, groupCode), cancellationToken));
    }

    private static async Task<ApiResult<SettingDetailDto>> DetailAsync(string key, ISettingService service, CancellationToken cancellationToken)
    {
        return ApiResult<SettingDetailDto>.Ok(await service.GetAsync(key, cancellationToken));
    }

    private static async Task<ApiResult<SettingMutationResponse>> UpdateAsync(string key, UpdateSettingRequest request, HttpContext httpContext, ISettingService service, IConfigurationClock clock, CancellationToken cancellationToken)
    {
        return ApiResult<SettingMutationResponse>.Ok(await service.UpdateAsync(key, request, Context(httpContext, clock), cancellationToken));
    }

    private static async Task<ApiResult<ValidateIpRulesResponse>> ValidateIpRules(ValidateIpRulesRequest request, HttpContext httpContext, ISettingService service, IConfigurationClock clock, CancellationToken cancellationToken)
    {
        return ApiResult<ValidateIpRulesResponse>.Ok(await service.ValidateIpRulesAsync(request, Context(httpContext, clock), cancellationToken));
    }

    private static async Task<ApiResult<object>> ReloadCacheAsync(HttpContext httpContext, ISettingService service, IConfigurationClock clock, CancellationToken cancellationToken)
    {
        await service.ReloadCacheAsync(Context(httpContext, clock), cancellationToken);
        return ApiResult<object>.Ok(new { });
    }

    private static SettingRequestContext Context(HttpContext httpContext, IConfigurationClock clock)
    {
        var userIdText = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(userIdText, out var userId))
        {
            throw new DomainException(ApiCodes.Unauthorized, "Authentication is required.");
        }

        return new SettingRequestContext(userId, httpContext.User.Identity?.Name ?? string.Empty, httpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty, httpContext.Request.Headers.UserAgent.ToString(), httpContext.TraceIdentifier, clock.UtcNow);
    }
}
