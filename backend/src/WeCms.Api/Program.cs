using System.Globalization;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using WeCms.Api.Extensions;
using WeCms.Api.Json;
using WeCms.Api.Middleware;
using WeCms.Infrastructure.Migration;
using WeCms.Infrastructure.Security;
using WeCms.Modules.System.Auth;
using WeCms.Modules.System.Permissions;
using WeCms.Modules.System.System;
using WeCms.Shared.Security;

var builder = WebApplication.CreateSlimBuilder(args);

// M0-BE-010: OpenAPI document generation (source-generated for Native AOT)
builder.Services.AddOpenApi();

// Register infrastructure services (DB, password hasher, clock, migration runner, token services)
builder.Services.AddWeCmsInfrastructure();
builder.Services.AddWeCmsAuth();

// Register Auth services (scoped)
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Register Permission services
builder.Services.AddSingleton<IPermissionChecker, PermissionChecker>();
builder.Services.AddSingleton<PermissionEndpointFilter>();

// Register JWT Token Service (singleton — uses configuration)
var jwtSigningKey = builder.Configuration["Jwt:SigningKey"] ?? throw new InvalidOperationException("配置缺失：Jwt:SigningKey");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "WeCMS";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "WeCMS";
var jwtExpirySeconds = int.Parse(builder.Configuration["Jwt:AccessTokenExpirySeconds"] ?? "1800", CultureInfo.InvariantCulture);
builder.Services.AddSingleton<ITokenService>(new JwtTokenService(jwtSigningKey, jwtIssuer, jwtAudience, jwtExpirySeconds));

// Register JSON serializer context for Native AOT (singleton — needed by endpoint filters)
builder.Services.AddSingleton<System.Text.Json.Serialization.JsonSerializerContext>(WeCmsJsonContext.Default);

// Configure JSON serializer context for Native AOT
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, WeCmsJsonContext.Default);
});

// Configure JWT Bearer authentication
builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// M0-BE-004: Middleware pipeline — RequestId first (trace propagation), then Exception (error handling)
app.UseMiddleware<RequestIdMiddleware>();
app.UseMiddleware<ExceptionMiddleware>();

// Authentication & Authorization middleware
app.UseAuthentication();
app.UseAuthorization();

// M0-BE-007: Register System endpoints (health, ping, version, db-check, secure-ping)
SystemEndpoints.Map(app);

// M0-BE-008: Auth endpoints
app.MapAuthEndpoints();

// M0-BE-010: Handle --export-openapi (before DB migrations — no DB needed for schema export)
if (OpenApiExtensions.IsExportMode(args))
{
    await app.ExportOpenApiAsync(OpenApiExtensions.GetExportPath(args));
    return;
}

// M0-BE-006: Run database migrations and seed on startup
using (var scope = app.Services.CreateScope())
{
    var migrator = scope.ServiceProvider.GetRequiredService<DbMigrationRunner>();
    await migrator.RunAsync();
}

app.MapOpenApi();

app.Run();
