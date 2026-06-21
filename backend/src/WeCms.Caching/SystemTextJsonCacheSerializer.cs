using System.Text.Json;

namespace WeCms.Caching;

public sealed class SystemTextJsonCacheSerializer : ICacheSerializer
{
    private static readonly JsonSerializerOptions DefaultOptions = new(JsonSerializerDefaults.Web);

    private readonly JsonSerializerOptions options;

    public SystemTextJsonCacheSerializer()
        : this(DefaultOptions)
    {
    }

    public SystemTextJsonCacheSerializer(JsonSerializerOptions options)
    {
        this.options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public byte[] Serialize<T>(T? value)
    {
        return JsonSerializer.SerializeToUtf8Bytes(value, options);
    }

    public T? Deserialize<T>(ReadOnlySpan<byte> value)
    {
        return JsonSerializer.Deserialize<T>(value, options);
    }
}
