namespace WeCms.Api.Security;

public static class WeCmsCorsExtensions
{
    private static readonly string[] AllowedMethods =
    [
        HttpMethods.Get,
        HttpMethods.Post,
        HttpMethods.Put,
        HttpMethods.Patch,
        HttpMethods.Delete,
        HttpMethods.Options
    ];

    public static IServiceCollection AddWeCmsCors(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        var origins = ReadAllowedOrigins(configuration);
        services.AddCors(options =>
        {
            options.AddPolicy(WeCmsCorsPolicyNames.AdminApi, policy =>
            {
                policy
                    .WithOrigins(origins)
                    .WithMethods(AllowedMethods)
                    .AllowAnyHeader()
                    .AllowCredentials();
            });
        });

        return services;
    }

    private static string[] ReadAllowedOrigins(IConfiguration configuration)
    {
        return configuration.GetSection("Security:AllowedOrigins")
            .GetChildren()
            .Select(child => child.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim().TrimEnd('/'))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }
}
