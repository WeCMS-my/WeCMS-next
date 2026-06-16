using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WeCms.Modules.System.Auth;
using WeCms.Modules.System.Permissions;
using WeCms.Shared;

namespace WeCms.Modules.System.Dicts;

public static class DictEndpoints
{
    public static IEndpointRouteBuilder MapDictEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/v1/system")
            .RequireAuthorization();

        group.MapGet("/dict-types", ListTypesAsync).RequirePermission(DictPermissions.TypeList);
        group.MapGet("/dict-types/{id:long}", DetailTypeAsync).RequirePermission(DictPermissions.TypeList);
        group.MapPost("/dict-types", CreateTypeAsync).RequirePermission(DictPermissions.TypeCreate);
        group.MapPut("/dict-types/{id:long}", UpdateTypeAsync).RequirePermission(DictPermissions.TypeUpdate);
        group.MapDelete("/dict-types/{id:long}", DeleteTypeAsync).RequirePermission(DictPermissions.TypeDelete);
        group.MapGet("/dict-types/{typeCode}/values", ListValuesAsync).RequirePermission(DictPermissions.ValueList);
        group.MapPost("/dict-types/{typeCode}/values", CreateValueAsync).RequirePermission(DictPermissions.ValueCreate);
        group.MapPut("/dict-values/{id:long}", UpdateValueAsync).RequirePermission(DictPermissions.ValueUpdate);
        group.MapDelete("/dict-values/{id:long}", DeleteValueAsync).RequirePermission(DictPermissions.ValueDelete);

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

    private static async Task<ApiResult<DictMutationResponse>> CreateTypeAsync(CreateDictTypeRequest request, HttpContext httpContext, IDictService service, IAuthClock clock, CancellationToken cancellationToken)
    {
        return ApiResult<DictMutationResponse>.Ok(await service.CreateTypeAsync(request, Context(httpContext, clock), cancellationToken));
    }

    private static async Task<ApiResult<DictMutationResponse>> UpdateTypeAsync(long id, UpdateDictTypeRequest request, HttpContext httpContext, IDictService service, IAuthClock clock, CancellationToken cancellationToken)
    {
        return ApiResult<DictMutationResponse>.Ok(await service.UpdateTypeAsync(id, request, Context(httpContext, clock), cancellationToken));
    }

    private static async Task<ApiResult<object>> DeleteTypeAsync(long id, HttpContext httpContext, IDictService service, IAuthClock clock, CancellationToken cancellationToken)
    {
        await service.DeleteTypeAsync(id, Context(httpContext, clock), cancellationToken);
        return ApiResult<object>.Ok(new { });
    }

    private static async Task<ApiResult<IReadOnlyList<DictValueDto>>> ListValuesAsync(string typeCode, IDictService service, CancellationToken cancellationToken)
    {
        return ApiResult<IReadOnlyList<DictValueDto>>.Ok(await service.ListValuesAsync(typeCode, cancellationToken));
    }

    private static async Task<ApiResult<DictMutationResponse>> CreateValueAsync(string typeCode, CreateDictValueRequest request, HttpContext httpContext, IDictService service, IAuthClock clock, CancellationToken cancellationToken)
    {
        return ApiResult<DictMutationResponse>.Ok(await service.CreateValueAsync(typeCode, request, Context(httpContext, clock), cancellationToken));
    }

    private static async Task<ApiResult<DictMutationResponse>> UpdateValueAsync(long id, UpdateDictValueRequest request, HttpContext httpContext, IDictService service, IAuthClock clock, CancellationToken cancellationToken)
    {
        return ApiResult<DictMutationResponse>.Ok(await service.UpdateValueAsync(id, request, Context(httpContext, clock), cancellationToken));
    }

    private static async Task<ApiResult<object>> DeleteValueAsync(long id, HttpContext httpContext, IDictService service, IAuthClock clock, CancellationToken cancellationToken)
    {
        await service.DeleteValueAsync(id, Context(httpContext, clock), cancellationToken);
        return ApiResult<object>.Ok(new { });
    }

    private static DictRequestContext Context(HttpContext httpContext, IAuthClock clock)
    {
        var userIdText = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(userIdText, out var userId))
        {
            throw new DomainException(ApiCodes.Unauthorized, "Authentication is required.");
        }

        return new DictRequestContext(userId, httpContext.User.Identity?.Name ?? string.Empty, httpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty, httpContext.Request.Headers.UserAgent.ToString(), httpContext.TraceIdentifier, clock.UtcNow);
    }
}
