namespace WeCms.Aop;

[AttributeUsage(AttributeTargets.Interface | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class CacheableAttribute : Attribute
{
    public CacheableAttribute(string keyTemplate)
    {
        if (string.IsNullOrWhiteSpace(keyTemplate))
        {
            throw new ArgumentException("Cache key template is required.", nameof(keyTemplate));
        }

        KeyTemplate = keyTemplate;
    }

    public string KeyTemplate { get; }

    public int Order { get; init; } = 100;
}
