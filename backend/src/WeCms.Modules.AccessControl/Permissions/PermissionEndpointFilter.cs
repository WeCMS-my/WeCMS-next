using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using WeCms.Modules.AccessControl.Records;
using WeCms.Shared;

namespace WeCms.Modules.AccessControl.Permissions;

public sealed class PermissionEndpointFilter : IEndpointFilter
{
    private readonly IPermissionChecker _checker;
    private readonly IPermissionSecurityEventWriter _securityEventWriter;
    private readonly IAccessControlClock _clock;

    public PermissionEndpointFilter(
        IPermissionChecker checker,
        IPermissionSecurityEventWriter securityEventWriter,
        IAccessControlClock clock)
    {
        _checker = checker;
        _securityEventWriter = securityEventWriter;
        _clock = clock;
    }

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

        var result = await _checker.CheckAsync(userId, metadata.Code, context.HttpContext.RequestAborted);
        if (result == PermissionCheckResult.UserDisabled)
        {
            await RecordDeniedAsync(
                context.HttpContext,
                userId,
                metadata.Code,
                "User account is disabled.",
                context.HttpContext.RequestAborted);
            return Results.Json(
                ApiResult<object>.Error(ApiCodes.Unauthorized, "User account is disabled.", context.HttpContext.TraceIdentifier),
                statusCode: ApiCodes.ToHttpStatus(ApiCodes.Unauthorized));
        }

        if (result == PermissionCheckResult.Forbidden)
        {
            await RecordDeniedAsync(
                context.HttpContext,
                userId,
                metadata.Code,
                "Permission denied.",
                context.HttpContext.RequestAborted);
            return Results.Json(
                ApiResult<object>.Error(ApiCodes.Forbidden, "Permission denied.", context.HttpContext.TraceIdentifier),
                statusCode: ApiCodes.ToHttpStatus(ApiCodes.Forbidden));
        }

        return await next(context);
    }

    private Task RecordDeniedAsync(
        HttpContext context,
        long userId,
        string permissionCode,
        string reason,
        CancellationToken cancellationToken)
    {
        return _securityEventWriter.RecordAsync(
            new PermissionSecurityEventRecord(
                "permission_denied",
                userId,
                context.User.Identity?.Name,
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                $"{reason} Required permission: {permissionCode}.",
                _clock.UtcNow,
                context.TraceIdentifier),
            cancellationToken);
    }
}
