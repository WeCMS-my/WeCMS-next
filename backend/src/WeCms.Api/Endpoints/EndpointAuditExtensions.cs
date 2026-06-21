using Microsoft.AspNetCore.Builder;
using WeCms.Shared.Endpoints;

namespace WeCms.Api.Endpoints;

public static class EndpointAuditExtensions
{
    public static RouteHandlerBuilder Audit(
        this RouteHandlerBuilder builder,
        string module,
        string resource,
        string action)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(module);
        ArgumentException.ThrowIfNullOrWhiteSpace(resource);
        ArgumentException.ThrowIfNullOrWhiteSpace(action);

        return builder.WithMetadata(new EndpointAuditMetadata(module, resource, action));
    }
}
