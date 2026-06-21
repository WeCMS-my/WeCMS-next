namespace WeCms.Aop;

public sealed record CacheInvocationContext(
    string Tenant,
    IReadOnlyList<object?> Parameters,
    string? Version = null);
