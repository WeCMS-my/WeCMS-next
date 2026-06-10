using System.Security.Claims;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using WeCms.Shared;
using WeCms.Shared.Security;

namespace WeCms.Modules.System.Permissions;

public sealed class PermissionEndpointFilter : IEndpointFilter
{
    private readonly JsonTypeInfo<ApiResult<object?>> _apiResultTypeInfo;

    public PermissionEndpointFilter(JsonSerializerContext jsonContext)
    {
        _apiResultTypeInfo = (JsonTypeInfo<ApiResult<object?>>)jsonContext.GetTypeInfo(typeof(ApiResult<object?>))!;
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

        var checker = httpContext.RequestServices.GetRequiredService<IPermissionChecker>();
        var result = await checker.CheckAsync(userId, metadata.Code, context.HttpContext.RequestAborted);

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
