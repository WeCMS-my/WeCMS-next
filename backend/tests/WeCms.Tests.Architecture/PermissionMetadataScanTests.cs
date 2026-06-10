using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using WeCms.Modules.System.Auth;
using WeCms.Modules.System.Permissions;
using WeCms.Modules.System.System;
using WeCms.Shared.Security;

namespace WeCms.Tests.Architecture;

public sealed class PermissionMetadataScanTests
{
    private static WebApplication BuildTestApp()
    {
        var builder = WebApplication.CreateSlimBuilder([]);

        builder.Configuration["Jwt:SigningKey"] = "test-key-that-is-at-least-256-bits-long-for-arch-testing!";
        builder.Configuration["Jwt:Issuer"] = "WeCMS";
        builder.Configuration["Jwt:Audience"] = "WeCMS";
        builder.Configuration["Jwt:AccessTokenExpirySeconds"] = "1800";

        builder.Services.AddSingleton<IPermissionChecker>(new AlwaysAllowPermissionChecker());
        builder.Services.AddSingleton<PermissionEndpointFilter>();

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
        var dataSources = app.Services.GetRequiredService<IEnumerable<EndpointDataSource>>();
        var securePing = dataSources
            .SelectMany(ds => ds.Endpoints)
            .FirstOrDefault(e => e.DisplayName?.Contains("secure-ping") == true);

        Assert.NotNull(securePing);
        var metadata = securePing!.Metadata.GetMetadata<PermissionMetadata>();
        Assert.NotNull(metadata);
        Assert.Equal(SystemPermissions.SystemSecurePing, metadata!.Code);
    }

    [Fact]
    public void SecurePing_ShouldRequire_Authorization()
    {
        var app = BuildTestApp();
        var dataSources = app.Services.GetRequiredService<IEnumerable<EndpointDataSource>>();
        var securePing = dataSources
            .SelectMany(ds => ds.Endpoints)
            .FirstOrDefault(e => e.DisplayName?.Contains("secure-ping") == true);

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
        var exemptEndpoints = new HashSet<string>
        {
            "Auth_Logout",
            "Auth_Me",
        };

        var allEndpoints = dataSources.SelectMany(ds => ds.Endpoints).ToList();

        foreach (var endpoint in allEndpoints)
        {
            var route = endpoint.DisplayName ?? "";
            var requiresAuth = endpoint.Metadata.GetMetadata<IAuthorizeData>() is not null
                            || endpoint.Metadata.GetMetadata<IAuthorizeData>() is not null;
            
            // Skip if the endpoint doesn't require authorization
            var authReq = endpoint.Metadata.GetMetadata<IAuthorizeData>();
            if (authReq is null) continue;

            var hasPermission = endpoint.Metadata.GetMetadata<PermissionMetadata>() is not null;
            var isExempt = exemptEndpoints.Any(e => route.Contains(e));

            Assert.True(hasPermission || isExempt,
                $"Endpoint '{route}' requires authorization but has no PermissionMetadata. " +
                "Add .RequirePermission(...) or add it to the exempt list.");
        }
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
