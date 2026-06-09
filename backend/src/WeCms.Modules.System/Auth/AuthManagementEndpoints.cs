 using global::WeCms.Shared;
 using WeCms.Shared.Contracts;
 
 namespace WeCms.Modules.System.Auth;
 
 public static class AuthManagementEndpoints
 {
     public static RouteGroupBuilder MapAuthManagementEndpoints(this RouteGroupBuilder group)
     {
         group.MapPost("/auth/password", ChangePasswordAsync).RequireAuthorization().RequireRateLimiting("password");
         group.MapGet("/auth/sessions", ListSessionsAsync).RequireAuthorization();
         group.MapDelete("/auth/sessions/{id:long}", RevokeSessionAsync).RequireAuthorization();
         return group;
     }
 
     private static async Task<IResult> ChangePasswordAsync(HttpContext ctx, ChangePasswordRequest req, IPasswordHasher hasher, IDbConnectionFactory db, CancellationToken ct)
     { var uid = GetUserId(ctx); if (!uid.HasValue) return Results.Ok(ApiResult<string>.Fail(ApiCodes.Unauthorized, "Not authenticated")); if (string.IsNullOrWhiteSpace(req.OldPassword) || string.IsNullOrWhiteSpace(req.NewPassword) || req.NewPassword.Length < 8) return Results.Ok(ApiResult<string>.Fail(ApiCodes.ValidationError, "Password must be at least 8 characters")); await using var c = await db.OpenAsync(ct); var cur = await c.QueryFirstOrDefaultAsync<string>(new CommandDefinition("SELECT password_hash FROM sys_user WHERE id=@Id", new { Id = uid }, cancellationToken: ct)); if (string.IsNullOrEmpty(cur) || !hasher.Verify(req.OldPassword, cur)) return Results.Ok(ApiResult<string>.Fail(ApiCodes.BusinessError, "Current password is incorrect")); await c.ExecuteAsync(new CommandDefinition("UPDATE sys_user SET password_hash=@H, password_hash_algorithm='pbkdf2-sha256', security_stamp=@S WHERE id=@Id", new { H = hasher.Hash(req.NewPassword), S = Guid.NewGuid().ToString("N"), Id = uid }, cancellationToken: ct)); return Results.Ok(ApiResult<string>.Ok("Password changed")); }
 
     private static async Task<IResult> ListSessionsAsync(HttpContext ctx, IDbConnectionFactory db, CancellationToken ct)
     { var uid = GetUserId(ctx); if (!uid.HasValue) return Results.Ok(ApiResult<string>.Fail(ApiCodes.Unauthorized, "Not authenticated")); await using var c = await db.OpenAsync(ct); var s = await c.QueryAsync<SessionItem>(new CommandDefinition("SELECT id, ip, user_agent, created_at, expires_at FROM sys_user_session WHERE user_id=@Id AND revoked_at IS NULL ORDER BY created_at DESC", new { Id = uid }, cancellationToken: ct)); return Results.Ok(ApiResult<IReadOnlyList<SessionItem>>.Ok(s.AsList())); }
 
     private static async Task<IResult> RevokeSessionAsync(long id, HttpContext ctx, IDbConnectionFactory db, CancellationToken ct)
     { var uid = GetUserId(ctx); if (!uid.HasValue) return Results.Ok(ApiResult<string>.Fail(ApiCodes.Unauthorized, "Not authenticated")); await using var c = await db.OpenAsync(ct); await c.ExecuteAsync(new CommandDefinition("UPDATE sys_user_session SET revoked_at=@Now WHERE id=@Id AND user_id=@Uid", new { Now = DateTime.UtcNow, Id = id, Uid = uid }, cancellationToken: ct)); return Results.Ok(ApiResult<string>.Ok("Session revoked")); }
 
     private static long? GetUserId(HttpContext ctx) { var s = ctx.User.FindFirst("sub")?.Value; return s is not null && long.TryParse(s, out var id) ? id : null; }
 
     public sealed record ChangePasswordRequest(string OldPassword, string NewPassword);
     public sealed record SessionItem(long Id, string? Ip, string? UserAgent, DateTime CreatedAt, DateTime ExpiresAt);
 }
