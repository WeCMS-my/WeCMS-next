namespace WeCms.Shared.Endpoints;

public sealed record EndpointPermissionMetadata(
    string PermissionCode,
    EndpointPermissionKind Kind);

public enum EndpointPermissionKind
{
    Api,
    Url,
    Button
}
