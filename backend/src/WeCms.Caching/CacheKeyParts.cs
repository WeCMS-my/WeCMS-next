namespace WeCms.Caching;

public sealed record CacheKeyParts(
    string Tenant,
    string Module,
    string Resource,
    string Identifier,
    string? Version = null);
