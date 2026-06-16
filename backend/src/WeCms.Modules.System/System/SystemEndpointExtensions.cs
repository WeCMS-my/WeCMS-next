using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Mvc;
using WeCms.Shared;

namespace WeCms.Modules.System.System;

public static class SystemEndpointExtensions
{
    private const string DatabaseUnavailableMessage = "Database is unavailable.";

    public static IEndpointRouteBuilder MapSystemEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/health/live", () =>
                Results.Ok(ApiResult<SystemLiveResponse>.Ok(new SystemLiveResponse("live"))))
            .WithMetadata(new OpenApiResponseMetadata(typeof(SystemLiveResponse)))
            .AllowAnonymous();

        endpoints.MapGet("/health/ready", CheckReadyAsync)
            .WithMetadata(new OpenApiResponseMetadata(typeof(SystemReadyResponse)))
            .AllowAnonymous();

        endpoints.MapGet("/api/v1/system/ping", () =>
                Results.Ok(ApiResult<SystemPingResponse>.Ok(new SystemPingResponse("ok"))))
            .WithMetadata(new OpenApiResponseMetadata(typeof(SystemPingResponse)))
            .AllowAnonymous();

        endpoints.MapGet("/api/v1/system/version", () =>
                Results.Ok(ApiResult<SystemVersionResponse>.Ok(new SystemVersionResponse(Version()))))
            .WithMetadata(new OpenApiResponseMetadata(typeof(SystemVersionResponse)))
            .AllowAnonymous();

        endpoints.MapGet("/api/v1/system/db-check", CheckDatabaseAsync)
            .WithMetadata(new OpenApiResponseMetadata(typeof(SystemDbCheckResponse)))
            .AllowAnonymous();

        return endpoints;
    }

    private static async Task<IResult> CheckReadyAsync(
        HttpContext context,
        [FromServices] ISystemDatabaseProbe databaseProbe,
        CancellationToken cancellationToken)
    {
        var result = await databaseProbe.CheckAsync(cancellationToken);
        if (!result.Available)
        {
            return Results.Json(
                ApiResult<SystemReadyResponse>.Error(
                    ApiCodes.ServiceUnavailable,
                    DatabaseUnavailableMessage,
                    context.TraceIdentifier),
                statusCode: ApiCodes.ToHttpStatus(ApiCodes.ServiceUnavailable));
        }

        return Results.Ok(ApiResult<SystemReadyResponse>.Ok(new SystemReadyResponse("ready", Database: true)));
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

    private static string Version()
    {
        return typeof(SystemEndpointExtensions).Assembly.GetName().Version?.ToString()
            ?? "0.0.0.0";
    }
}
