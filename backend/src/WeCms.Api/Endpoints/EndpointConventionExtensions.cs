using Microsoft.AspNetCore.Builder;
using WeCms.Shared.Endpoints;

namespace WeCms.Api.Endpoints;

public static class EndpointConventionExtensions
{
    public static RouteHandlerBuilder WithModule(this RouteHandlerBuilder builder, string moduleName)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(moduleName);

        return builder.WithMetadata(new EndpointModuleMetadata(moduleName));
    }
}
