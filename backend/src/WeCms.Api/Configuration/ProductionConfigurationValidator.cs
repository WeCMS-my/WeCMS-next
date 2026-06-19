using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System.Net;

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
        RequireForwardedHeaders(configuration);
        RequireSecureHeaders(configuration);
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

            if ((!string.IsNullOrEmpty(uri.AbsolutePath) && uri.AbsolutePath != "/")
                || !string.IsNullOrEmpty(uri.Query)
                || !string.IsNullOrEmpty(uri.Fragment))
            {
                throw new InvalidOperationException("Security:AllowedOrigins must contain origins only, without path, query, or fragment.");
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

    private static void RequireForwardedHeaders(IConfiguration configuration)
    {
        if (!ReadBool(configuration, "Security:ForwardedHeaders:Enabled", defaultValue: false))
        {
            return;
        }

        var proxies = ReadStringArray(configuration, "Security:ForwardedHeaders:KnownProxies");
        var networks = ReadStringArray(configuration, "Security:ForwardedHeaders:KnownNetworks");

        if (proxies.Length == 0 && networks.Length == 0)
        {
            throw new InvalidOperationException("Security:ForwardedHeaders requires KnownProxies or KnownNetworks when enabled in Production.");
        }

        foreach (var proxy in proxies)
        {
            if (!IPAddress.TryParse(proxy, out _))
            {
                throw new InvalidOperationException($"Security:ForwardedHeaders:KnownProxies contains invalid IP address '{proxy}'.");
            }
        }

        foreach (var network in networks)
        {
            if (!IsValidCidr(network))
            {
                throw new InvalidOperationException($"Security:ForwardedHeaders:KnownNetworks contains invalid CIDR network '{network}'.");
            }
        }
    }

    private static void RequireSecureHeaders(IConfiguration configuration)
    {
        var cspEnabled = ReadBool(configuration, "Security:SecureHeaders:CspEnabled", defaultValue: false);
        var cspReportOnlyEnabled = ReadBool(configuration, "Security:SecureHeaders:CspReportOnlyEnabled", defaultValue: true);

        if (!cspEnabled && !cspReportOnlyEnabled)
        {
            throw new InvalidOperationException("At least one CSP mode must be enabled in Production.");
        }

        if (cspEnabled)
        {
            RequireCsp(configuration["Security:SecureHeaders:Csp"], "Security:SecureHeaders:Csp");
        }

        if (cspReportOnlyEnabled)
        {
            RequireCsp(configuration["Security:SecureHeaders:CspReportOnly"], "Security:SecureHeaders:CspReportOnly");
        }
    }

    private static void RequireCsp(string? value, string key)
    {
        RequireConfigured(value, key);

        if (!value!.Contains("object-src 'none'", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"{key} must include object-src 'none' in Production.");
        }

        if (!value.Contains("frame-ancestors", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"{key} must include frame-ancestors in Production.");
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

    private static string[] ReadStringArray(IConfiguration configuration, string key)
    {
        return configuration.GetSection(key)
            .Get<string[]>()?
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .ToArray() ?? [];
    }

    private static bool ReadBool(IConfiguration configuration, string key, bool defaultValue)
    {
        var value = configuration[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        if (bool.TryParse(value, out var parsed))
        {
            return parsed;
        }

        throw new InvalidOperationException($"{key} must be true or false.");
    }

    private static bool IsValidCidr(string value)
    {
        var parts = value.Split('/', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2
            || !IPAddress.TryParse(parts[0], out var address)
            || !int.TryParse(parts[1], out var prefixLength))
        {
            return false;
        }

        var maxPrefixLength = address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ? 32 : 128;
        return prefixLength >= 0 && prefixLength <= maxPrefixLength;
    }

    private static bool IsLocalhost(string host)
    {
        return string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
            || string.Equals(host, "127.0.0.1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(host, "::1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(host, "[::1]", StringComparison.OrdinalIgnoreCase);
    }
}
