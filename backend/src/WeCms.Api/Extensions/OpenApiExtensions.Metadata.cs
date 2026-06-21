using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using WeCms.Api.Endpoints;
using WeCms.Shared.Endpoints;

namespace WeCms.Api.Extensions;

public static partial class OpenApiExtensions
{
    private static IReadOnlyDictionary<OpenApiOperationKey, OpenApiRuntimeEndpointMetadata> DiscoverEndpointMetadata()
    {
        var serviceProvider = new OpenApiEndpointServiceProvider();
        var endpoints = new OpenApiEndpointRouteBuilder(serviceProvider);
        endpoints.MapWeCmsApiEndpoints();

        return endpoints.DataSources
            .SelectMany(static dataSource => dataSource.Endpoints)
            .OfType<RouteEndpoint>()
            .SelectMany(EndpointOperations)
            .GroupBy(static item => item.Key)
            .ToDictionary(
                static group => group.Key,
                static group => group.First().Metadata);
    }

    private static IEnumerable<(OpenApiOperationKey Key, OpenApiRuntimeEndpointMetadata Metadata)> EndpointOperations(RouteEndpoint endpoint)
    {
        var route = NormalizeRoute(endpoint.RoutePattern.RawText ?? string.Empty);
        if (string.IsNullOrWhiteSpace(route))
        {
            yield break;
        }

        var httpMethods = endpoint.Metadata
            .GetMetadata<IHttpMethodMetadata>()?
            .HttpMethods
            .Where(static method => !string.IsNullOrWhiteSpace(method))
            .Select(static method => method.ToLowerInvariant())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (httpMethods is null || httpMethods.Length == 0)
        {
            yield break;
        }

        var metadata = new OpenApiRuntimeEndpointMetadata(
            Module: endpoint.Metadata.GetMetadata<EndpointModuleMetadata>()?.Module,
            Permission: endpoint.Metadata.GetMetadata<EndpointPermissionMetadata>()?.PermissionCode,
            Audit: AuditDescriptor(endpoint.Metadata.GetMetadata<EndpointAuditMetadata>()),
            RateLimitPolicy: endpoint.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName
                ?? endpoint.Metadata.GetMetadata<EndpointRateLimitMetadata>()?.PolicyName);

        foreach (var method in httpMethods)
        {
            yield return (new OpenApiOperationKey(route, method), metadata);
        }
    }

    private static string NormalizeRoute(string route)
    {
        var normalized = route.StartsWith("/", StringComparison.Ordinal)
            ? route
            : $"/{route}";
        return normalized.Length > 1 ? normalized.TrimEnd('/') : normalized;
    }

    private static OpenApiAuditDescriptor? AuditDescriptor(EndpointAuditMetadata? metadata)
    {
        return metadata is null
            ? null
            : new OpenApiAuditDescriptor(metadata.Module, metadata.Resource, metadata.Action);
    }

    private readonly record struct OpenApiOperationKey(string Path, string Method);

    private sealed record OpenApiRuntimeEndpointMetadata(
        string? Module,
        string? Permission,
        OpenApiAuditDescriptor? Audit,
        string? RateLimitPolicy);

    private sealed record OpenApiAuditDescriptor(string Module, string Resource, string Action);

    private sealed class OpenApiEndpointRouteBuilder(IServiceProvider serviceProvider) : IEndpointRouteBuilder
    {
        public ICollection<EndpointDataSource> DataSources { get; } = [];

        public IServiceProvider ServiceProvider { get; } = serviceProvider;

        public IApplicationBuilder CreateApplicationBuilder()
        {
            return new ApplicationBuilder(ServiceProvider);
        }
    }

    private sealed class OpenApiEndpointServiceProvider : IServiceProvider, IServiceProviderIsService
    {
        public object? GetService(Type serviceType)
        {
            ArgumentNullException.ThrowIfNull(serviceType);

            return serviceType == typeof(IServiceProviderIsService) ? this : null;
        }

        public bool IsService(Type serviceType)
        {
            ArgumentNullException.ThrowIfNull(serviceType);

            return serviceType.IsInterface || serviceType.IsAbstract;
        }
    }
}
