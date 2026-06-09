 using global::WeCms.Shared;
 using WeCms.Shared.Contracts;
 
 namespace WeCms.Modules.System.Auth;
 
 public static class AuthEndpoints
 {
     public static RouteGroupBuilder MapAuthEndpoints(this RouteGroupBuilder group)
     {
         group.MapPost("/auth/login", LoginAsync).AllowAnonymous().RequireRateLimiting("login");
         group.MapPost("/auth/refresh", RefreshAsync).AllowAnonymous().RequireRateLimiting("login");
         group.MapPost("/auth/logout", LogoutAsync).RequireAuthorization();
         group.MapGet("/auth/me", GetCurrentUserAsync).RequireAuthorization();
         return group;
     }
 
     private static async Task<IResult> LoginAsync(LoginRequest req, HttpContext ctx, IAuthService svc, IDbConnectionFactory db, CancellationToken ct)
     { var r = await svc.LoginAsync(req.Username, req.Password, ct); var ip = ctx.Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? ctx.Connection.RemoteIpAddress?.ToString(); if (r is not null) { await using var c = await db.OpenAsync(ct); await c.ExecuteAsync(new CommandDefinition("INSERT INTO sys_login_log (user_id,username,login_type,status,ip,user_agent,created_at) SELECT id,username,'password','success',@Ip,@Ua,@Now FROM sys_user WHERE username=@U", new { Ip = ip, Ua = ctx.Request.Headers.UserAgent.ToString(), Now = DateTime.UtcNow, U = req.Username }, cancellationToken: ct)); return Results.Ok(ApiResult<LoginResponse>.Ok(r)); } return Results.Ok(ApiResult<LoginResponse>.Fail(ApiCodes.Unauthorized, "Invalid credentials")); }
 
     private static async Task<IResult> RefreshAsync(RefreshRequest req, IAuthService svc, CancellationToken ct)
     { var r = await svc.RefreshTokenAsync(req.RefreshToken, ct); return r is null ? Results.Ok(ApiResult<RefreshResponse>.Fail(ApiCodes.Unauthorized, "Invalid or expired refresh token")) : Results.Ok(ApiResult<RefreshResponse>.Ok(r)); }
 
     private static async Task<IResult> LogoutAsync(HttpContext ctx, IAuthService svc, IDbConnectionFactory db, CancellationToken ct)
     { var at = ctx.Request.Headers.Authorization.ToString().Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase); await svc.LogoutAsync(at, ct); var uid = ctx.User.FindFirst("sub")?.Value; if (uid is not null && long.TryParse(uid, out var id)) { await using var c = await db.OpenAsync(ct); var un = ctx.User.FindFirst("username")?.Value; var ip = ctx.Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? ctx.Connection.RemoteIpAddress?.ToString(); await c.ExecuteAsync(new CommandDefinition("INSERT INTO sys_security_event (event_type,severity,user_id,username,ip,detail,created_at) VALUES ('logout','info',@Id,@Un,@Ip,'User logged out',@Now)", new { Id = id, Un = un, Ip = ip, Now = DateTime.UtcNow }, cancellationToken: ct)); } return Results.Ok(ApiResult<string>.Ok("logged out")); }
 
     private static async Task<IResult> GetCurrentUserAsync(HttpContext ctx, IAuthService svc, CancellationToken ct)
     { var uid = ctx.User.FindFirst("sub")?.Value; if (uid is null || !long.TryParse(uid, out var id)) return Results.Ok(ApiResult<CurrentUserResponse>.Fail(ApiCodes.Unauthorized, "Not authenticated")); var u = await svc.GetCurrentUserAsync(id, ct); return u is null ? Results.Ok(ApiResult<CurrentUserResponse>.Fail(ApiCodes.NotFound, "Not found")) : Results.Ok(ApiResult<CurrentUserResponse>.Ok(u)); }
 }
