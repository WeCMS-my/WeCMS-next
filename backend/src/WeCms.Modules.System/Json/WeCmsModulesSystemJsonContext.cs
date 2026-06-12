using System.Text.Json.Serialization;
using WeCms.Shared;
using WeCms.Modules.System.Auth;
using WeCms.Modules.System.System;

namespace WeCms.Modules.System;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(ApiResult<object?>))]
[JsonSerializable(typeof(LoginRequest))]
[JsonSerializable(typeof(CaptchaChallengeResponse))]
[JsonSerializable(typeof(RefreshRequest))]
[JsonSerializable(typeof(LogoutRequest))]
[JsonSerializable(typeof(VerifyTwoFactorRequest))]
[JsonSerializable(typeof(ApiResult<LoginResponse>))]
[JsonSerializable(typeof(ApiResult<CaptchaChallengeResponse>))]
[JsonSerializable(typeof(ApiResult<RefreshResponse>))]
[JsonSerializable(typeof(ApiResult<VerifyTwoFactorResponse>))]
[JsonSerializable(typeof(ApiResult<CurrentUserResponse>))]
[JsonSerializable(typeof(ApiResult<HealthLiveResponse>))]
[JsonSerializable(typeof(ApiResult<HealthReadyResponse>))]
[JsonSerializable(typeof(ApiResult<SystemPingResponse>))]
[JsonSerializable(typeof(ApiResult<SystemVersionResponse>))]
[JsonSerializable(typeof(ApiResult<DbCheckResponse>))]
[JsonSerializable(typeof(ApiResult<SecurePingResponse>))]
[JsonSerializable(typeof(SecurePingResponse))]
internal sealed partial class WeCmsModulesSystemJsonContext : JsonSerializerContext
{
}
