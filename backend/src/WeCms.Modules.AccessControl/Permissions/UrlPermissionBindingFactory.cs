using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using WeCms.Shared.Endpoints;

namespace WeCms.Modules.AccessControl.Permissions;

public static class UrlPermissionBindingFactory
{
    public static IReadOnlyList<UrlPermissionBinding> FromEndpoints(IEnumerable<Endpoint> endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        return endpoints.SelectMany(FromEndpoint).ToArray();
    }

    public static IReadOnlyList<UrlPermissionBinding> FromEndpoint(Endpoint endpoint)
    {
        ArgumentNullException.ThrowIfNull(endpoint);

        var urlPermissions = endpoint.Metadata
            .OfType<EndpointPermissionMetadata>()
            .Where(static metadata => metadata.Kind == EndpointPermissionKind.Url)
            .ToArray();
        if (urlPermissions.Length == 0)
        {
            return [];
        }

        if (endpoint is not RouteEndpoint routeEndpoint)
        {
            throw new InvalidOperationException("URL permission endpoint must be a route endpoint.");
        }

        var module = endpoint.Metadata.GetMetadata<EndpointModuleMetadata>()?.Module
            ?? throw new InvalidOperationException("URL permission endpoint must declare module metadata.");
        var methods = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods
            ?? throw new InvalidOperationException("URL permission endpoint must declare HTTP methods.");
        if (methods.Count == 0)
        {
            throw new InvalidOperationException("URL permission endpoint must declare HTTP methods.");
        }

        var routePattern = routeEndpoint.RoutePattern.RawText
            ?? throw new InvalidOperationException("URL permission endpoint must declare a route pattern.");
        var bindings = new List<UrlPermissionBinding>(urlPermissions.Length * methods.Count);
        foreach (var permission in urlPermissions)
        {
            foreach (var method in methods)
            {
                bindings.Add(new UrlPermissionBinding(permission.PermissionCode, module, method, routePattern));
            }
        }

        return bindings;
    }
}
