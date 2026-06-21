using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using WeCms.Modules.Platform.Permissions;
using WeCms.Shared;
using WeCms.Shared.Endpoints;

namespace WeCms.Modules.Platform.System;

public static class PlatformSystemEndpointExtensions
{
    private const string DatabaseUnavailableMessage = "Database is unavailable.";

    public static IEndpointRouteBuilder MapPlatformSystemEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/health/live", () =>
                Results.Ok(ApiResult<SystemLiveResponse>.Ok(new SystemLiveResponse("live"))))
            .WithMetadata(new EndpointModuleMetadata("platform"))
            .WithMetadata(new OpenApiResponseMetadata(typeof(SystemLiveResponse)))
            .AllowAnonymous();

        endpoints.MapGet("/health/ready", CheckReadyAsync)
            .WithMetadata(new EndpointModuleMetadata("platform"))
            .WithMetadata(new OpenApiResponseMetadata(typeof(SystemReadyResponse)))
            .AllowAnonymous();

        endpoints.MapGet("/health/dependencies", CheckDependenciesAsync)
            .WithMetadata(new EndpointModuleMetadata("platform"))
            .WithMetadata(new OpenApiResponseMetadata(typeof(SystemDependenciesResponse)))
            .RequireAuthorization()
            .RequireEndpointPermission(PlatformPermissions.SecurePing);

        endpoints.MapGet("/api/v1/system/version", () =>
                Results.Ok(ApiResult<SystemVersionResponse>.Ok(new SystemVersionResponse(Version()))))
            .WithMetadata(new EndpointModuleMetadata("platform"))
            .WithMetadata(new OpenApiResponseMetadata(typeof(SystemVersionResponse)))
            .AllowAnonymous();

        endpoints.MapGet("/api/v1/system/db-check", CheckDatabaseAsync)
            .WithMetadata(new EndpointModuleMetadata("platform"))
            .WithMetadata(new OpenApiResponseMetadata(typeof(SystemDbCheckResponse)))
            .AllowAnonymous();

        endpoints.MapGet("/api/v1/system/ping", () =>
                Results.Ok(ApiResult<SystemPingResponse>.Ok(new SystemPingResponse("ok"))))
            .WithMetadata(new EndpointModuleMetadata("platform"))
            .WithMetadata(new OpenApiResponseMetadata(typeof(SystemPingResponse)))
            .AllowAnonymous();

        endpoints.MapGet("/api/v1/system/secure-ping", () =>
                Results.Ok(ApiResult<SecurePingResponse>.Ok(new SecurePingResponse("ok"))))
            .WithMetadata(new EndpointModuleMetadata("platform"))
            .WithMetadata(new OpenApiResponseMetadata(typeof(SecurePingResponse)))
            .RequireAuthorization()
            .RequireEndpointPermission(PlatformPermissions.SecurePing);

        return endpoints;
    }

    private static async Task<IResult> CheckReadyAsync(
        HttpContext context,
        [FromServices] ISystemDatabaseProbe databaseProbe,
        [FromServices] ISystemMigrationProbe migrationProbe,
        [FromServices] IWebHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        var database = await databaseProbe.CheckAsync(cancellationToken);
        if (!database.Available)
        {
            return Results.Json(
                ApiResult<SystemReadyResponse>.Error(
                    ApiCodes.ServiceUnavailable,
                    DatabaseUnavailableMessage,
                    context.TraceIdentifier),
                statusCode: ApiCodes.ToHttpStatus(ApiCodes.ServiceUnavailable));
        }

        var migrations = await migrationProbe.CheckAsync(cancellationToken);
        if (!migrations.Available)
        {
            return Results.Json(
                ApiResult<SystemReadyResponse>.Error(
                    ApiCodes.ServiceUnavailable,
                    "Database migrations are unavailable.",
                    context.TraceIdentifier),
                statusCode: ApiCodes.ToHttpStatus(ApiCodes.ServiceUnavailable));
        }

        return Results.Ok(ApiResult<SystemReadyResponse>.Ok(new SystemReadyResponse("ready", Database: true, Migrations: true, CriticalConfiguration: IsCriticalConfigurationLoaded(environment))));
    }

    private static async Task<IResult> CheckDependenciesAsync(
        [FromServices] ISystemDatabaseProbe databaseProbe,
        [FromServices] ISystemMigrationProbe migrationProbe,
        [FromServices] IWebHostEnvironment environment,
        CancellationToken cancellationToken)
    {
        var database = await databaseProbe.CheckAsync(cancellationToken);
        var migrations = await migrationProbe.CheckAsync(cancellationToken);
        var criticalConfigurationLoaded = IsCriticalConfigurationLoaded(environment);
        var status = database.Available && migrations.Available && criticalConfigurationLoaded
            ? "ready"
            : "degraded";

        return Results.Ok(ApiResult<SystemDependenciesResponse>.Ok(new SystemDependenciesResponse(
            status,
            ToDependencyStatus(database),
            ToDependencyStatus(migrations),
            new SystemDependencyStatus(
                criticalConfigurationLoaded ? "ok" : "unavailable",
                criticalConfigurationLoaded,
                null,
                criticalConfigurationLoaded ? null : "critical_configuration_unavailable"))));
    }

    private static async Task<IResult> CheckDatabaseAsync(
        HttpContext context,
        [FromServices] ISystemDatabaseProbe databaseProbe,
        CancellationToken cancellationToken)
    {
        var result = await databaseProbe.CheckAsync(cancellationToken);
        if (!result.Available)
        {
            return Results.Json(
                ApiResult<SystemDbCheckResponse>.Error(
                    ApiCodes.ServiceUnavailable,
                    DatabaseUnavailableMessage,
                    context.TraceIdentifier),
                statusCode: ApiCodes.ToHttpStatus(ApiCodes.ServiceUnavailable));
        }

        return Results.Ok(ApiResult<SystemDbCheckResponse>.Ok(new SystemDbCheckResponse("ok", Database: true)));
    }

    private static SystemDependencyStatus ToDependencyStatus(SystemDatabaseProbeResult result)
    {
        return new SystemDependencyStatus(
            result.Available ? "ok" : "unavailable",
            result.Available,
            result.LatencyMs,
            result.FailureCode);
    }

    private static SystemDependencyStatus ToDependencyStatus(SystemMigrationProbeResult result)
    {
        return new SystemDependencyStatus(
            result.Available ? "ok" : "unavailable",
            result.Available,
            result.LatencyMs,
            result.FailureCode);
    }

    private static bool IsCriticalConfigurationLoaded(IWebHostEnvironment environment)
    {
        return !string.IsNullOrWhiteSpace(environment.EnvironmentName);
    }

    private static string Version()
    {
        return typeof(PlatformSystemEndpointExtensions).Assembly.GetName().Version?.ToString()
            ?? "0.0.0.0";
    }
}
