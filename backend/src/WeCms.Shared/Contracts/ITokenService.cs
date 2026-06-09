 namespace WeCms.Shared.Contracts;
 
 public sealed record TokenPair(string AccessToken, string RefreshToken, long ExpiresIn);
 
 public sealed record TokenPrincipal(long UserId, string Username, string SecurityStamp, long PermissionVersion, bool IsSuperAdmin = false);
 
 public interface ITokenService
 {
     TokenPair GenerateTokenPair(TokenPrincipal principal);
     TokenPrincipal? ValidateAccessToken(string accessToken);
 }
