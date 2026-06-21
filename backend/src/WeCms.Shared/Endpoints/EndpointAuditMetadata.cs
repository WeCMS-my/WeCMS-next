namespace WeCms.Shared.Endpoints;

public sealed record EndpointAuditMetadata(
    string Module,
    string Resource,
    string Action);
