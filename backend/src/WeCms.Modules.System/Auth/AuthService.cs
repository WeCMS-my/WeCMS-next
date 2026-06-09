 using System.Text;
 using WeCms.Shared.Contracts;
 using System.Security.Cryptography;
 using WeCms.Modules.System.Auth;
 
 namespace WeCms.Modules.System.Auth;
 
 public sealed class AuthService : IAuthService
 {
     private readonly ITokenService _tokenService;
     private readonly IPasswordHasher _passwordHasher;
     private readonly IDbConnectionFactory _db;
     private readonly ISecurityEventLogger? _eventLogger;
 
     public AuthService(ITokenService ts, IPasswordHasher ph, IDbConnectionFactory db, ISecurityEventLogger? eventLogger = null)
     { _tokenService = ts; _passwordHasher = ph; _db = db; _eventLogger = eventLogger; }
 
     public async Task<LoginResponse?> LoginAsync(string username, string password, CancellationToken ct)
     {
         await using var conn = await _db.OpenAsync(ct);
         var u = await conn.QueryFirstOrDefaultAsync<UserRecord>(new CommandDefinition(
             "SELECT id, username, display_name, password_hash, security_stamp, permission_version, status, two_factor_enabled FROM sys_user WHERE username=@U AND deleted_at IS NULL",
             new { U = username }, cancellationToken: ct));
         if (u is null || u.Status != "active" || !_passwordHasher.Verify(password, u.PasswordHash))
         { if (_eventLogger is not null) await _eventLogger.LogAsync("login_failed", "warning", u?.Id, username, null, "Invalid credentials", ct); return null; }
         var pair = _tokenService.GenerateTokenPair(new TokenPrincipal(u.Id, u.Username, u.SecurityStamp, u.PermissionVersion));
         await StoreRefreshToken(u.Id, pair.RefreshToken, ct);
         return new LoginResponse(pair.AccessToken, pair.RefreshToken, pair.ExpiresIn, u.TwoFactorEnabled);
     }
 
     public async Task<RefreshResponse?> RefreshTokenAsync(string refreshToken, CancellationToken ct)
     {
         var tokenHash = HashToken(refreshToken);
         await using var conn = await _db.OpenAsync(ct);
         var rec = await conn.QueryFirstOrDefaultAsync<RefreshTokenRecord>(new CommandDefinition(
             "SELECT srt.id, srt.user_id, srt.family_id, u.username, u.security_stamp, u.permission_version, u.status FROM sys_refresh_token srt JOIN sys_user u ON u.id=srt.user_id WHERE srt.token_hash=@H AND srt.revoked_at IS NULL AND srt.expires_at>@N", new { H = tokenHash, N = DateTime.UtcNow }, cancellationToken: ct));
         if (rec is null || rec.Status != "active") return null;
         await conn.ExecuteAsync(new CommandDefinition("UPDATE sys_refresh_token SET revoked_at=@N WHERE id=@Id", new { N = DateTime.UtcNow, rec.Id }, cancellationToken: ct));
         var pair = _tokenService.GenerateTokenPair(new TokenPrincipal(rec.UserId, rec.Username, rec.SecurityStamp, rec.PermissionVersion));
         await StoreRefreshToken(rec.UserId, pair.RefreshToken, ct);
         return new RefreshResponse(pair.AccessToken, pair.RefreshToken, pair.ExpiresIn);
     }
 
     public async Task LogoutAsync(string accessToken, CancellationToken ct)
     {
         var principal = _tokenService.ValidateAccessToken(accessToken);
         if (principal is null) return;
         await using var conn = await _db.OpenAsync(ct);
         await conn.ExecuteAsync(new CommandDefinition(
             "UPDATE sys_refresh_token SET revoked_at=@N WHERE user_id=@Uid AND revoked_at IS NULL",
             new { N = DateTime.UtcNow, Uid = principal.UserId }, cancellationToken: ct));
     }
 
     public async Task<CurrentUserResponse?> GetCurrentUserAsync(long userId, CancellationToken ct)
     {
         await using var conn = await _db.OpenAsync(ct);
         var u = await conn.QueryFirstOrDefaultAsync<CurrentUserRecord>(new CommandDefinition("SELECT id, username, display_name FROM sys_user WHERE id=@Id AND deleted_at IS NULL", new { Id = userId }, cancellationToken: ct));
         if (u is null) return null;
         var rolesTask = conn.QueryAsync<string>(new CommandDefinition("SELECT r.code FROM sys_role r JOIN sys_user_role ur ON ur.role_id=r.id WHERE ur.user_id=@Id AND r.status='active' AND r.deleted_at IS NULL", new { Id = userId }, cancellationToken: ct));
         var permsTask = conn.QueryAsync<string>(new CommandDefinition("SELECT DISTINCT p.code FROM sys_permission p JOIN sys_role_permission rp ON rp.permission_id=p.id JOIN sys_user_role ur ON ur.role_id=rp.role_id WHERE ur.user_id=@Id AND p.status='active'", new { Id = userId }, cancellationToken: ct));
         await Task.WhenAll(rolesTask, permsTask);
         return new CurrentUserResponse(u.Id, u.Username, u.DisplayName, rolesTask.Result.AsList().ToArray(), permsTask.Result.AsList().ToArray(), Array.Empty<object>());
     }
 
     private async Task StoreRefreshToken(long uid, string token, CancellationToken ct)
     { await using var c = await _db.OpenAsync(ct); await c.ExecuteAsync(new CommandDefinition("INSERT INTO sys_refresh_token (user_id,token_hash,family_id,expires_at,created_at) VALUES (@U,@H,@F,@E,@N)", new { U = uid, H = HashToken(token), F = Guid.NewGuid().ToString("N"), E = DateTime.UtcNow.AddDays(7), N = DateTime.UtcNow }, cancellationToken: ct)); }
 
     private static string HashToken(string t) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(t)));
     private sealed record UserRecord(long Id, string Username, string DisplayName, string PasswordHash, string SecurityStamp, long PermissionVersion, string Status, bool TwoFactorEnabled);
     private sealed record RefreshTokenRecord(long Id, long UserId, string FamilyId, string Username, string SecurityStamp, long PermissionVersion, string Status);
     private sealed record CurrentUserRecord(long Id, string Username, string DisplayName);
 }
