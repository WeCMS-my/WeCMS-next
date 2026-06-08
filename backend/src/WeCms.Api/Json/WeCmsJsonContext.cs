 using System.Text.Json.Serialization;
 using WeCms.Modules.System.Auth;
 using WeCms.Shared;
 
 namespace WeCms.Api.Json;
 
 [JsonSerializable(typeof(ApiResult<string>))]
 [JsonSerializable(typeof(ApiResult<object>))]
 [JsonSerializable(typeof(ApiResult<int>))]
 [JsonSerializable(typeof(ApiResult<LoginResponse>))]
 [JsonSerializable(typeof(ApiResult<RefreshResponse>))]
 [JsonSerializable(typeof(ApiResult<CurrentUserResponse>))]
 [JsonSerializable(typeof(LoginRequest))]
 [JsonSerializable(typeof(RefreshRequest))]
 [JsonSerializable(typeof(LoginResponse))]
 [JsonSerializable(typeof(RefreshResponse))]
 [JsonSerializable(typeof(CurrentUserResponse))]
 internal partial class WeCmsJsonContext : JsonSerializerContext
 {
 }
