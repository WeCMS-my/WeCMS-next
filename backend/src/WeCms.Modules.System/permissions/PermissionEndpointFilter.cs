using System.Security.Claims;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Http;
using WeCms.Shared;
using WeCms.Shared.Security;

namespace WeCms.Modules.System.Permissions;

public sealed class PermissionEndpointFilter : IEndpointFilter
{
    private readonly JsonTypeInfo<ApiResult<object?>> _apiResultTypeInfo;
    private readonly IPermissionChecker _permissionChecker;

    public PermissionEndpointFilter(JsonSerializerContext jsonContext, IPermissionChecker permissionChecker)
    {
        _apiResultTypeInfo = (JsonTypeInfo<ApiResult<object?>>)jsonContext.GetTypeInfo(typeof(ApiResult<object?>))!;
        _permissionChecker = permissionChecker;
    }

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var endpoint = context.HttpContext.GetEndpoint();
        var metadata = endpoint?.Metadata.GetMetadata<PermissionMetadata>();
        if (metadata is null)
            return await next(context);

        var httpContext = context.HttpContext;

        var subClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(subClaim) || !long.TryParse(subClaim, out var userId))
        {
            return TypedResults.Json(
                ApiResult<object?>.Fail(ApiCodes.Unauthorized, "未登录"),
                _apiResultTypeInfo,
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var result = await _permissionChecker.CheckAsync(userId, metadata.Code, context.HttpContext.RequestAborted);

        if (!result.IsActive)
        {
            return TypedResults.Json(
                ApiResult<object?>.Fail(ApiCodes.Unauthorized, "用户已被禁用"),
                _apiResultTypeInfo,
                statusCode: StatusCodes.Status401Unauthorized);
        }

        if (!result.HasPermission)
        {
            return TypedResults.Json(
                ApiResult<object?>.Fail(ApiCodes.Forbidden, "无权限"),
                _apiResultTypeInfo,
                statusCode: StatusCodes.Status403Forbidden);
        }

        return await next(context);
    }
}
