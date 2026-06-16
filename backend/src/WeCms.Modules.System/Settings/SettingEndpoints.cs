using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WeCms.Modules.System.Auth;
using WeCms.Modules.System.Permissions;
using WeCms.Shared;

namespace WeCms.Modules.System.Settings;

public static class SettingEndpoints
{
    public static IEndpointRouteBuilder MapSettingEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/system")
            .RequireAuthorization();

        group.MapGet("/settings", ListAsync).RequirePermission(SettingPermissions.List);
        group.MapGet("/settings/{key}", DetailAsync).RequirePermission(SettingPermissions.Detail);
        group.MapPut("/settings/{key}", UpdateAsync).RequirePermission(SettingPermissions.Update);

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

    private static async Task<ApiResult<SettingMutationResponse>> UpdateAsync(string key, UpdateSettingRequest request, HttpContext httpContext, ISettingService service, IAuthClock clock, CancellationToken cancellationToken)
    {
        return ApiResult<SettingMutationResponse>.Ok(await service.UpdateAsync(key, request, Context(httpContext, clock), cancellationToken));
    }

    private static SettingRequestContext Context(HttpContext httpContext, IAuthClock clock)
    {
        var userIdText = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(userIdText, out var userId))
        {
            throw new DomainException(ApiCodes.Unauthorized, "Authentication is required.");
        }

        return new SettingRequestContext(userId, httpContext.User.Identity?.Name ?? string.Empty, httpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty, httpContext.Request.Headers.UserAgent.ToString(), httpContext.TraceIdentifier, clock.UtcNow);
    }
}
