using System.Globalization;
using System.Security.Claims;
using WeCms.Shared.Endpoints;

namespace WeCms.Api.Endpoints;

public sealed class AuditEndpointFilter : IEndpointFilter
{
    private readonly IAuditWriter _auditWriter;

    public AuditEndpointFilter(IAuditWriter auditWriter)
    {
        _auditWriter = auditWriter;
    }

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var metadata = context.HttpContext.GetEndpoint()?.Metadata.GetMetadata<EndpointAuditMetadata>();
        if (metadata is null)
        {
            return await next(context);
        }

        await WriteAsync(context, metadata, AuditWriteStatus.Started, detail: "");

        try
        {
            var result = await next(context);
            await WriteAsync(context, metadata, AuditWriteStatus.Completed, detail: "");

            return result;
        }
        catch (Exception exception)
        {
            await WriteAsync(context, metadata, AuditWriteStatus.Failed, exception.Message);
            throw;
        }
    }

    private ValueTask WriteAsync(
        EndpointFilterInvocationContext context,
        EndpointAuditMetadata metadata,
        AuditWriteStatus status,
        string detail)
    {
        return _auditWriter.WriteAsync(
            new AuditWriteRecord(
                metadata.Module,
                metadata.Resource,
                metadata.Action,
                status,
                context.HttpContext.Request.Method,
                context.HttpContext.Request.Path.Value ?? "",
                context.HttpContext.TraceIdentifier,
                detail,
                UserId: CurrentUserId(context.HttpContext),
                Username: CurrentUsername(context.HttpContext),
                IpAddress: context.HttpContext.Connection.RemoteIpAddress?.ToString(),
                UserAgent: context.HttpContext.Request.Headers.UserAgent.ToString(),
                TargetId: TargetId(context.HttpContext)),
            context.HttpContext.RequestAborted);
    }

    private static long? CurrentUserId(HttpContext httpContext)
    {
        var value = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var userId)
            ? userId
            : null;
    }

    private static string? CurrentUsername(HttpContext httpContext)
    {
        var value = httpContext.User.FindFirstValue(ClaimTypes.Name)
            ?? httpContext.User.Identity?.Name;
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string? TargetId(HttpContext httpContext)
    {
        foreach (var key in new[] { "id", "key", "typeCode", "code" })
        {
            if (httpContext.Request.RouteValues.TryGetValue(key, out var value) && value is not null)
            {
                var text = Convert.ToString(value, CultureInfo.InvariantCulture);
                return string.IsNullOrWhiteSpace(text) ? null : text;
            }
        }

        return null;
    }
}
