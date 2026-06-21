namespace WeCms.Caching;

public sealed record CacheOptions
{
    public string ApplicationName { get; set; } = "wecms";

    public string EnvironmentName { get; set; } = "local";

    public string Version { get; set; } = "v1";

    public bool CacheNullValues { get; set; }
}
