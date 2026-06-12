using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using WeCms.Api.Extensions;
using WeCms.Modules.System.Auth;
using WeCms.Modules.System.Permissions;
using WeCms.Modules.System.System;
using WeCms.Shared.Security;

namespace WeCms.Tests.Architecture;

public sealed class PermissionMetadataScanTests
{
    private static WebApplication BuildTestApp()
    {
        var builder = WebApplication.CreateBuilder([]);

        builder.Configuration["Jwt:SigningKey"] = "test-key-that-is-at-least-256-bits-long-for-arch-testing!";
        builder.Configuration["Jwt:Issuer"] = "WeCMS";
        builder.Configuration["Jwt:Audience"] = "WeCMS";
        builder.Configuration["Jwt:AccessTokenExpirySeconds"] = "1800";

        builder.Services.AddSingleton<IPermissionChecker>(new AlwaysAllowPermissionChecker());
        builder.Services.AddSingleton<PermissionEndpointFilter>();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddScoped<AuthEndpointHandlers>();
        builder.Services.AddScoped<SystemEndpointHandlers>();

        builder.Services.AddAuthentication("Bearer")
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes("test-key-that-is-at-least-256-bits-long-for-arch-testing!")),
                    ValidateIssuer = true,
                    ValidIssuer = "WeCMS",
                    ValidateAudience = true,
                    ValidAudience = "WeCMS",
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                };
            });

        // Endpoint data source resolution in this test host depends on routing services being
        // available before endpoint descriptors are finalized.
        builder.Services.AddRouting();
        builder.Services.AddAuthorization();

        var app = builder.Build();

        SystemEndpoints.Map(app);
        app.MapAuthEndpoints();

        return app;
    }

    [Fact]
    public void SecurePing_ShouldHave_PermissionMetadata()
    {
        var app = BuildTestApp();
        var appEndpoints = GetDiscoveredEndpoints(app);
        var securePing = FindSecurePingEndpoint(appEndpoints);

        if (securePing is null)
        {
            var endpointInfo = string.Join(
                "; ",
                appEndpoints.Select(e =>
                {
                    var routeText = (e as RouteEndpoint)?.RoutePattern.RawText ?? "<non-route>";
                    var permissionText = e.Metadata.GetMetadata<PermissionMetadata>()?.Code ?? "<null>";
                    var authText = e.Metadata.GetMetadata<IAuthorizeData>() is null ? "<null>" : "present";
                    return $"route={routeText};permission={permissionText};auth={authText}";
                }));
            Assert.Fail($"Could not locate secure-ping endpoint by PermissionMetadata. Endpoints: {endpointInfo}");
        }

        var metadata = securePing.Metadata.GetMetadata<PermissionMetadata>();
        Assert.NotNull(metadata);
        Assert.Equal(SystemPermissions.SystemSecurePing, metadata!.Code);
    }

    [Fact]
    public void SecurePing_ShouldRequire_Authorization()
    {
        var app = BuildTestApp();
        var appEndpoints = GetDiscoveredEndpoints(app);
        var securePing = FindSecurePingEndpoint(appEndpoints);

        Assert.NotNull(securePing);

        var authData = securePing!.Metadata.GetMetadata<IAuthorizeData>();
        Assert.NotNull(authData);
    }

    [Fact]
    public void AllAuthenticatedEndpoints_ShouldHave_PermissionMetadata_OrBeExempt()
    {
        var app = BuildTestApp();
        var dataSources = app.Services.GetRequiredService<IEnumerable<EndpointDataSource>>();

        // Endpoints that are authenticated but don't need a specific permission code
        var exemptRoutes = new HashSet<string>
        {
            "/api/v1/auth/logout",
            "/api/v1/auth/me",
        };

        var allEndpoints = dataSources.SelectMany(ds => ds.Endpoints).ToList();

        foreach (var endpoint in allEndpoints)
        {
            var authReq = endpoint.Metadata.GetMetadata<IAuthorizeData>();
            if (authReq is null) continue;

            var route = endpoint is RouteEndpoint routeEndpoint
                ? routeEndpoint.RoutePattern.RawText ?? endpoint.DisplayName ?? string.Empty
                : endpoint.DisplayName ?? string.Empty;

            var hasPermission = endpoint.Metadata.GetMetadata<PermissionMetadata>() is not null;
            var isExempt = exemptRoutes.Contains(route);

            Assert.True(hasPermission || isExempt,
                $"Endpoint '{route}' requires authorization but has no PermissionMetadata. " +
                "Add .RequirePermission(...) or add it to the exempt list.");
        }
    }

    private static IReadOnlyList<Endpoint> GetDiscoveredEndpoints(WebApplication app)
    {
        var fromService = app.Services
            .GetService<IEnumerable<EndpointDataSource>>()
            ?.SelectMany(ds => ds.Endpoints)
            .ToList();

        if (fromService is { Count: > 0 })
        {
            return fromService;
        }

        var dataSourcesProperty = app.GetType().GetProperty(
            "DataSources",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);

        if (dataSourcesProperty is null)
        {
            return fromService ?? [];
        }

        if (dataSourcesProperty.GetValue(app) is not IEnumerable<EndpointDataSource> dataSources)
        {
            return fromService ?? [];
        }

        var fromProperty = dataSources
            .SelectMany(ds => ds.Endpoints)
            .ToList();

        return fromProperty;
    }

    private static Endpoint? FindSecurePingEndpoint(IEnumerable<Endpoint> endpoints)
    {
        return endpoints.FirstOrDefault(e =>
            string.Equals(GetRouteMetadataPath(e), "/api/v1/system/secure-ping", StringComparison.OrdinalIgnoreCase)
            || e.Metadata.GetMetadata<PermissionMetadata>()?.Code == SystemPermissions.SystemSecurePing
            || (e.Metadata.GetMetadata<PermissionMetadata>()?.Code?.Contains("secure", StringComparison.OrdinalIgnoreCase) == true));
    }

    private static string GetRouteMetadataPath(Endpoint endpoint)
    {
        return endpoint is RouteEndpoint routeEndpoint
            ? (routeEndpoint.RoutePattern.RawText ?? routeEndpoint.RoutePattern.ToString() ?? string.Empty)
            : endpoint.DisplayName ?? string.Empty;
    }

    private sealed class AlwaysAllowPermissionChecker : IPermissionChecker
    {
        public Task<PermissionCheckResult> CheckAsync(
            long userId,
            string permissionCode,
            CancellationToken cancellationToken)
            => Task.FromResult(new PermissionCheckResult(true, true));
    }
}
