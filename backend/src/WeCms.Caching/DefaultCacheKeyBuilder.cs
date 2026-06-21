using System.Globalization;

namespace WeCms.Caching;

public sealed class DefaultCacheKeyBuilder : ICacheKeyBuilder
{
    private readonly CacheOptions options;

    public DefaultCacheKeyBuilder(CacheOptions options)
    {
        this.options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public string Build(CacheKeyParts parts)
    {
        ArgumentNullException.ThrowIfNull(parts);

        var app = RequiredSegment(options.ApplicationName, nameof(CacheOptions.ApplicationName));
        var environment = RequiredSegment(options.EnvironmentName, nameof(CacheOptions.EnvironmentName));
        var tenant = RequiredSegment(parts.Tenant, nameof(CacheKeyParts.Tenant));
        var module = RequiredSegment(parts.Module, nameof(CacheKeyParts.Module));
        var resource = RequiredSegment(parts.Resource, nameof(CacheKeyParts.Resource));
        var version = RequiredSegment(parts.Version ?? options.Version, nameof(CacheOptions.Version));
        var identifier = RequiredSegment(parts.Identifier, nameof(CacheKeyParts.Identifier));

        return string.Join(
            ':',
            app,
            environment,
            tenant,
            module,
            resource,
            version,
            identifier);
    }

    private static string RequiredSegment(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Cache key segment is required.", name);
        }

        var segment = value.Trim();
        if (segment.Contains(':', StringComparison.Ordinal))
        {
            throw new ArgumentException(
                string.Create(CultureInfo.InvariantCulture, $"Cache key segment '{name}' must not contain ':'."),
                name);
        }

        return segment;
    }
}
