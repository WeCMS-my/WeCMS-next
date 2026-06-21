namespace WeCms.Caching;

public interface ICacheKeyBuilder
{
    string Build(CacheKeyParts parts);
}
