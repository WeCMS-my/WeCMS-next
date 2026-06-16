using System.Text.Json.Serialization;
using WeCms.Modules.System.Auth;
using WeCms.Modules.System.Permissions;
using WeCms.Modules.System.System;
using WeCms.Shared;

namespace WeCms.Api.Json;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(ApiResult<object>))]
[JsonSerializable(typeof(ApiResult<LoginResponse>))]
[JsonSerializable(typeof(ApiResult<AuthMeResponse>))]
[JsonSerializable(typeof(ApiResult<SecurePingResponse>))]
[JsonSerializable(typeof(ApiResult<SystemLiveResponse>))]
[JsonSerializable(typeof(ApiResult<SystemReadyResponse>))]
[JsonSerializable(typeof(ApiResult<SystemPingResponse>))]
[JsonSerializable(typeof(ApiResult<SystemVersionResponse>))]
[JsonSerializable(typeof(ApiResult<SystemDbCheckResponse>))]
[JsonSerializable(typeof(LoginRequest))]
[JsonSerializable(typeof(RefreshTokenRequest))]
[JsonSerializable(typeof(LogoutRequest))]
[JsonSerializable(typeof(LoginResponse))]
[JsonSerializable(typeof(AuthMeResponse))]
[JsonSerializable(typeof(SecurePingResponse))]
[JsonSerializable(typeof(SystemLiveResponse))]
[JsonSerializable(typeof(SystemReadyResponse))]
[JsonSerializable(typeof(SystemPingResponse))]
[JsonSerializable(typeof(SystemVersionResponse))]
[JsonSerializable(typeof(SystemDbCheckResponse))]
[JsonSerializable(typeof(AuthUserDto))]
[JsonSerializable(typeof(AuthMenuDto))]
[JsonSerializable(typeof(IReadOnlyList<string>))]
[JsonSerializable(typeof(IReadOnlyList<AuthMenuDto>))]
[JsonSerializable(typeof(IReadOnlyDictionary<string, string[]>))]
[JsonSerializable(typeof(Dictionary<string, string[]>))]
public sealed partial class WeCmsJsonSerializerContext : JsonSerializerContext;
