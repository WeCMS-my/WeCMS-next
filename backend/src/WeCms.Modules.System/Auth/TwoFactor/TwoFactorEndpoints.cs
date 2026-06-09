 using WeCms.Shared;
 using WeCms.Shared.Contracts;
 
 namespace WeCms.Modules.System.Auth.TwoFactor;
 
 public static class TwoFactorEndpoints
 {
     public static RouteGroupBuilder MapTwoFactorEndpoints(this RouteGroupBuilder group)
     {
         group.MapGet("/auth/2fa/setup", SetupAsync).RequireAuthorization();
         group.MapPost("/auth/2fa/enable", EnableAsync).RequireAuthorization();
         group.MapPost("/auth/2fa/disable", DisableAsync).RequireAuthorization();
         group.MapPost("/auth/2fa/verify", VerifyAsync).AllowAnonymous();
         return group;
     }
 
     private static async Task<IResult> SetupAsync(HttpContext ctx, ITwoFactorService twoFactor,
         IDbConnectionFactory db, CancellationToken ct)
     {
         var uid = GetUserId(ctx);
         if (!uid.HasValue) return Results.Ok(ApiResult<TwoFactorSetupResponse>.Fail(ApiCodes.Unauthorized, "Not authenticated"));
         await using var conn = await db.OpenAsync(ct);
 
         var secret = twoFactor.GenerateSecret();
         var uri = twoFactor.GenerateQrCodeUri(
             ctx.User.FindFirst("username")?.Value ?? "", "WeCMS", secret);
         var (plain, hashed) = twoFactor.GenerateBackupCodes();
         var codesJson = global::System.Text.Json.JsonSerializer.Serialize(hashed);
 
         await conn.ExecuteAsync(new CommandDefinition(
             "UPDATE sys_user SET two_factor_temp_secret=@S, two_factor_temp_codes=@C WHERE id=@Id",
             new { S = secret, C = codesJson, Id = uid.Value }, cancellationToken: ct));
 
         return Results.Ok(ApiResult<TwoFactorSetupResponse>.Ok(new(secret, uri, plain)));
     }
 
     private static async Task<IResult> EnableAsync(HttpContext ctx, TwoFactorEnableRequest req,
         ITwoFactorService twoFactor, IDbConnectionFactory db, CancellationToken ct)
     {
         var uid = GetUserId(ctx);
         if (!uid.HasValue) return Results.Ok(ApiResult<string>.Fail(ApiCodes.Unauthorized, "Not authenticated"));
         await using var conn = await db.OpenAsync(ct);
 
         var row = await conn.QueryFirstOrDefaultAsync<(string Secret, string Codes)>(new CommandDefinition(
             "SELECT two_factor_temp_secret AS Secret, two_factor_temp_codes AS Codes FROM sys_user WHERE id=@Id",
             new { Id = uid.Value }, cancellationToken: ct));
 
         if (string.IsNullOrEmpty(row.Secret))
             return Results.Ok(ApiResult<string>.Fail(ApiCodes.BusinessError, "2FA setup not initiated"));
 
         if (!twoFactor.Verify(row.Secret, req.Code))
             return Results.Ok(ApiResult<string>.Fail(ApiCodes.BusinessError, "Invalid verification code"));
 
         await conn.ExecuteAsync(new CommandDefinition(
             "UPDATE sys_user SET two_factor_enabled=1,two_factor_secret=@S,two_factor_backup_codes=@C,two_factor_confirmed_at=@Now,two_factor_temp_secret=NULL,two_factor_temp_codes=NULL WHERE id=@Id",
             new { S = row.Secret, C = row.Codes, Now = DateTime.UtcNow, Id = uid.Value }, cancellationToken: ct));
 
         return Results.Ok(ApiResult<string>.Ok("2FA enabled"));
     }
 
     private static async Task<IResult> DisableAsync(HttpContext ctx, TwoFactorDisableRequest req,
         ITwoFactorService twoFactor, IDbConnectionFactory db, CancellationToken ct)
     {
         var uid = GetUserId(ctx);
         if (!uid.HasValue) return Results.Ok(ApiResult<string>.Fail(ApiCodes.Unauthorized, "Not authenticated"));
         await using var conn = await db.OpenAsync(ct);
 
         var secret = await conn.QueryFirstOrDefaultAsync<string>(new CommandDefinition(
             "SELECT two_factor_secret FROM sys_user WHERE id=@Id", new { Id = uid.Value }, cancellationToken: ct));
         if (string.IsNullOrEmpty(secret))
             return Results.Ok(ApiResult<string>.Fail(ApiCodes.BusinessError, "2FA not enabled"));
 
         if (!twoFactor.Verify(secret, req.Code))
             return Results.Ok(ApiResult<string>.Fail(ApiCodes.BusinessError, "Invalid verification code"));
 
         await conn.ExecuteAsync(new CommandDefinition(
             "UPDATE sys_user SET two_factor_enabled=0,two_factor_secret=NULL,two_factor_backup_codes=NULL WHERE id=@Id",
             new { Id = uid.Value }, cancellationToken: ct));
 
         return Results.Ok(ApiResult<string>.Ok("2FA disabled"));
     }
 
     private static async Task<IResult> VerifyAsync(HttpContext ctx, TwoFactorVerifyRequest req,
         ITwoFactorService twoFactor, IDbConnectionFactory db, CancellationToken ct)
     {
         // Called during login with a temp 2FA ticket (not implemented in M0 mini login)
         return Results.Ok(ApiResult<string>.Ok("ok"));
     }
 
     private static long? GetUserId(HttpContext ctx)
     {
         var s = ctx.User.FindFirst("sub")?.Value;
         return s is not null && long.TryParse(s, out var id) ? id : null;
     }
 }
