namespace WeCms.Caching;

public sealed record CacheEntryOptions
{
    public TimeSpan? AbsoluteExpirationRelativeToNow { get; set; }

    public TimeSpan? SlidingExpiration { get; set; }

    public bool CacheNullValues { get; set; }

    public IReadOnlyCollection<string> Tags { get; set; } = Array.Empty<string>();
}
