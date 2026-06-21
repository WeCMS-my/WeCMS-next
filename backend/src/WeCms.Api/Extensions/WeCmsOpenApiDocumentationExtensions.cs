using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace WeCms.Api.Extensions;

public static class WeCmsOpenApiDocumentationExtensions
{
    private const string ConfigurationSection = "OpenApiDocumentation";

    public static IServiceCollection AddWeCmsOpenApiDocumentation(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "WeCMS API",
                Version = "v1"
            });
            options.AddSecurityDefinition("bearerAuth", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });
            options.AddScalarFilters();
        });

        return services;
    }

    public static WebApplication MapWeCmsOpenApiDocumentation(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        if (!IsOpenApiDocumentationEnabled(app.Environment, app.Configuration))
        {
            return app;
        }

        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.RoutePrefix = "swagger";
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "WeCMS API v1");
        });
        app.MapScalarApiReference("/scalar", options => options.WithOpenApiRoutePattern("/swagger/{documentName}/swagger.json"));

        return app;
    }

    public static bool IsOpenApiDocumentationEnabled(IHostEnvironment environment, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(environment);
        ArgumentNullException.ThrowIfNull(configuration);

        return environment.IsDevelopment()
            || configuration.GetValue($"{ConfigurationSection}:Enabled", false);
    }
}
