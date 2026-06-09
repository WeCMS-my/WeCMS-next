using System.Text;
using System.Threading;
using WeCms.Shared.Contracts;
using System.Security.Cryptography;
using System.Collections.Concurrent;

namespace WeCms.Modules.System.Auth;

public sealed class AuthService : IAuthService
{
    private readonly ITokenService _ts;
    private readonly IPasswordHasher _ph;
    private readonly IDbConnectionFactory _db;
    private readonly ISecurityEventLogger _el;
    private readonly IClock _clock;

    // M0: static fields for 2FA ticket storage; extract to ITwoFactorTicketStore singleton in M1
    private static IClock _s_clock = null!;

    private static readonly ConcurrentDictionary<string, TwoFactorTicketData> _tickets = new();

    private static readonly Timer _ticketCleanupTimer = new(_ =>
    {
        if (_s_clock is null) return;
        var now = _s_clock.UtcNow.DateTime;
        foreach (var kv in _tickets)
        {
            if (kv.Value.ExpiresAt < now)
                _tickets.TryRemove(kv);
        }
    }, null, TimeSpan.FromSeconds(60), TimeSpan.FromSeconds(60));

    public AuthService(ITokenService ts, IPasswordHasher ph, IDbConnectionFactory db, ISecurityEventLogger el, IClock clock)
    { _ts = ts; _ph = ph; _db = db; _el = el; _clock = clock; _s_clock = clock; }

    public async Task<LoginResponse?> LoginAsync(string u, string p, string ip, CancellationToken ct)
    {
        await using var c = await _db.OpenAsync(ct);
        var r = await c.QueryFirstOrDefaultAsync<UserR>(new CommandDefinition(
            "SELECT id,username,display_name,password_hash,security_stamp,permission_version,status,two_factor_enabled,is_super_admin FROM sys_user WHERE username=@U AND deleted_at IS NULL",
            new { U = u }, cancellationToken: ct));
        if (r is null || r.Status != "active" || !_ph.Verify(p, r.PasswordHash))
        { await _el.LogAsync("login_failed","warning",r?.Id,u,ip,"Invalid credentials",ct); return null; }

        if (r.TwoFactorEnabled)
        {
            await c.ExecuteAsync(new CommandDefinition(
                "INSERT INTO sys_login_log (user_id,username,login_type,status,ip,created_at) VALUES (@Id,@U,'password','2fa_required',@Ip,@N)",
                new { r.Id, U = r.Username, Ip = ip ?? "unknown", N = _clock.UtcNow.DateTime }, cancellationToken: ct));
            var ticket = GenerateTicket();
            _tickets[ticket] = new TwoFactorTicketData(r.Id, r.Username, r.SecurityStamp, r.PermissionVersion, r.IsSuperAdmin, _clock.UtcNow.DateTime.AddMinutes(5));
            return new LoginResponse(null, null, 0, true, ticket);
        }

        // Update login info
        await c.ExecuteAsync(new CommandDefinition(
            "UPDATE sys_user SET last_login_at=@N, last_login_ip=@Ip WHERE id=@Id",
            new { N = _clock.UtcNow.DateTime, Ip = ip ?? "unknown", Id = r.Id }, cancellationToken: ct));
        await c.ExecuteAsync(new CommandDefinition(
            "INSERT INTO sys_login_log (user_id,username,login_type,status,ip,user_agent,created_at) VALUES (@Id,@U,'password','success',@Ip,'',@N)",
            new { r.Id, U = r.Username, Ip = ip ?? "unknown", N = _clock.UtcNow.DateTime }, cancellationToken: ct));

        var pair = _ts.GenerateTokenPair(new TokenPrincipal(r.Id,r.Username,r.SecurityStamp,r.PermissionVersion,r.IsSuperAdmin));
        await StoreRefreshToken(c, r.Id, pair.RefreshToken, ip, null, ct);
        return new LoginResponse(pair.AccessToken,pair.RefreshToken,pair.ExpiresIn,false);
    }

    public async Task<LoginResponse?> VerifyTwoFactorAndLoginAsync(string? ticket, string username, string code, ITwoFactorService twoFactor, CancellationToken ct)
    {
        if (!string.IsNullOrWhiteSpace(ticket))
        {
            if (!_tickets.TryRemove(ticket, out var data) || data.ExpiresAt < _clock.UtcNow.DateTime)
                return null;

            await using var c = await _db.OpenAsync(ct);
            var row = await c.QueryFirstOrDefaultAsync<TwoFactorRow>(new CommandDefinition(
                "SELECT two_factor_secret, two_factor_last_used_ts FROM sys_user WHERE id=@Id AND deleted_at IS NULL AND two_factor_enabled=1",
                new { Id = data.UserId }, cancellationToken: ct));
            if (row is null || string.IsNullOrEmpty(row.Secret)) return null;

            // TOTP replay protection: same time window cannot be reused
            var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var step = now / 30;
            if (row.LastUsedStep == step) return null;

            if (!twoFactor.Verify(row.Secret, code)) return null;

            // Update last used step
            await c.ExecuteAsync(new CommandDefinition(
                "UPDATE sys_user SET two_factor_last_used_ts=@S WHERE id=@Id",
                new { S = step, Id = data.UserId }, cancellationToken: ct));

            // Update login info
            await c.ExecuteAsync(new CommandDefinition(
                "UPDATE sys_user SET last_login_at=@N WHERE id=@Id",
                new { N = _clock.UtcNow.DateTime, Id = data.UserId }, cancellationToken: ct));

            var pair = _ts.GenerateTokenPair(new TokenPrincipal(data.UserId, data.Username, data.SecurityStamp, data.PermissionVersion, data.IsSuperAdmin));
            // M0: IP/UA not available in ticket flow; add to ticket data in M1
            await StoreRefreshToken(c, data.UserId, pair.RefreshToken, "", "", ct);
            return new LoginResponse(pair.AccessToken, pair.RefreshToken, pair.ExpiresIn, false);
        }

        // Legacy flow removed: 2FA verification now requires a valid ticket from login
        return null;
    }

    public async Task<RefreshResponse?> RefreshTokenAsync(string rt, CancellationToken ct)
    {
        var h = HashToken(rt);
        await using var c = await _db.OpenAsync(ct);
        await using var tx = await c.BeginTransactionAsync(ct);

        // Check if token exists (may be revoked)
        // Note: intentionally no WHERE srt.revoked_at IS NULL - we check revoked status programmatically for reuse detection
        var revoked = await c.QueryFirstOrDefaultAsync<RefreshR>(new CommandDefinition(
            "SELECT srt.id,srt.user_id,srt.family_id,srt.revoked_at IS NOT NULL AS revoked,srt.expires_at,u.username,u.security_stamp,u.permission_version,u.status,u.is_super_admin FROM sys_refresh_token srt JOIN sys_user u ON u.id=srt.user_id WHERE srt.token_hash=@H",
            new { H = h }, cancellationToken: ct));

        if (revoked is null)
        {
            await tx.RollbackAsync(ct);
            return null;
        }

        // Reuse detection: token was already revoked
        if (revoked.Revoked)
        {
            // Revoke entire family
            await c.ExecuteAsync(new CommandDefinition(
                "UPDATE sys_refresh_token SET revoked_at=@N WHERE family_id=@F AND revoked_at IS NULL",
                new { N = _clock.UtcNow.DateTime, F = revoked.FamilyId }, cancellationToken: ct));
            await _el.LogAsync("token_reuse_detected", "warning", revoked.UserId, revoked.Username, null, "Refresh token reuse detected", ct);
            await tx.CommitAsync(ct);
            return null;
        }

        // Check expiry
        if (revoked.ExpiresAt < _clock.UtcNow.DateTime || revoked.Status != "active")
        {
            await tx.RollbackAsync(ct);
            return null;
        }

        // Revoke current token
        await c.ExecuteAsync(new CommandDefinition(
            "UPDATE sys_refresh_token SET revoked_at=@N WHERE id=@Id",
            new { N = _clock.UtcNow.DateTime, Id = revoked.Id }, cancellationToken: ct));

        // Issue new token pair
        var pair = _ts.GenerateTokenPair(new TokenPrincipal(revoked.UserId, revoked.Username, revoked.SecurityStamp, revoked.PermissionVersion, revoked.IsSuperAdmin));
        await c.ExecuteAsync(new CommandDefinition(
            "INSERT INTO sys_refresh_token (user_id,token_hash,family_id,expires_at,created_at) VALUES (@U,@H,@F,@E,@N)",
            new { U = revoked.UserId, H = HashToken(pair.RefreshToken), F = revoked.FamilyId, E = _clock.UtcNow.DateTime.AddDays(7), N = _clock.UtcNow.DateTime }, cancellationToken: ct));

        await tx.CommitAsync(ct);
        return new RefreshResponse(pair.AccessToken, pair.RefreshToken, pair.ExpiresIn);
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken ct)
    {
        var h = HashToken(refreshToken);
        await using var c = await _db.OpenAsync(ct);
        await c.ExecuteAsync(new CommandDefinition(
            "UPDATE sys_refresh_token SET revoked_at=@N WHERE token_hash=@H AND revoked_at IS NULL",
            new { N = _clock.UtcNow.DateTime, H = h }, cancellationToken: ct));
    }

    public async Task<CurrentUserResponse?> GetCurrentUserAsync(long uid, CancellationToken ct)
    { await using var c = await _db.OpenAsync(ct); var u = await c.QueryFirstOrDefaultAsync<CurU>(new CommandDefinition("SELECT id,username,display_name FROM sys_user WHERE id=@Id AND deleted_at IS NULL",new{Id=uid},cancellationToken:ct)); if(u is null)return null; var rt=await c.QueryAsync<string>(new CommandDefinition("SELECT r.code FROM sys_role r JOIN sys_user_role ur ON ur.role_id=r.id WHERE ur.user_id=@Id AND r.status='active' AND r.deleted_at IS NULL",new{Id=uid},cancellationToken:ct)); var pt=await c.QueryAsync<string>(new CommandDefinition("SELECT DISTINCT p.code FROM sys_permission p JOIN sys_role_permission rp ON rp.permission_id=p.id JOIN sys_user_role ur ON ur.role_id=rp.role_id WHERE ur.user_id=@Id AND p.status='active'",new{Id=uid},cancellationToken:ct)); return new CurrentUserResponse(u.Id,u.Username,u.DisplayName,rt.AsList().ToArray(),pt.AsList().ToArray(),Array.Empty<object>()); }

    private async Task StoreRefreshToken(DbConnection c, long uid, string t, string? ip, string? ua, CancellationToken ct)
    { await c.ExecuteAsync(new CommandDefinition("INSERT INTO sys_refresh_token (user_id,token_hash,family_id,created_ip,user_agent,expires_at,created_at) VALUES (@U,@H,@F,@Ip,@Ua,@E,@N)", new{U=uid,H=HashToken(t),F=Guid.NewGuid().ToString("N"),Ip=ip??"",Ua=ua??"",E=_clock.UtcNow.DateTime.AddDays(7),N=_clock.UtcNow.DateTime},cancellationToken:ct)); }
    private static string HashToken(string t) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(t)));
    private static string GenerateTicket() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

    private sealed record UserR(long Id,string Username,string DisplayName,string PasswordHash,string SecurityStamp,long PermissionVersion,string Status,bool TwoFactorEnabled,bool IsSuperAdmin);
    private sealed record RefreshR(long Id,long UserId,string FamilyId,string Username,string SecurityStamp,long PermissionVersion,string Status,bool IsSuperAdmin,bool Revoked,DateTime ExpiresAt);
    private sealed record CurU(long Id,string Username,string DisplayName);
    private sealed record TwoFactorTicketData(long UserId, string Username, string SecurityStamp, long PermissionVersion, bool IsSuperAdmin, DateTime ExpiresAt);
    private sealed record TwoFactorRow(string? Secret, long? LastUsedStep);
}
