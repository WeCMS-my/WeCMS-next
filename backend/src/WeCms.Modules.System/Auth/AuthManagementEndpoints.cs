 using global::WeCms.Shared;
 using WeCms.Shared.Contracts;
 using System.Security.Cryptography;
 using System.Text;
 
 namespace WeCms.Modules.System.Auth;
 
 public static class AuthManagementEndpoints
 {
     public static RouteGroupBuilder MapAuthManagementEndpoints(this RouteGroupBuilder group)
     {
         group.MapPost("/auth/password", ChangePasswordAsync).RequireAuthorization().RequireRateLimiting("password");
         group.MapGet("/auth/sessions", ListSessionsAsync).RequireAuthorization();
         group.MapDelete("/auth/sessions/{id:long}", RevokeSessionAsync).RequireAuthorization();
         group.MapPost("/auth/password/forgot", ForgotPasswordAsync).AllowAnonymous().RequireRateLimiting("password");
         group.MapPost("/auth/password/reset", ResetPasswordAsync).AllowAnonymous().RequireRateLimiting("password");
         return group;
     }
 
     private static async Task<IResult> ChangePasswordAsync(HttpContext ctx, ChangePasswordRequest req, IPasswordHasher hasher, IDbConnectionFactory db, CancellationToken ct)
     { var uid = GetUserId(ctx); if (!uid.HasValue) return Results.Ok(ApiResult<string>.Fail(ApiCodes.Unauthorized, "Not authenticated")); if (string.IsNullOrWhiteSpace(req.OldPassword) || string.IsNullOrWhiteSpace(req.NewPassword) || req.NewPassword.Length < 8) return Results.Ok(ApiResult<string>.Fail(ApiCodes.ValidationError, "Password must be at least 8 characters")); await using var c = await db.OpenAsync(ct); var cur = await c.QueryFirstOrDefaultAsync<string>(new CommandDefinition("SELECT password_hash FROM sys_user WHERE id=@Id", new { Id = uid }, cancellationToken: ct)); if (string.IsNullOrEmpty(cur) || !hasher.Verify(req.OldPassword, cur)) return Results.Ok(ApiResult<string>.Fail(ApiCodes.BusinessError, "Current password is incorrect")); await c.ExecuteAsync(new CommandDefinition("UPDATE sys_user SET password_hash=@H, password_hash_algorithm='pbkdf2-sha256', security_stamp=@S WHERE id=@Id", new { H = hasher.Hash(req.NewPassword), S = Guid.NewGuid().ToString("N"), Id = uid }, cancellationToken: ct)); return Results.Ok(ApiResult<string>.Ok("Password changed")); }
 
     private static async Task<IResult> ListSessionsAsync(HttpContext ctx, IDbConnectionFactory db, CancellationToken ct)
     { var uid = GetUserId(ctx); if (!uid.HasValue) return Results.Ok(ApiResult<string>.Fail(ApiCodes.Unauthorized, "Not authenticated")); await using var c = await db.OpenAsync(ct); var s = await c.QueryAsync<SessionItem>(new CommandDefinition("SELECT id, ip, user_agent, created_at, expires_at FROM sys_user_session WHERE user_id=@Id AND revoked_at IS NULL ORDER BY created_at DESC", new { Id = uid }, cancellationToken: ct)); return Results.Ok(ApiResult<IReadOnlyList<SessionItem>>.Ok(s.AsList())); }
 
     private static async Task<IResult> RevokeSessionAsync(long id, HttpContext ctx, IDbConnectionFactory db, CancellationToken ct)
     { var uid = GetUserId(ctx); if (!uid.HasValue) return Results.Ok(ApiResult<string>.Fail(ApiCodes.Unauthorized, "Not authenticated")); await using var c = await db.OpenAsync(ct); await c.ExecuteAsync(new CommandDefinition("UPDATE sys_user_session SET revoked_at=@Now WHERE id=@Id AND user_id=@Uid", new { Now = DateTime.UtcNow, Id = id, Uid = uid }, cancellationToken: ct)); return Results.Ok(ApiResult<string>.Ok("Session revoked")); }
 
     private static async Task<IResult> ForgotPasswordAsync(ForgotPasswordRequest req, IDbConnectionFactory db, ISecurityEventLogger? el, CancellationToken ct)
     { if (string.IsNullOrWhiteSpace(req.Email)) return Results.Ok(ApiResult<string>.Ok("If the email exists, a reset link has been sent")); await using var c = await db.OpenAsync(ct); var u = await c.QueryFirstOrDefaultAsync<(long Id, string Username)?>(new CommandDefinition("SELECT id, username FROM sys_user WHERE email=@E AND deleted_at IS NULL AND status='active'", new { E = req.Email }, cancellationToken: ct)); if (u.HasValue) { var token = Guid.NewGuid().ToString("N"); var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token))); await c.ExecuteAsync(new CommandDefinition("INSERT INTO sys_password_reset_token (user_id,token_hash,expires_at,created_at) VALUES (@U,@H,@E,@N)", new { U = u.Value.Id, H = hash, E = DateTime.UtcNow.AddHours(1), N = DateTime.UtcNow }, cancellationToken: ct)); } return Results.Ok(ApiResult<string>.Ok("If the email exists, a reset link has been sent")); }
 
     private static async Task<IResult> ResetPasswordAsync(ResetPasswordRequest req, IPasswordHasher hasher, IDbConnectionFactory db, CancellationToken ct)
     { if (string.IsNullOrWhiteSpace(req.Token) || string.IsNullOrWhiteSpace(req.NewPassword) || req.NewPassword.Length < 8) return Results.Ok(ApiResult<string>.Fail(ApiCodes.ValidationError, "Invalid request")); var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(req.Token))); await using var c = await db.OpenAsync(ct); var row = await c.QueryFirstOrDefaultAsync<(long UserId, DateTime Expires)?>(new CommandDefinition("SELECT user_id, expires_at FROM sys_password_reset_token WHERE token_hash=@H AND used_at IS NULL AND expires_at>@N", new { H = hash, N = DateTime.UtcNow }, cancellationToken: ct)); if (!row.HasValue) return Results.Ok(ApiResult<string>.Fail(ApiCodes.BusinessError, "Invalid or expired token")); await c.ExecuteAsync(new CommandDefinition("UPDATE sys_password_reset_token SET used_at=@N WHERE token_hash=@H", new { H = hash, N = DateTime.UtcNow }, cancellationToken: ct)); await c.ExecuteAsync(new CommandDefinition("UPDATE sys_user SET password_hash=@H, password_hash_algorithm='pbkdf2-sha256', security_stamp=@S WHERE id=@Id", new { H = hasher.Hash(req.NewPassword), S = Guid.NewGuid().ToString("N"), Id = row.Value.UserId }, cancellationToken: ct)); return Results.Ok(ApiResult<string>.Ok("Password reset successful")); }
 
     private static long? GetUserId(HttpContext ctx) { var s = ctx.User.FindFirst("sub")?.Value; return s is not null && long.TryParse(s, out var id) ? id : null; }
 
     public sealed record ChangePasswordRequest(string OldPassword, string NewPassword);
     public sealed record SessionItem(long Id, string? Ip, string? UserAgent, DateTime CreatedAt, DateTime ExpiresAt);
     public sealed record ForgotPasswordRequest(string Email);
     public sealed record ResetPasswordRequest(string Token, string NewPassword);
 }
