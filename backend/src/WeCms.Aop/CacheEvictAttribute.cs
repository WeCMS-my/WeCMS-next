namespace WeCms.Aop;

[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class CacheEvictAttribute : Attribute
{
    public CacheEvictAttribute(string keyTemplate, CacheEvictionMode mode = CacheEvictionMode.Key)
    {
        if (string.IsNullOrWhiteSpace(keyTemplate))
        {
            throw new ArgumentException("Cache eviction key template is required.", nameof(keyTemplate));
        }

        KeyTemplate = keyTemplate;
        Mode = mode;
    }

    public string KeyTemplate { get; }

    public CacheEvictionMode Mode { get; }

    public int Order { get; init; } = 200;
}
