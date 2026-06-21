namespace WeCms.Caching;

public interface ICacheInvalidator
{
    ValueTask RemoveByPrefixAsync(string prefix, CancellationToken cancellationToken = default);
}
