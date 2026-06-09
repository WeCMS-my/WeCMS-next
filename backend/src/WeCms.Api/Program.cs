 using System.Threading.RateLimiting;
 using WeCms.Api.Middleware;
 using WeCms.Api.Extensions;
 using WeCms.Modules.System;
 using WeCms.Modules.System.Auth;
 using WeCms.Modules.System.Auth.TwoFactor;
 using WeCms.Modules.System.Users;
 using WeCms.Modules.System.Roles;
 using WeCms.Modules.System.Menus;
 using WeCms.Modules.System.Settings;
 using WeCms.Modules.System.Dicts;
 using WeCms.Modules.System.Files;
 using WeCms.Modules.System.Logs;
using WeCms.Modules.System.Permissions;
 using Microsoft.AspNetCore.Authentication.JwtBearer;
 using Microsoft.IdentityModel.Tokens;
 using System.Text;
 
 var builder = WebApplication.CreateSlimBuilder(args);
 builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.TypeInfoResolverChain.Insert(0, WeCms.Api.Json.WeCmsJsonContext.Default));
 builder.Services.AddWeCmsInfrastructure(builder.Configuration);
 
 var jwtSecret = builder.Configuration["Auth:JwtSecret"] ?? throw new InvalidOperationException("Auth:JwtSecret is not configured");
 builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o => o.TokenValidationParameters = new TokenValidationParameters { ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)), ValidateIssuer = true, ValidIssuer = "wecms", ValidateAudience = true, ValidAudience = "wecms-admin", ValidateLifetime = true, ClockSkew = TimeSpan.Zero });
 builder.Services.AddAuthorization();
 builder.Services.AddRateLimiter(o => o.AddPolicy("login", c => RateLimitPartition.GetFixedWindowLimiter("login", _ => new FixedWindowRateLimiterOptions{PermitLimit=5,Window=TimeSpan.FromMinutes(1),QueueLimit=0})).AddPolicy("password", c => RateLimitPartition.GetFixedWindowLimiter("password", _ => new FixedWindowRateLimiterOptions{PermitLimit=3,Window=TimeSpan.FromMinutes(1),QueueLimit=0})));
 builder.Services.AddOpenApi();
 
 var app = builder.Build();
 app.UseMiddleware<ExceptionMiddleware>();
 app.UseAuthentication();
 app.UseAuthorization();
 
 app.MapGet("/health/live", () => Results.Ok(new { status = "healthy" })).AllowAnonymous();
 app.MapGroup("/api/v1/system").MapSystemEndpoints();
 app.MapGroup("/api/v1").MapAuthEndpoints();
 app.MapGroup("/api/v1").MapTwoFactorEndpoints();
 app.MapGroup("/api/v1").MapAuthManagementEndpoints();
 app.MapGroup("/api/v1").MapUserEndpoints();
 app.MapGroup("/api/v1").MapRoleEndpoints();
 app.MapGroup("/api/v1").MapMenuEndpoints();
 app.MapGroup("/api/v1").MapSettingEndpoints();
 app.MapGroup("/api/v1").MapDictEndpoints();
 app.MapGroup("/api/v1").MapFileEndpoints();
 app.MapGroup("/api/v1").MapLogEndpoints();
app.MapGroup("/api/v1").MapPermissionEndpoints();
 app.MapOpenApi();
 app.Run();
