using System.Text;
using WeCms.Shared.Contracts;
using System.Security.Cryptography;
using WeCms.Modules.System.Auth;

namespace WeCms.Modules.System.Auth;

public sealed class AuthService : IAuthService
{
    private readonly ITokenService _tokenService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IDbConnectionFactory _connectionFactory;

    public AuthService(ITokenService ts, IPasswordHasher ph, IDbConnectionFactory cf)
    { _tokenService = ts; _passwordHasher = ph; _connectionFactory = cf; }

    public async Task<LoginResponse?> LoginAsync(string username, string password, CancellationToken ct)
    {
        await using var conn = await _connectionFactory.OpenAsync(ct);
        const string sql = "SELECT id, username, display_name AS DisplayName, password_hash AS PasswordHash, security_stamp AS SecurityStamp, permission_version AS PermissionVersion, status, two_factor_enabled AS TwoFactorEnabled FROM sys_user WHERE username = @Username AND deleted_at IS NULL";
        var u = await conn.QueryFirstOrDefaultAsync<UserRecord>(new CommandDefinition(sql, new { Username = username }, cancellationToken: ct));
        if (u is null || u.Status != "active" || !_passwordHasher.Verify(password, u.PasswordHash)) return null;
        var pair = _tokenService.GenerateTokenPair(new TokenPrincipal(u.Id, u.Username, u.SecurityStamp, u.PermissionVersion));
        await StoreRefreshToken(u.Id, pair.RefreshToken, ct);
        return new LoginResponse(pair.AccessToken, pair.RefreshToken, pair.ExpiresIn, u.TwoFactorEnabled);
    }

    public async Task<RefreshResponse?> RefreshTokenAsync(string refreshToken, CancellationToken ct)
    {
        var tokenHash = HashToken(refreshToken);
        await using var conn = await _connectionFactory.OpenAsync(ct);
        const string sql = "SELECT srt.id AS Id, srt.user_id AS UserId, srt.family_id AS FamilyId, u.username AS Username, u.security_stamp AS SecurityStamp, u.permission_version AS PermissionVersion, u.status AS Status FROM sys_refresh_token srt JOIN sys_user u ON u.id = srt.user_id WHERE srt.token_hash = @TokenHash AND srt.revoked_at IS NULL AND srt.expires_at > @Now";
        var rec = await conn.QueryFirstOrDefaultAsync<RefreshTokenRecord>(new CommandDefinition(sql, new { TokenHash = tokenHash, Now = DateTime.UtcNow }, cancellationToken: ct));
        if (rec is null || rec.Status != "active") return null;
        await conn.ExecuteAsync(new CommandDefinition("UPDATE sys_refresh_token SET revoked_at = @Now WHERE id = @Id", new { Now = DateTime.UtcNow, rec.Id }, cancellationToken: ct));
        var pair = _tokenService.GenerateTokenPair(new TokenPrincipal(rec.UserId, rec.Username, rec.SecurityStamp, rec.PermissionVersion));
        await StoreRefreshToken(rec.UserId, pair.RefreshToken, ct);
        return new RefreshResponse(pair.AccessToken, pair.RefreshToken, pair.ExpiresIn);
    }

    public Task LogoutAsync(string accessToken, CancellationToken ct) => Task.CompletedTask;

    public async Task<CurrentUserResponse?> GetCurrentUserAsync(long userId, CancellationToken ct)
    {
        await using var conn = await _connectionFactory.OpenAsync(ct);
        var u = await conn.QueryFirstOrDefaultAsync<CurrentUserRecord>(new CommandDefinition("SELECT id AS Id, username AS Username, display_name AS DisplayName FROM sys_user WHERE id = @UserId AND deleted_at IS NULL", new { UserId = userId }, cancellationToken: ct));
        if (u is null) return null;
        var roles = await conn.QueryAsync<string>(new CommandDefinition("SELECT r.code FROM sys_role r JOIN sys_user_role ur ON ur.role_id = r.id WHERE ur.user_id = @UserId AND r.status = 'active' AND r.deleted_at IS NULL", new { UserId = userId }, cancellationToken: ct));
        var perms = await conn.QueryAsync<string>(new CommandDefinition("SELECT DISTINCT p.code FROM sys_permission p JOIN sys_role_permission rp ON rp.permission_id = p.id JOIN sys_user_role ur ON ur.role_id = rp.role_id WHERE ur.user_id = @UserId AND p.status = 'active'", new { UserId = userId }, cancellationToken: ct));
        return new CurrentUserResponse(u.Id, u.Username, u.DisplayName, roles.AsList().ToArray(), perms.AsList().ToArray(), Array.Empty<object>());
    }

    private async Task StoreRefreshToken(long uid, string token, CancellationToken ct)
    {
        await using var conn = await _connectionFactory.OpenAsync(ct);
        await conn.ExecuteAsync(new CommandDefinition("INSERT INTO sys_refresh_token (user_id, token_hash, family_id, expires_at, created_at) VALUES (@Uid, @Hash, @Fid, @Exp, @Now)",
            new { Uid = uid, Hash = HashToken(token), Fid = Guid.NewGuid().ToString("N"), Exp = DateTime.UtcNow.AddDays(7), Now = DateTime.UtcNow }, cancellationToken: ct));
    }

    private static string HashToken(string t) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(t)));

    private sealed record UserRecord(long Id, string Username, string DisplayName, string PasswordHash, string SecurityStamp, long PermissionVersion, string Status, bool TwoFactorEnabled);
    private sealed record RefreshTokenRecord(long Id, long UserId, string FamilyId, string Username, string SecurityStamp, long PermissionVersion, string Status);
    private sealed record CurrentUserRecord(long Id, string Username, string DisplayName);
}