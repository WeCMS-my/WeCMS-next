using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace WeCms.Modules.System.Auth;

public static class SystemAuthServiceCollectionExtensions
{
    public static IServiceCollection AddWeCmsSystemAuth(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = new AuthTokenOptions(
            Required(configuration, "Auth:AccessTokenSecret"),
            configuration["Auth:Issuer"] ?? "wecms",
            TimeSpan.FromMinutes(ReadInt(configuration, "Auth:AccessTokenMinutes", 15)),
            TimeSpan.FromDays(ReadInt(configuration, "Auth:RefreshTokenDays", 7)));

        services.AddSingleton(options);
        services.AddScoped<IAuthService, AuthService>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IAccessTokenService, AccessTokenService>();
        services.AddSingleton<IAuthTokenEntropy, AuthTokenEntropy>();
        services.AddSingleton<IRefreshTokenService>(sp => new RefreshTokenService(
            options.RefreshTokenLifetime,
            sp.GetRequiredService<IAuthTokenEntropy>()));
        services.AddSingleton<IAuthClock, SystemAuthClock>();
        services.AddAuthentication(AccessTokenAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, AccessTokenAuthenticationHandler>(
                AccessTokenAuthenticationHandler.SchemeName,
                configureOptions: null);
        services.AddAuthorization();

        return services;
    }

    private static string Required(IConfiguration configuration, string key)
    {
        var value = configuration[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{key} must be configured.");
        }

        return value;
    }

    private static int ReadInt(IConfiguration configuration, string key, int defaultValue)
    {
        var value = configuration[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        if (!int.TryParse(value, global::System.Globalization.NumberStyles.None, global::System.Globalization.CultureInfo.InvariantCulture, out var result))
        {
            throw new InvalidOperationException($"{key} must be an integer.");
        }

        return result;
    }
}
