using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace WeCms.Api.Configuration;

public static class ProductionConfigurationValidator
{
    private const string DevelopmentSeedPassword = "Admin@123";
    private const int MinimumSecretLength = 32;
    private const int MinimumSeedPasswordLength = 12;

    private static readonly string[] PlaceholderValues =
    [
        "__SET_BY_ENV__",
        "__SET_BY_SECRET_MANAGER__",
        "__SET_BY_USER_SECRETS__"
    ];

    public static void Validate(IConfiguration configuration, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        if (!environment.IsProduction())
        {
            return;
        }

        RequireConfigured(configuration.GetConnectionString("Default"), "ConnectionStrings:Default");
        RequireSecret(configuration["Auth:AccessTokenSecret"], "Auth:AccessTokenSecret");
        RequireSecret(configuration["Security:TwoFactor:SecretProtectionKey"], "Security:TwoFactor:SecretProtectionKey");
        RequireAllowedOrigins(configuration);
        RequireSeedAdminPassword(configuration["Database:SeedAdminPassword"]);
    }

    private static void RequireConfigured(string? value, string key)
    {
        if (string.IsNullOrWhiteSpace(value) || ContainsPlaceholder(value))
        {
            throw new InvalidOperationException($"{key} must be configured for Production.");
        }
    }

    private static void RequireSecret(string? value, string key)
    {
        RequireConfigured(value, key);

        if (value!.Length < MinimumSecretLength)
        {
            throw new InvalidOperationException($"{key} must be at least {MinimumSecretLength} characters for Production.");
        }
    }

    private static void RequireAllowedOrigins(IConfiguration configuration)
    {
        var origins = configuration.GetSection("Security:AllowedOrigins")
            .Get<string[]>()?
            .Where(origin => !string.IsNullOrWhiteSpace(origin))
            .Select(origin => origin.Trim())
            .ToArray() ?? [];

        if (origins.Length == 0)
        {
            throw new InvalidOperationException("Security:AllowedOrigins must contain at least one origin for Production.");
        }

        foreach (var origin in origins)
        {
            if (origin == "*")
            {
                throw new InvalidOperationException("Security:AllowedOrigins must not contain wildcard origins in Production.");
            }

            if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
            {
                throw new InvalidOperationException($"Security:AllowedOrigins contains invalid origin '{origin}'.");
            }

            if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Security:AllowedOrigins must use HTTPS origins in Production.");
            }

            if (IsLocalhost(uri.Host))
            {
                throw new InvalidOperationException("Security:AllowedOrigins must not contain localhost origins in Production.");
            }
        }
    }

    private static void RequireSeedAdminPassword(string? value)
    {
        RequireConfigured(value, "Database:SeedAdminPassword");

        if (string.Equals(value, DevelopmentSeedPassword, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Database:SeedAdminPassword must not use the Development default in Production.");
        }

        if (!IsStrongSeedPassword(value!))
        {
            throw new InvalidOperationException(
                $"Database:SeedAdminPassword must be at least {MinimumSeedPasswordLength} characters and include uppercase, lowercase, digit, and symbol characters for Production.");
        }
    }

    private static bool IsStrongSeedPassword(string value)
    {
        return value.Length >= MinimumSeedPasswordLength
            && value.Any(char.IsUpper)
            && value.Any(char.IsLower)
            && value.Any(char.IsDigit)
            && value.Any(ch => !char.IsLetterOrDigit(ch));
    }

    private static bool ContainsPlaceholder(string value)
    {
        return PlaceholderValues.Any(placeholder => value.Contains(placeholder, StringComparison.Ordinal));
    }

    private static bool IsLocalhost(string host)
    {
        return string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
            || string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(host, "::1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(host, "[::1]", StringComparison.OrdinalIgnoreCase);
    }
}
