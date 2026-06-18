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
        var loginFailureOptions = new LoginFailurePolicyOptions(
            ReadBool(configuration, "Security:LoginFailure:Enabled", true),
            TimeSpan.FromMinutes(ReadInt(configuration, "Security:LoginFailure:WindowMinutes", 10)),
            ReadInt(configuration, "Security:LoginFailure:UsernameThreshold", 5),
            ReadInt(configuration, "Security:LoginFailure:IpThreshold", 20),
            ReadInt(configuration, "Security:LoginFailure:BanThreshold", 10),
            TimeSpan.FromMinutes(ReadInt(configuration, "Security:LoginFailure:BanMinutes", 15)));
        var twoFactorChallengeOptions = new TwoFactorChallengeOptions(
            TimeSpan.FromMinutes(ReadInt(configuration, "Security:TwoFactor:ChallengeMinutes", 5)),
            ReadInt(configuration, "Security:TwoFactor:ChallengeMaxFailedAttempts", 5));

        services.AddSingleton(options);
        services.AddSingleton(loginFailureOptions);
        services.AddSingleton(twoFactorChallengeOptions);
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAuthAuditWriter, AuthAuditWriter>();
        services.AddScoped<IAuthSecurityEventWriter, AuthSecurityEventWriter>();
        services.AddScoped<IRefreshTokenRotationService, RefreshTokenRotationService>();
        services.AddScoped<ILogoutTokenRevoker, LogoutTokenRevoker>();
        services.AddScoped<IAuthSessionIssuer, AuthSessionIssuer>();
        services.AddScoped<IAuthTwoFactorChallengeService, AuthTwoFactorChallengeService>();
        services.AddScoped<IAccountTwoFactorService, AccountTwoFactorService>();
        services.AddScoped<IAccountProfileService, AccountProfileService>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddSingleton<IAccessTokenService, AccessTokenService>();
        services.AddSingleton<IAuthTokenEntropy, AuthTokenEntropy>();
        services.AddSingleton<IAuthChallengeEntropy, AuthChallengeEntropy>();
        services.AddScoped<ICookieAuthOriginValidator, CookieAuthOriginValidator>();
        services.AddScoped<ILoginFailureLimiter, LoginFailureLimiter>();
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

    private static bool ReadBool(IConfiguration configuration, string key, bool defaultValue)
    {
        var value = configuration[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        if (!bool.TryParse(value, out var result))
        {
            throw new InvalidOperationException($"{key} must be a boolean.");
        }

        return result;
    }
}
