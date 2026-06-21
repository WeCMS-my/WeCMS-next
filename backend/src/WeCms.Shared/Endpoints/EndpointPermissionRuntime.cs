using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace WeCms.Shared.Endpoints;

public enum EndpointPermissionCheckResult
{
    Allowed,
    UserDisabled,
    Forbidden
}

public interface IEndpointPermissionChecker
{
    Task<EndpointPermissionCheckResult> CheckAsync(long userId, string permissionCode, CancellationToken cancellationToken);
}

public interface IEndpointPermissionDeniedRecorder
{
    Task RecordAsync(
        long userId,
        string? username,
        string permissionCode,
        string ip,
        string reason,
        string traceId,
        CancellationToken cancellationToken);
}

public sealed class EndpointPermissionFilter : IEndpointFilter
{
    private readonly IEndpointPermissionChecker _checker;
    private readonly IEndpointPermissionDeniedRecorder _deniedRecorder;

    public EndpointPermissionFilter(
        IEndpointPermissionChecker checker,
        IEndpointPermissionDeniedRecorder deniedRecorder)
    {
        _checker = checker;
        _deniedRecorder = deniedRecorder;
    }

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var metadata = context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<EndpointPermissionMetadata>()
            ?? throw new InvalidOperationException("EndpointPermissionMetadata is required.");
        var userIdValue = context.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!long.TryParse(userIdValue, out var userId))
        {
            return Results.Json(
                ApiResult<object>.Error(ApiCodes.Unauthorized, "Authentication is required.", context.HttpContext.TraceIdentifier),
                statusCode: ApiCodes.ToHttpStatus(ApiCodes.Unauthorized));
        }

        var result = await _checker.CheckAsync(userId, metadata.PermissionCode, context.HttpContext.RequestAborted);
        if (result == EndpointPermissionCheckResult.UserDisabled)
        {
            await RecordDeniedAsync(
                context.HttpContext,
                userId,
                metadata.PermissionCode,
                "User account is disabled.",
                context.HttpContext.RequestAborted);
            return Results.Json(
                ApiResult<object>.Error(ApiCodes.Unauthorized, "User account is disabled.", context.HttpContext.TraceIdentifier),
                statusCode: ApiCodes.ToHttpStatus(ApiCodes.Unauthorized));
        }

        if (result == EndpointPermissionCheckResult.Forbidden)
        {
            await RecordDeniedAsync(
                context.HttpContext,
                userId,
                metadata.PermissionCode,
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
        return _deniedRecorder.RecordAsync(
            userId,
            context.User.Identity?.Name,
            permissionCode,
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            reason,
            context.TraceIdentifier,
            cancellationToken);
    }
}

public static class EndpointPermissionRuntimeExtensions
{
    public static RouteHandlerBuilder RequireEndpointPermission(
        this RouteHandlerBuilder builder,
        string permissionCode)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionCode);

        return builder
            .WithMetadata(new EndpointPermissionMetadata(permissionCode, EndpointPermissionKind.Api))
            .AddEndpointFilter<EndpointPermissionFilter>();
    }
}
