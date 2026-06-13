using System.Net.Http;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using WeCms.Modules.System.System;
using WeCms.Shared;

namespace WeCms.Api.Extensions;

internal sealed class SystemOpenApiDocumentTransformer : IOpenApiDocumentTransformer
{
    public async Task TransformAsync(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        CancellationToken cancellationToken)
    {
        await AddGetAsync<ApiResult<HealthLiveResponse>>(document, context, "/health/live", "System_HealthLive", "OK", "200", cancellationToken);
        await AddGetAsync<ApiResult<HealthReadyResponse>>(document, context, "/health/ready", "System_HealthReady", "OK", "200", cancellationToken);
        await AddGetAsync<ApiResult<HealthReadyResponse>>(document, context, "/health/ready", "System_HealthReady", "Service Unavailable", "503", cancellationToken);
        await AddGetAsync<ApiResult<SystemPingResponse>>(document, context, "/api/v1/system/ping", "System_Ping", "OK", "200", cancellationToken);
        await AddGetAsync<ApiResult<SystemVersionResponse>>(document, context, "/api/v1/system/version", "System_Version", "OK", "200", cancellationToken);
        await AddGetAsync<ApiResult<DbCheckResponse>>(document, context, "/api/v1/system/db-check", "System_DbCheck", "OK", "200", cancellationToken);
        await AddGetAsync<ApiResult<DbCheckResponse>>(document, context, "/api/v1/system/db-check", "System_DbCheck", "Service Unavailable", "503", cancellationToken);
        await AddGetAsync<ApiResult<SecurePingResponse>>(document, context, "/api/v1/system/secure-ping", "System_SecurePing", "OK", "200", cancellationToken);
    }

    private static async Task AddGetAsync<TResponse>(
        OpenApiDocument document,
        OpenApiDocumentTransformerContext context,
        string path,
        string operationId,
        string description,
        string statusCode,
        CancellationToken cancellationToken)
    {
        var schema = await context.GetOrCreateSchemaAsync(typeof(TResponse), null, cancellationToken);
        document.Paths ??= new OpenApiPaths();
        OpenApiPathItem pathItem;
        if (document.Paths.TryGetValue(path, out var existingPathItem))
        {
            pathItem = existingPathItem as OpenApiPathItem
                ?? throw new InvalidOperationException($"Unsupported OpenAPI path item type for {path}.");
        }
        else
        {
            pathItem = new OpenApiPathItem();
            document.Paths[path] = pathItem;
        }

        var operation = pathItem.Operations is not null &&
            pathItem.Operations.TryGetValue(HttpMethod.Get, out var existingOperation)
                ? existingOperation
                : null;
        if (operation is null)
        {
            operation = new OpenApiOperation
            {
                OperationId = operationId
            };
            pathItem.AddOperation(HttpMethod.Get, operation);
        }

        operation.Responses ??= new OpenApiResponses();
        operation.Responses[statusCode] = new OpenApiResponse
        {
            Description = description,
            Content = new Dictionary<string, OpenApiMediaType>
            {
                ["application/json"] = new() { Schema = schema }
            }
        };
    }
}
