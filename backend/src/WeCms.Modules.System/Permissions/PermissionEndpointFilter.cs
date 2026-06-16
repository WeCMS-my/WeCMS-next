using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using WeCms.Shared;

namespace WeCms.Modules.System.Permissions;

public sealed class PermissionEndpointFilter : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var metadata = context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<PermissionMetadata>()
            ?? throw new InvalidOperationException("PermissionMetadata is required.");
        var userIdValue = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(userIdValue, out var userId))
        {
            return Results.Json(
                ApiResult<object>.Error(ApiCodes.Unauthorized, "Authentication is required.", context.HttpContext.TraceIdentifier),
                statusCode: ApiCodes.ToHttpStatus(ApiCodes.Unauthorized));
        }

        var checker = context.HttpContext.RequestServices.GetRequiredService<IPermissionChecker>();
        var result = await checker.CheckAsync(userId, metadata.Code, context.HttpContext.RequestAborted);
        if (result == PermissionCheckResult.UserDisabled)
        {
            return Results.Json(
                ApiResult<object>.Error(ApiCodes.Unauthorized, "Authentication is required.", context.HttpContext.TraceIdentifier),
                statusCode: ApiCodes.ToHttpStatus(ApiCodes.Unauthorized));
        }

        if (result == PermissionCheckResult.Forbidden)
        {
            return Results.Json(
                ApiResult<object>.Error(ApiCodes.Forbidden, "Permission denied.", context.HttpContext.TraceIdentifier),
                statusCode: ApiCodes.ToHttpStatus(ApiCodes.Forbidden));
        }

        return await next(context);
    }
}
