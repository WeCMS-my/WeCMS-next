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
                detail),
            context.HttpContext.RequestAborted);
    }
}
