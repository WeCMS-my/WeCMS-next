 using System.Text;
using System.Security.Cryptography;
using WeCms.Shared;
using WeCms.Shared.Contracts;

namespace WeCms.Modules.System.Auth;
 
 public static class AuthManagementEndpoints
 {
     public static RouteGroupBuilder MapAuthManagementEndpoints(this RouteGroupBuilder group)
     {
         group.MapPost("/auth/change-password", ChangePasswordAsync).RequireAuthorization().RequireRateLimiting("password");
         group.MapPost("/auth/forgot-password", ForgotPasswordAsync).AllowAnonymous().RequireRateLimiting("password");
         group.MapPost("/auth/reset-password", ResetPasswordAsync).AllowAnonymous().RequireRateLimiting("password");
         group.MapGet("/auth/sessions", ListSessionsAsync).RequireAuthorization();
         return group;
     }
 
     private static async Task<IResult> ChangePasswordAsync(HttpContext ctx, ChangePasswordRequest req, IPasswordHasher hasher, IDbConnectionFactory db, ISecurityEventLogger el, CancellationToken ct)
     { var uid = GetUserId(ctx); if (!uid.HasValue) return Results.Ok(ApiResult<string>.Fail(ApiCodes.Unauthorized, "Not authenticated")); await using var c = await db.OpenAsync(ct); var hash = await c.QueryFirstOrDefaultAsync<string>(new CommandDefinition("SELECT password_hash FROM sys_user WHERE id=@Id", new { Id = uid.Value }, cancellationToken: ct)); if (hash is null || !hasher.Verify(req.OldPassword, hash)) return Results.Ok(ApiResult<string>.Fail(ApiCodes.BusinessError, "Current password is incorrect")); var newHash = hasher.Hash(req.NewPassword); await c.ExecuteAsync(new CommandDefinition("UPDATE sys_user SET password_hash=@H, security_stamp=@S, updated_at=@N WHERE id=@Id", new { H = newHash, S = Guid.NewGuid().ToString("N"), N = DateTime.UtcNow, Id = uid.Value }, cancellationToken: ct)); await el.LogAsync("password_changed", "info", uid.Value, ctx.User.FindFirst("username")?.Value, null, "Password changed", ct); return Results.Ok(ApiResult<string>.Ok("Password changed")); }
 
     private static async Task<IResult> ForgotPasswordAsync(ForgotPasswordRequest req, CancellationToken ct)
     { return Results.Ok(ApiResult<string>.Ok("If the email exists, a reset link has been sent")); }
 
     private static async Task<IResult> ResetPasswordAsync(ResetPasswordRequest req, IPasswordHasher hasher, IDbConnectionFactory db, ISecurityEventLogger el, CancellationToken ct)
     { var tokenHash = HashToken(req.Token); await using var c = await db.OpenAsync(ct); await using var tx = await c.BeginTransactionAsync(ct); var tokenRow = await c.QueryFirstOrDefaultAsync<ResetTokenRow>(new CommandDefinition("SELECT id, user_id FROM sys_password_reset_token WHERE token_hash=@H AND expires_at>@N AND used_at IS NULL", new { H = tokenHash, N = DateTime.UtcNow }, cancellationToken: ct)); if (tokenRow is null) { await tx.RollbackAsync(ct); return Results.Ok(ApiResult<string>.Fail(ApiCodes.BusinessError, "Invalid or expired reset token")); } var username = await c.QueryFirstOrDefaultAsync<string>(new CommandDefinition("SELECT username FROM sys_user WHERE id=@Id AND deleted_at IS NULL", new { Id = tokenRow.UserId }, cancellationToken: ct)); if (username is null) { await tx.RollbackAsync(ct); return Results.Ok(ApiResult<string>.Fail(ApiCodes.BusinessError, "User not found")); } var newHash = hasher.Hash(req.NewPassword); await c.ExecuteAsync(new CommandDefinition("UPDATE sys_user SET password_hash=@H, security_stamp=@S, updated_at=@N WHERE id=@Id", new { H = newHash, S = Guid.NewGuid().ToString("N"), N = DateTime.UtcNow, Id = tokenRow.UserId }, cancellationToken: ct)); await c.ExecuteAsync(new CommandDefinition("UPDATE sys_password_reset_token SET used_at=@N WHERE id=@Id", new { N = DateTime.UtcNow, Id = tokenRow.Id }, cancellationToken: ct)); await c.ExecuteAsync(new CommandDefinition("UPDATE sys_refresh_token SET revoked_at=@N WHERE user_id=@Id AND revoked_at IS NULL", new { N = DateTime.UtcNow, Id = tokenRow.UserId }, cancellationToken: ct)); await el.LogAsync("password_reset", "info", tokenRow.UserId, username, null, "Password reset via token", ct); await tx.CommitAsync(ct); return Results.Ok(ApiResult<string>.Ok("Password reset successfully")); }
 
     private static async Task<IResult> ListSessionsAsync(HttpContext ctx, IDbConnectionFactory db, CancellationToken ct)
     { var uid = GetUserId(ctx); if (!uid.HasValue) return Results.Ok(ApiResult<IReadOnlyList<SessionItem>>.Fail(ApiCodes.Unauthorized, "Not authenticated")); await using var c = await db.OpenAsync(ct); var items = await c.QueryAsync<SessionItem>(new CommandDefinition("SELECT id, ip, user_agent, created_at, expires_at FROM sys_refresh_token WHERE user_id=@Id AND revoked_at IS NULL AND expires_at>@N", new { Id = uid.Value, N = DateTime.UtcNow }, cancellationToken: ct)); return Results.Ok(ApiResult<IReadOnlyList<SessionItem>>.Ok(items.AsList())); }
 
     private static long? GetUserId(HttpContext ctx) { var s = ctx.User.FindFirst("sub")?.Value; return s is not null && long.TryParse(s, out var id) ? id : null; }
 
     private sealed record ResetTokenRow(long Id, long UserId);
     private static string HashToken(string t) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(t)));
 }

 public sealed record ChangePasswordRequest(string OldPassword, string NewPassword);
 public sealed record SessionItem(long Id, string? Ip, string? UserAgent, DateTime CreatedAt, DateTime ExpiresAt);
 public sealed record ForgotPasswordRequest(string Email);
 public sealed record ResetPasswordRequest(string Token, string NewPassword);
