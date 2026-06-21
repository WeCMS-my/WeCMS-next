using System.Security.Cryptography;
using System.Text.Json;
using WeCms.Caching;

namespace WeCms.Aop;

public sealed class CacheInterceptor
{
    private static readonly JsonSerializerOptions HashJsonOptions = new(JsonSerializerDefaults.Web);

    private readonly ICache cache;
    private readonly ICacheKeyBuilder keyBuilder;

    public CacheInterceptor(ICache cache, ICacheKeyBuilder keyBuilder)
    {
        this.cache = cache ?? throw new ArgumentNullException(nameof(cache));
        this.keyBuilder = keyBuilder ?? throw new ArgumentNullException(nameof(keyBuilder));
    }

    public string BuildKey(CacheableAttribute attribute, CacheInvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(attribute);
        return BuildKey(attribute.KeyTemplate, context);
    }

    public string BuildKey(CacheEvictAttribute attribute, CacheInvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(attribute);
        return BuildKey(attribute.KeyTemplate, context);
    }

    public async Task<TResult?> InvokeCacheableAsync<TResult>(
        CacheableAttribute attribute,
        CacheInvocationContext context,
        Func<CancellationToken, Task<TResult?>> operation,
        CacheEntryOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        var key = BuildKey(attribute, context);
        return await cache.GetOrCreateAsync(
            key,
            async token => await operation(token),
            options,
            cancellationToken);
    }

    public async Task InvokeEvictAsync(
        CacheEvictAttribute attribute,
        CacheInvocationContext context,
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        await operation(cancellationToken);
        await EvictAsync(attribute, context, cancellationToken);
    }

    public async Task<TResult> InvokeEvictAsync<TResult>(
        CacheEvictAttribute attribute,
        CacheInvocationContext context,
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        var result = await operation(cancellationToken);
        await EvictAsync(attribute, context, cancellationToken);

        return result;
    }

    private async ValueTask EvictAsync(
        CacheEvictAttribute attribute,
        CacheInvocationContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(attribute);

        if (attribute.Mode == CacheEvictionMode.Prefix)
        {
            await cache.RemoveByPrefixAsync(BuildPrefix(attribute.KeyTemplate, context), cancellationToken);
            return;
        }

        await cache.RemoveAsync(BuildKey(attribute, context), cancellationToken);
    }

    private string BuildKey(string keyTemplate, CacheInvocationContext context)
    {
        var template = CacheKeyTemplate.Parse(keyTemplate);
        var parameterHash = HashParameters(context);

        return keyBuilder.Build(new CacheKeyParts(
            Tenant: Required(context.Tenant, nameof(CacheInvocationContext.Tenant)),
            Module: template.Module,
            Resource: template.Resource,
            Identifier: $"{template.Identifier}-{parameterHash}",
            Version: context.Version));
    }

    private string BuildPrefix(string keyTemplate, CacheInvocationContext context)
    {
        var template = CacheKeyTemplate.Parse(keyTemplate);
        var wildcardKey = keyBuilder.Build(new CacheKeyParts(
            Tenant: Required(context.Tenant, nameof(CacheInvocationContext.Tenant)),
            Module: template.Module,
            Resource: template.Resource,
            Identifier: "*",
            Version: context.Version));

        return wildcardKey[..^1];
    }

    private static string HashParameters(CacheInvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(context.Parameters);

        var payload = JsonSerializer.SerializeToUtf8Bytes(context.Parameters, HashJsonOptions);
        var hash = SHA256.HashData(payload);

        return Convert.ToHexString(hash)[..16].ToLowerInvariant();
    }

    private static string Required(string? value, string name)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Cache invocation value is required.", name);
        }

        return value.Trim();
    }

    private sealed record CacheKeyTemplate(string Module, string Resource, string Identifier)
    {
        public static CacheKeyTemplate Parse(string keyTemplate)
        {
            var parts = Required(keyTemplate, nameof(keyTemplate)).Split(':');
            if (parts.Length != 3)
            {
                throw new ArgumentException("Cache key template must use 'module:resource:identifier'.", nameof(keyTemplate));
            }

            return new CacheKeyTemplate(
                Required(parts[0], nameof(Module)),
                Required(parts[1], nameof(Resource)),
                Required(parts[2], nameof(Identifier)));
        }
    }
}
