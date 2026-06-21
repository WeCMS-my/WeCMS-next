using Microsoft.AspNetCore.Builder;
using WeCms.Shared;

namespace WeCms.Api.Endpoints;

public static class EndpointOpenApiExtensions
{
    public static RouteHandlerBuilder ProducesApi<TResponse>(this RouteHandlerBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithMetadata(new OpenApiResponseMetadata(typeof(TResponse)));
    }
}
