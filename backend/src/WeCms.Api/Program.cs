using WeCms.Api.Middleware;
using WeCms.Api.Extensions;
using WeCms.Modules.System;
using WeCms.Modules.System.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, WeCms.Api.Json.WeCmsJsonContext.Default);
});

builder.Services.AddWeCmsInfrastructure(builder.Configuration);

var jwtSecret = builder.Configuration["Auth:JwtSecret"] ?? "wecms-dev-secret-change-in-production-32chars";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ValidateIssuer = true, ValidIssuer = "wecms",
            ValidateAudience = true, ValidAudience = "wecms-admin",
            ValidateLifetime = true, ClockSkew = TimeSpan.Zero
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddOpenApi();

var app = builder.Build();
app.UseMiddleware<ExceptionMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health/live", () => Results.Ok(new { status = "healthy", timestamp = DateTimeOffset.UtcNow })).AllowAnonymous();
app.MapGet("/health/ready", () => Results.Ok(new { status = "ready", timestamp = DateTimeOffset.UtcNow })).AllowAnonymous();
app.MapGroup("/api/v1/system").MapSystemEndpoints();
app.MapGroup("/api/v1").MapAuthEndpoints();
app.MapOpenApi();
app.Run();