using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using WeCms.Modules.System.Auth;
using WeCms.Modules.System.Permissions;
using WeCms.Modules.System.Security;
using WeCms.Shared;

namespace WeCms.Modules.System.I18n;

public static class I18nEndpoints
{
    public static IEndpointRouteBuilder MapI18nEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var systemGroup = endpoints.MapGroup("/api/v1/system/i18n/messages")
            .RequireAuthorization();

        systemGroup.MapGet("", ListAsync).RequirePermission(I18nPermissions.List);
        systemGroup.MapGet("/{id:long}", DetailAsync).RequirePermission(I18nPermissions.Detail);
        systemGroup.MapPost("", CreateAsync).RequirePermission(I18nPermissions.Create).RequireRateLimiting(RateLimitPolicyNames.AdminWrite);
        systemGroup.MapPut("/{id:long}", UpdateAsync).RequirePermission(I18nPermissions.Update).RequireRateLimiting(RateLimitPolicyNames.AdminWrite);
        systemGroup.MapDelete("/{id:long}", DeleteAsync).RequirePermission(I18nPermissions.Delete).RequireRateLimiting(RateLimitPolicyNames.AdminWrite);

        endpoints.MapGet("/api/v1/i18n/messages", PublicMessagesAsync);

        endpoints.MapPost("/api/v1/account/i18n/switch", SwitchLocaleAsync)
            .RequireAuthorization()
            .RequirePermission(I18nPermissions.AccountSwitch)
            .RequireRateLimiting(RateLimitPolicyNames.AdminWrite);

        return endpoints;
    }

    private static async Task<ApiResult<PagedResult<I18nMessageSummaryDto>>> ListAsync(int page, int pageSize, string? locale, string? module, string? keyword, string? status, II18nMessageService service, CancellationToken cancellationToken)
    {
        return ApiResult<PagedResult<I18nMessageSummaryDto>>.Ok(await service.ListAsync(new I18nMessageListQuery(page, pageSize, locale, module, keyword, status), cancellationToken));
    }

    private static async Task<ApiResult<I18nMessageDetailDto>> DetailAsync(long id, II18nMessageService service, CancellationToken cancellationToken)
    {
        return ApiResult<I18nMessageDetailDto>.Ok(await service.GetAsync(id, cancellationToken));
    }

    private static async Task<ApiResult<I18nMutationResponse>> CreateAsync(CreateI18nMessageRequest request, HttpContext httpContext, II18nMessageService service, IAuthClock clock, CancellationToken cancellationToken)
    {
        return ApiResult<I18nMutationResponse>.Ok(await service.CreateAsync(request, Context(httpContext, clock), cancellationToken));
    }

    private static async Task<ApiResult<I18nMutationResponse>> UpdateAsync(long id, UpdateI18nMessageRequest request, HttpContext httpContext, II18nMessageService service, IAuthClock clock, CancellationToken cancellationToken)
    {
        return ApiResult<I18nMutationResponse>.Ok(await service.UpdateAsync(id, request, Context(httpContext, clock), cancellationToken));
    }

    private static async Task<ApiResult<object>> DeleteAsync(long id, HttpContext httpContext, II18nMessageService service, IAuthClock clock, CancellationToken cancellationToken)
    {
        await service.DeleteAsync(id, Context(httpContext, clock), cancellationToken);
        return ApiResult<object>.Ok(new { });
    }

    private static async Task<ApiResult<I18nMessagesResponse>> PublicMessagesAsync(string locale, II18nMessageService service, CancellationToken cancellationToken)
    {
        return ApiResult<I18nMessagesResponse>.Ok(await service.GetPublicMessagesAsync(new PublicI18nMessagesQuery(locale), cancellationToken));
    }

    private static async Task<ApiResult<AccountI18nSwitchResponse>> SwitchLocaleAsync(SwitchAccountLocaleRequest request, HttpContext httpContext, II18nMessageService service, IAuthClock clock, CancellationToken cancellationToken)
    {
        return ApiResult<AccountI18nSwitchResponse>.Ok(await service.SwitchLocaleAsync(request, Context(httpContext, clock), cancellationToken));
    }

    private static I18nRequestContext Context(HttpContext httpContext, IAuthClock clock)
    {
        var userIdText = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(userIdText, out var userId))
        {
            throw new DomainException(ApiCodes.Unauthorized, "Authentication is required.");
        }

        return new I18nRequestContext(userId, httpContext.User.Identity?.Name ?? string.Empty, httpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty, httpContext.Request.Headers.UserAgent.ToString(), httpContext.TraceIdentifier, clock.UtcNow);
    }
}
