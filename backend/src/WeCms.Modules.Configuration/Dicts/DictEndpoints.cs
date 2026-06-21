using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using WeCms.Shared;
using WeCms.Shared.Endpoints;

namespace WeCms.Modules.Configuration.Dicts;

public static class DictEndpoints
{
    private const string AdminWriteRateLimitPolicy = "admin_write_policy";

    public static IEndpointRouteBuilder MapDictEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/system")
            .WithEndpointModule("configuration")
            .AuditWriteEndpoints("configuration", "dicts")
            .RequireAuthorization();

        group.MapGet("/dict-types", ListTypesAsync).RequireEndpointPermission(DictPermissions.TypeList);
        group.MapGet("/dict-types/{id:long}", DetailTypeAsync).RequireEndpointPermission(DictPermissions.TypeList);
        group.MapPost("/dict-types", CreateTypeAsync).RequireEndpointPermission(DictPermissions.TypeCreate).RequireRateLimiting(AdminWriteRateLimitPolicy);
        group.MapPut("/dict-types/{id:long}", UpdateTypeAsync).RequireEndpointPermission(DictPermissions.TypeUpdate).RequireRateLimiting(AdminWriteRateLimitPolicy);
        group.MapDelete("/dict-types/{id:long}", DeleteTypeAsync).RequireEndpointPermission(DictPermissions.TypeDelete).RequireRateLimiting(AdminWriteRateLimitPolicy);
        group.MapPost("/dict-types/{id:long}/enable", EnableTypeAsync).RequireEndpointPermission(DictPermissions.TypeEnable).RequireRateLimiting(AdminWriteRateLimitPolicy);
        group.MapPost("/dict-types/{id:long}/disable", DisableTypeAsync).RequireEndpointPermission(DictPermissions.TypeDisable).RequireRateLimiting(AdminWriteRateLimitPolicy);
        group.MapGet("/dict-types/{typeCode}/values", ListValuesAsync).RequireEndpointPermission(DictPermissions.ValueList);
        group.MapPost("/dict-types/{typeCode}/values", CreateValueAsync).RequireEndpointPermission(DictPermissions.ValueCreate).RequireRateLimiting(AdminWriteRateLimitPolicy);
        group.MapPut("/dict-values/{id:long}", UpdateValueAsync).RequireEndpointPermission(DictPermissions.ValueUpdate).RequireRateLimiting(AdminWriteRateLimitPolicy);
        group.MapDelete("/dict-values/{id:long}", DeleteValueAsync).RequireEndpointPermission(DictPermissions.ValueDelete).RequireRateLimiting(AdminWriteRateLimitPolicy);
        group.MapPost("/dict-values/{id:long}/enable", EnableValueAsync).RequireEndpointPermission(DictPermissions.ValueEnable).RequireRateLimiting(AdminWriteRateLimitPolicy);
        group.MapPost("/dict-values/{id:long}/disable", DisableValueAsync).RequireEndpointPermission(DictPermissions.ValueDisable).RequireRateLimiting(AdminWriteRateLimitPolicy);

        return endpoints;
    }

    private static async Task<ApiResult<PagedResult<DictTypeSummaryDto>>> ListTypesAsync(int page, int pageSize, string? keyword, string? status, IDictService service, CancellationToken cancellationToken)
    {
        return ApiResult<PagedResult<DictTypeSummaryDto>>.Ok(await service.ListTypesAsync(new DictTypeListQuery(page, pageSize, keyword, status), cancellationToken));
    }

    private static async Task<ApiResult<DictTypeDetailDto>> DetailTypeAsync(long id, IDictService service, CancellationToken cancellationToken)
    {
        return ApiResult<DictTypeDetailDto>.Ok(await service.GetTypeAsync(id, cancellationToken));
    }

    private static async Task<ApiResult<DictMutationResponse>> CreateTypeAsync(CreateDictTypeRequest request, HttpContext httpContext, IDictService service, IConfigurationClock clock, CancellationToken cancellationToken)
    {
        return ApiResult<DictMutationResponse>.Ok(await service.CreateTypeAsync(request, Context(httpContext, clock), cancellationToken));
    }

    private static async Task<ApiResult<DictMutationResponse>> UpdateTypeAsync(long id, UpdateDictTypeRequest request, HttpContext httpContext, IDictService service, IConfigurationClock clock, CancellationToken cancellationToken)
    {
        return ApiResult<DictMutationResponse>.Ok(await service.UpdateTypeAsync(id, request, Context(httpContext, clock), cancellationToken));
    }

    private static async Task<ApiResult<object>> DeleteTypeAsync(long id, HttpContext httpContext, IDictService service, IConfigurationClock clock, CancellationToken cancellationToken)
    {
        await service.DeleteTypeAsync(id, Context(httpContext, clock), cancellationToken);
        return ApiResult<object>.Ok(new { });
    }

    private static async Task<ApiResult<object>> EnableTypeAsync(long id, HttpContext httpContext, IDictService service, IConfigurationClock clock, CancellationToken cancellationToken)
    {
        await service.EnableTypeAsync(id, Context(httpContext, clock), cancellationToken);
        return ApiResult<object>.Ok(new { });
    }

    private static async Task<ApiResult<object>> DisableTypeAsync(long id, DisableDictTypeRequest request, HttpContext httpContext, IDictService service, IConfigurationClock clock, CancellationToken cancellationToken)
    {
        await service.DisableTypeAsync(id, request, Context(httpContext, clock), cancellationToken);
        return ApiResult<object>.Ok(new { });
    }

    private static async Task<ApiResult<IReadOnlyList<DictValueDto>>> ListValuesAsync(string typeCode, IDictService service, CancellationToken cancellationToken)
    {
        return ApiResult<IReadOnlyList<DictValueDto>>.Ok(await service.ListValuesAsync(typeCode, cancellationToken));
    }

    private static async Task<ApiResult<DictMutationResponse>> CreateValueAsync(string typeCode, CreateDictValueRequest request, HttpContext httpContext, IDictService service, IConfigurationClock clock, CancellationToken cancellationToken)
    {
        return ApiResult<DictMutationResponse>.Ok(await service.CreateValueAsync(typeCode, request, Context(httpContext, clock), cancellationToken));
    }

    private static async Task<ApiResult<DictMutationResponse>> UpdateValueAsync(long id, UpdateDictValueRequest request, HttpContext httpContext, IDictService service, IConfigurationClock clock, CancellationToken cancellationToken)
    {
        return ApiResult<DictMutationResponse>.Ok(await service.UpdateValueAsync(id, request, Context(httpContext, clock), cancellationToken));
    }

    private static async Task<ApiResult<object>> DeleteValueAsync(long id, HttpContext httpContext, IDictService service, IConfigurationClock clock, CancellationToken cancellationToken)
    {
        await service.DeleteValueAsync(id, Context(httpContext, clock), cancellationToken);
        return ApiResult<object>.Ok(new { });
    }

    private static async Task<ApiResult<object>> EnableValueAsync(long id, HttpContext httpContext, IDictService service, IConfigurationClock clock, CancellationToken cancellationToken)
    {
        await service.EnableValueAsync(id, Context(httpContext, clock), cancellationToken);
        return ApiResult<object>.Ok(new { });
    }

    private static async Task<ApiResult<object>> DisableValueAsync(long id, HttpContext httpContext, IDictService service, IConfigurationClock clock, CancellationToken cancellationToken)
    {
        await service.DisableValueAsync(id, Context(httpContext, clock), cancellationToken);
        return ApiResult<object>.Ok(new { });
    }

    private static DictRequestContext Context(HttpContext httpContext, IConfigurationClock clock)
    {
        var userIdText = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(userIdText, out var userId))
        {
            throw new DomainException(ApiCodes.Unauthorized, "Authentication is required.");
        }

        return new DictRequestContext(userId, httpContext.User.Identity?.Name ?? string.Empty, httpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty, httpContext.Request.Headers.UserAgent.ToString(), httpContext.TraceIdentifier, clock.UtcNow);
    }
}
