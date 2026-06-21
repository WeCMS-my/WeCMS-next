namespace WeCms.Modules.AccessControl.Permissions;

public sealed record UrlPermissionBinding
{
    public UrlPermissionBinding(
        string permissionCode,
        string module,
        string httpMethod,
        string routePattern)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(module);
        ArgumentException.ThrowIfNullOrWhiteSpace(httpMethod);
        ArgumentException.ThrowIfNullOrWhiteSpace(routePattern);

        PermissionCode = permissionCode;
        Module = module;
        HttpMethod = httpMethod.ToUpperInvariant();
        RoutePattern = routePattern;
    }

    public string PermissionCode { get; }

    public string Module { get; }

    public string HttpMethod { get; }

    public string RoutePattern { get; }
}
