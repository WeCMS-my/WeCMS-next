 namespace WeCms.Modules.System.Auth;
 
 public sealed record LoginRequest(string Username, string Password);
 
 public sealed record LoginResponse(
     string? AccessToken,
     string? RefreshToken,
     long ExpiresIn,
     bool RequiresTwoFactor,
     string? TwoFactorTicket = null
 );
 
 public sealed record RefreshRequest(string RefreshToken);
 
 public sealed record RefreshResponse(
     string AccessToken,
     string RefreshToken,
     long ExpiresIn
 );
 
 public sealed record CurrentUserResponse(
     long Id,
     string Username,
     string DisplayName,
     string[] Roles,
     string[] Permissions,
     object[] Menus
 );
