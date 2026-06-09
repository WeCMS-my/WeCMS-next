 using System.Text;
 using WeCms.Shared.Contracts;
 using System.Security.Cryptography;
 using WeCms.Modules.System.Auth;
 
 namespace WeCms.Modules.System.Auth;
 
 public sealed class AuthService : IAuthService
 {
     private readonly ITokenService _ts;
     private readonly IPasswordHasher _ph;
     private readonly IDbConnectionFactory _db;
     private readonly ISecurityEventLogger? _el;
 
     public AuthService(ITokenService ts, IPasswordHasher ph, IDbConnectionFactory db, ISecurityEventLogger? el = null)
     { _ts = ts; _ph = ph; _db = db; _el = el; }
 
     public async Task<LoginResponse?> LoginAsync(string u, string p, CancellationToken ct)
     {
         await using var c = await _db.OpenAsync(ct);
         var r = await c.QueryFirstOrDefaultAsync<UserR>(new CommandDefinition(
             "SELECT id,username,display_name,password_hash,security_stamp,permission_version,status,two_factor_enabled FROM sys_user WHERE username=@U AND deleted_at IS NULL",
             new { U = u }, cancellationToken: ct));
         if (r is null || r.Status != "active" || !_ph.Verify(p, r.PasswordHash))
         { if (_el is not null) await _el.LogAsync("login_failed","warning",r?.Id,u,null,"Invalid credentials",ct); return null; }
         var pair = _ts.GenerateTokenPair(new TokenPrincipal(r.Id,r.Username,r.SecurityStamp,r.PermissionVersion));
         await StoreRefreshToken(r.Id, pair.RefreshToken, ct);
         return new LoginResponse(pair.AccessToken,pair.RefreshToken,pair.ExpiresIn,r.TwoFactorEnabled);
     }
 
     public async Task<RefreshResponse?> RefreshTokenAsync(string rt, CancellationToken ct)
     {
         var h = HashToken(rt);
         await using var c = await _db.OpenAsync(ct);
         var r = await c.QueryFirstOrDefaultAsync<RefreshR>(new CommandDefinition(
             "SELECT srt.id,srt.user_id,srt.family_id,u.username,u.security_stamp,u.permission_version,u.status FROM sys_refresh_token srt JOIN sys_user u ON u.id=srt.user_id WHERE srt.token_hash=@H AND srt.revoked_at IS NULL AND srt.expires_at>@N",
             new { H = h, N = DateTime.UtcNow }, cancellationToken: ct));
         if (r is null || r.Status != "active") return null;
         await c.ExecuteAsync(new CommandDefinition(
             "UPDATE sys_refresh_token SET revoked_at=@N WHERE id=@Id OR (family_id=@F AND id!=@Id AND revoked_at IS NULL)",
             new { N = DateTime.UtcNow, r.Id, F = r.FamilyId }, cancellationToken: ct));
         var pair = _ts.GenerateTokenPair(new TokenPrincipal(r.UserId,r.Username,r.SecurityStamp,r.PermissionVersion));
         await StoreRefreshToken(r.UserId, pair.RefreshToken, ct);
         return new RefreshResponse(pair.AccessToken,pair.RefreshToken,pair.ExpiresIn);
     }
 
     public async Task LogoutAsync(string at, CancellationToken ct)
     { var p = _ts.ValidateAccessToken(at); if (p is null) return; await using var c = await _db.OpenAsync(ct); await c.ExecuteAsync(new CommandDefinition("UPDATE sys_refresh_token SET revoked_at=@N WHERE user_id=@U AND revoked_at IS NULL", new { N=DateTime.UtcNow, U=p.UserId }, cancellationToken: ct)); }
 
     public async Task<CurrentUserResponse?> GetCurrentUserAsync(long uid, CancellationToken ct)
     { await using var c = await _db.OpenAsync(ct); var u = await c.QueryFirstOrDefaultAsync<CurU>(new CommandDefinition("SELECT id,username,display_name FROM sys_user WHERE id=@Id AND deleted_at IS NULL",new{Id=uid},cancellationToken:ct)); if(u is null)return null; var rt=c.QueryAsync<string>(new CommandDefinition("SELECT r.code FROM sys_role r JOIN sys_user_role ur ON ur.role_id=r.id WHERE ur.user_id=@Id AND r.status='active' AND r.deleted_at IS NULL",new{Id=uid},cancellationToken:ct)); var pt=c.QueryAsync<string>(new CommandDefinition("SELECT DISTINCT p.code FROM sys_permission p JOIN sys_role_permission rp ON rp.permission_id=p.id JOIN sys_user_role ur ON ur.role_id=rp.role_id WHERE ur.user_id=@Id AND p.status='active'",new{Id=uid},cancellationToken:ct)); await Task.WhenAll(rt,pt); return new CurrentUserResponse(u.Id,u.Username,u.DisplayName,rt.Result.AsList().ToArray(),pt.Result.AsList().ToArray(),Array.Empty<object>()); }
 
     private async Task StoreRefreshToken(long uid, string t, CancellationToken ct)
     { await using var c = await _db.OpenAsync(ct); await c.ExecuteAsync(new CommandDefinition("INSERT INTO sys_refresh_token (user_id,token_hash,family_id,expires_at,created_at) VALUES (@U,@H,@F,@E,@N)", new{U=uid,H=HashToken(t),F=Guid.NewGuid().ToString("N"),E=DateTime.UtcNow.AddDays(7),N=DateTime.UtcNow},cancellationToken:ct)); }
     private static string HashToken(string t) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(t)));
     private sealed record UserR(long Id,string Username,string DisplayName,string PasswordHash,string SecurityStamp,long PermissionVersion,string Status,bool TwoFactorEnabled);
     private sealed record RefreshR(long Id,long UserId,string FamilyId,string Username,string SecurityStamp,long PermissionVersion,string Status);
     private sealed record CurU(long Id,string Username,string DisplayName);
 }
