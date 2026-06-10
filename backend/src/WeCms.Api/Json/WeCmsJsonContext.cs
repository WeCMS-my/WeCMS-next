using System.Text.Json.Serialization;
using WeCms.Shared;

namespace WeCms.Api.Json;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(ApiResult<object?>))]
internal sealed partial class WeCmsJsonContext : JsonSerializerContext
{
}
