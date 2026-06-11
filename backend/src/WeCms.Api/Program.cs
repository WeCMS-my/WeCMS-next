using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using WeCms.Api.Extensions;
using WeCms.Api.Json;
using WeCms.Api.Middleware;
using WeCms.Infrastructure.Security;
using WeCms.Modules.System.Auth;
using WeCms.Modules.System.Permissions;
using WeCms.Modules.System.System;
using WeCms.Persistence.Data;
using WeCms.Persistence.Migration;
using WeCms.Shared.Security;

var builder = WebApplication.CreateSlimBuilder(args);

// M0-BE-010: OpenAPI document generation (source-generated for Native AOT)
builder.Services.AddOpenApi();

// Register infrastructure services (password hasher, clock, token services)
builder.Services.AddWeCmsInfrastructure();

// Register persistence services (DB connection, unit of work, repositories, migration runner)
builder.Services.AddWeCmsPersistence();

// Register Auth services (scoped)
builder.Services.AddScoped<IAuthService, AuthService>();

// Register Permission endpoint filter
builder.Services.AddSingleton<PermissionEndpointFilter>();

// Register JWT Token Service (singleton — uses IConfiguration + IClock)
builder.Services.AddSingleton<ITokenService, JwtTokenService>();

// Register JSON serializer context for Native AOT (singleton — needed by endpoint filters)
builder.Services.AddSingleton<System.Text.Json.Serialization.JsonSerializerContext>(WeCmsJsonContext.Default);

// Configure JSON serializer context for Native AOT
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolverChain.Insert(0, WeCmsJsonContext.Default);
});

// Configure JWT Bearer authentication
var jwtSigningKey = builder.Configuration["Jwt:SigningKey"] ?? throw new InvalidOperationException("配置缺失：Jwt:SigningKey");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "WeCMS";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "WeCMS";
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
// default off by configuration; enable explicitly for local development when needed
var autoMigrate = builder.Configuration.GetValue("Database:AutoMigrate", false);
if (autoMigrate)
{
    using var scope = app.Services.CreateScope();
    var migrator = scope.ServiceProvider.GetRequiredService<DbMigrationRunner>();
    await migrator.RunAsync();
}
else
{
    Log.AutoMigrationDisabled(app.Logger);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.Run();

internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Database auto-migration disabled (Database:AutoMigrate=false)")]
    public static partial void AutoMigrationDisabled(ILogger logger);
}
