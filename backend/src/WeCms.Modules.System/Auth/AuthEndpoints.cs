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
 
     private static async Task<IResult> LoginAsync(LoginRequest req, IAuthService svc, CancellationToken ct)
     { var r = await svc.LoginAsync(req.Username, req.Password, ct); return r is null ? Results.Ok(ApiResult<LoginResponse>.Fail(ApiCodes.Unauthorized, "Invalid credentials")) : Results.Ok(ApiResult<LoginResponse>.Ok(r)); }
 
     private static async Task<IResult> RefreshAsync(RefreshRequest req, IAuthService svc, CancellationToken ct)
     { var r = await svc.RefreshTokenAsync(req.RefreshToken, ct); return r is null ? Results.Ok(ApiResult<RefreshResponse>.Fail(ApiCodes.Unauthorized, "Invalid or expired refresh token")) : Results.Ok(ApiResult<RefreshResponse>.Ok(r)); }
 
     private static async Task<IResult> LogoutAsync(HttpContext ctx, IAuthService svc, CancellationToken ct)
     { var at = ctx.Request.Headers.Authorization.ToString().Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase); await svc.LogoutAsync(at, ct); return Results.Ok(ApiResult<string>.Ok("logged out")); }
 
     private static async Task<IResult> GetCurrentUserAsync(HttpContext ctx, IAuthService svc, CancellationToken ct)
     { var uid = ctx.User.FindFirst("sub")?.Value; if (uid is null || !long.TryParse(uid, out var id)) return Results.Ok(ApiResult<CurrentUserResponse>.Fail(ApiCodes.Unauthorized, "Not authenticated")); var u = await svc.GetCurrentUserAsync(id, ct); return u is null ? Results.Ok(ApiResult<CurrentUserResponse>.Fail(ApiCodes.NotFound, "Not found")) : Results.Ok(ApiResult<CurrentUserResponse>.Ok(u)); }
 }
