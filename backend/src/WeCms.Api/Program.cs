using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using WeCms.Api.Extensions;
using WeCms.Api.Json;
using WeCms.Api.Middleware;
using WeCms.Api.Security;
using WeCms.Infrastructure.Security;
using WeCms.Modules.System.Auth;
using WeCms.Modules.System.Permissions;
using WeCms.Modules.System.System;
using WeCms.Persistence.Data;
using WeCms.Persistence.Migration;
using WeCms.Shared.Security;

var builder = WebApplication.CreateSlimBuilder(args);
var isOpenApiExportMode = OpenApiExtensions.IsExportMode(args);

// M0-BE-010: OpenAPI document generation (source-generated for Native AOT)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddWeCmsOpenApi();

// Register infrastructure services (password hasher, clock, token services)
builder.Services.AddWeCmsInfrastructure();

// Register persistence services (DB connection, unit of work, repositories, migration runner)
builder.Services.AddWeCmsPersistence();

// Register Auth services (scoped)
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAuthRiskService, AuthRiskService>();

// Register endpoint handler classes (scoped — constructor-injected dependencies)
builder.Services.AddScoped<AuthEndpointHandlers>();
builder.Services.AddSingleton<SystemEndpointHandlers>();

// Register Permission endpoint filter (scoped — requires per-request IPermissionChecker)
builder.Services.AddScoped<PermissionEndpointFilter>();
builder.Services.AddScoped<AccessTokenValidationEvents>();

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
var jwtSigningKey = builder.Configuration["Jwt:SigningKey"];
if (string.IsNullOrWhiteSpace(jwtSigningKey))
{
    if (!isOpenApiExportMode)
    {
        throw new InvalidOperationException("配置缺失：Jwt:SigningKey");
    }

    jwtSigningKey = "WeCmsOpenApiExportOnlySigningKey-NotForRuntimeAuthentication";
}

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
        options.EventsType = typeof(AccessTokenValidationEvents);
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
var systemEndpointHandlers = app.Services.GetRequiredService<SystemEndpointHandlers>();
SystemEndpoints.Map(app, systemEndpointHandlers);

// M0-BE-008: Auth endpoints
app.MapAuthEndpoints();

// M0-BE-010: Handle --export-openapi (before DB migrations — no DB needed for schema export)
if (isOpenApiExportMode)
{
    await app.ExportOpenApiAsync(OpenApiExtensions.GetExportPath(args));
    return;
}

if (args.Contains("--migrate-database", StringComparer.Ordinal))
{
    using var scope = app.Services.CreateScope();
    var migrator = scope.ServiceProvider.GetRequiredService<DbMigrationRunner>();
    await migrator.RunAsync();
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

public partial class Program { }

internal static partial class Log
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Database auto-migration disabled (Database:AutoMigrate=false)")]
    public static partial void AutoMigrationDisabled(ILogger logger);
}
