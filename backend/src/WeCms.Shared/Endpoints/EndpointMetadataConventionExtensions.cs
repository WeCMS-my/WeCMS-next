using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace WeCms.Shared.Endpoints;

public static class EndpointMetadataConventionExtensions
{
    public static RouteGroupBuilder WithEndpointModule(this RouteGroupBuilder builder, string module)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(module);

        return builder.WithMetadata(new EndpointModuleMetadata(module));
    }

    public static RouteGroupBuilder AuditWriteEndpoints(this RouteGroupBuilder builder, string module, string resource)
    {
        ArgumentNullException.ThrowIfNull(builder);
        ArgumentException.ThrowIfNullOrWhiteSpace(module);
        ArgumentException.ThrowIfNullOrWhiteSpace(resource);

        ((IEndpointConventionBuilder)builder).Add(endpointBuilder =>
        {
            if (endpointBuilder.Metadata.OfType<EndpointAuditMetadata>().Any())
            {
                return;
            }

            var method = endpointBuilder.Metadata
                .OfType<IHttpMethodMetadata>()
                .SelectMany(static metadata => metadata.HttpMethods)
                .FirstOrDefault(static method => !string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase));
            if (method is null)
            {
                return;
            }

            var routePattern = endpointBuilder is RouteEndpointBuilder routeEndpointBuilder
                ? routeEndpointBuilder.RoutePattern.RawText ?? string.Empty
                : string.Empty;
            endpointBuilder.Metadata.Add(new EndpointAuditMetadata(module, resource, AuditAction(method, routePattern)));
        });

        return builder;
    }

    private static string AuditAction(string method, string routePattern)
    {
        if (routePattern.EndsWith("/enable", StringComparison.OrdinalIgnoreCase))
        {
            return "enable";
        }

        if (routePattern.EndsWith("/disable", StringComparison.OrdinalIgnoreCase))
        {
            return "disable";
        }

        if (routePattern.EndsWith("/sort", StringComparison.OrdinalIgnoreCase))
        {
            return "sort";
        }

        if (routePattern.EndsWith("/reset-password", StringComparison.OrdinalIgnoreCase))
        {
            return "reset-password";
        }

        if (routePattern.EndsWith("/reset-2fa", StringComparison.OrdinalIgnoreCase))
        {
            return "reset-2fa";
        }

        if (routePattern.EndsWith("/roles", StringComparison.OrdinalIgnoreCase))
        {
            return "assign-role";
        }

        if (routePattern.EndsWith("/positions", StringComparison.OrdinalIgnoreCase))
        {
            return "assign-position";
        }

        if (routePattern.EndsWith("/permissions", StringComparison.OrdinalIgnoreCase))
        {
            return "assign-permission";
        }

        if (routePattern.EndsWith("/menus", StringComparison.OrdinalIgnoreCase))
        {
            return "assign-menu";
        }

        if (routePattern.EndsWith("/validate-ip-rules", StringComparison.OrdinalIgnoreCase)
            || routePattern.EndsWith("/reload-cache", StringComparison.OrdinalIgnoreCase)
            || routePattern.EndsWith("/batch-unban", StringComparison.OrdinalIgnoreCase)
            || routePattern.EndsWith("/recovery-codes/regenerate", StringComparison.OrdinalIgnoreCase)
            || routePattern.EndsWith("/recovery-code", StringComparison.OrdinalIgnoreCase)
            || routePattern.EndsWith("/verify", StringComparison.OrdinalIgnoreCase)
            || routePattern.EndsWith("/confirm", StringComparison.OrdinalIgnoreCase)
            || routePattern.EndsWith("/setup", StringComparison.OrdinalIgnoreCase)
            || routePattern.EndsWith("/unban", StringComparison.OrdinalIgnoreCase)
            || routePattern.EndsWith("/switch", StringComparison.OrdinalIgnoreCase)
            || routePattern.EndsWith("/login", StringComparison.OrdinalIgnoreCase)
            || routePattern.EndsWith("/refresh", StringComparison.OrdinalIgnoreCase)
            || routePattern.EndsWith("/logout", StringComparison.OrdinalIgnoreCase))
        {
            return LastSegment(routePattern);
        }

        return method.ToUpperInvariant() switch
        {
            "POST" => "create",
            "PUT" => "update",
            "PATCH" => "update",
            "DELETE" => "delete",
            _ => method.ToLowerInvariant()
        };
    }

    private static string LastSegment(string routePattern)
    {
        var index = routePattern.LastIndexOf('/');
        return index < 0 ? routePattern : routePattern[(index + 1)..];
    }
}
