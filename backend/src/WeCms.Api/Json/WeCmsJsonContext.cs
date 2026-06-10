using System.Text.Json.Serialization;
using WeCms.Shared;
using WeCms.Modules.System.Auth;
using WeCms.Modules.System.System;

namespace WeCms.Api.Json;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(ApiResult<object?>))]
[JsonSerializable(typeof(ApiResult<LoginResponse>))]
[JsonSerializable(typeof(ApiResult<RefreshResponse>))]
[JsonSerializable(typeof(ApiResult<CurrentUserResponse>))]
[JsonSerializable(typeof(ApiResult<HealthLiveResponse>))]
[JsonSerializable(typeof(ApiResult<HealthReadyResponse>))]
[JsonSerializable(typeof(ApiResult<SystemPingResponse>))]
[JsonSerializable(typeof(ApiResult<SystemVersionResponse>))]
[JsonSerializable(typeof(ApiResult<DbCheckResponse>))]
internal sealed partial class WeCmsJsonContext : JsonSerializerContext
{
}
