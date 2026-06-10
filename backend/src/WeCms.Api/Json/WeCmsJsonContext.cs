using System.Text.Json.Serialization;
using WeCms.Shared;
using WeCms.Modules.System.Auth;

namespace WeCms.Api.Json;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(ApiResult<object?>))]
[JsonSerializable(typeof(ApiResult<LoginResponse>))]
[JsonSerializable(typeof(ApiResult<RefreshResponse>))]
[JsonSerializable(typeof(ApiResult<CurrentUserResponse>))]
internal sealed partial class WeCmsJsonContext : JsonSerializerContext
{
}
