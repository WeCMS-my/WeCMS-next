namespace WeCms.Caching;

public sealed record RedisCacheOptions
{
    public bool Enabled { get; set; }

    public string ConnectionStringName { get; set; } = "Redis";

    public string InstanceName { get; set; } = "wecms";
}
