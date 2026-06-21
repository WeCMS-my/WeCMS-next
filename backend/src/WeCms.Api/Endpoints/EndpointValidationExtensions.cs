using Microsoft.AspNetCore.Builder;
using WeCms.Shared.Endpoints;

namespace WeCms.Api.Endpoints;

public static class EndpointValidationExtensions
{
    public static RouteHandlerBuilder Validate<TRequest>(this RouteHandlerBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithMetadata(new EndpointValidationMetadata(typeof(TRequest)));
    }
}
