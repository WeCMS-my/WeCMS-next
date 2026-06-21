using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using WeCms.Shared;

namespace WeCms.Modules.Identity.Services;

public interface ICookieAuthOriginValidator
{
    Task ValidateAsync(
        HttpContext context,
        string endpointName,
        AuthRequestContext requestContext,
        CancellationToken cancellationToken);
}

public static class CookieAuthOriginEndpoints
{
    public const string Refresh = "auth.refresh";
    public const string Logout = "auth.logout";
    public const string TwoFactorVerify = "auth.2fa.verify";
    public const string TwoFactorRecoveryCode = "auth.2fa.recovery-code";
}

public sealed class CookieAuthOriginValidator : ICookieAuthOriginValidator, IIdentityCookieAuthOriginValidator
{
    private const string RejectedEventType = "auth.cookie_origin_rejected";
    private const string RejectedMessage = "Cookie authenticated request origin is not allowed.";

    private readonly IAuthRepository _repository;
    private readonly IAuthClock _clock;
    private readonly IReadOnlySet<string> _allowedOrigins;
    private readonly bool _requireOrigin;
    private readonly bool _allowRefererFallback;

    public CookieAuthOriginValidator(
        IConfiguration configuration,
        IHostEnvironment environment,
        IAuthRepository repository,
        IAuthClock clock)
    {
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);
        _repository = repository;
        _clock = clock;

        _requireOrigin = ReadBool(configuration, "Security:RequireOriginForCookieAuth", defaultValue: true);
        _allowRefererFallback = ReadBool(configuration, "Security:AllowRefererFallbackForCookieAuth", defaultValue: true);

        if (!_requireOrigin && !IsDevelopment(environment))
        {
            throw new InvalidOperationException("Security:RequireOriginForCookieAuth=false is only allowed in Development.");
        }

        _allowedOrigins = ReadAllowedOrigins(configuration);
        if (_allowedOrigins.Count == 0 && !IsDevelopment(environment))
        {
            throw new InvalidOperationException("Security:AllowedOrigins must contain at least one origin outside Development.");
        }
    }

    public async Task ValidateAsync(
        HttpContext context,
        string endpointName,
        AuthRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requestContext);

        if (!_requireOrigin)
        {
            return;
        }

        if (TryReadNormalizedHeaderOrigin(context.Request.Headers.Origin.ToString(), out var origin)
            && _allowedOrigins.Contains(origin))
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(context.Request.Headers.Origin.ToString())
            && _allowRefererFallback
            && TryReadNormalizedRefererOrigin(context.Request.Headers.Referer.ToString(), out var refererOrigin)
            && _allowedOrigins.Contains(refererOrigin))
        {
            return;
        }

        await RecordRejectedAsync(endpointName, requestContext, cancellationToken);
        throw new DomainException(ApiCodes.Forbidden, RejectedMessage);
    }

    private async Task RecordRejectedAsync(
        string endpointName,
        AuthRequestContext requestContext,
        CancellationToken cancellationToken)
    {
        await _repository.RecordSecurityEventAsync(
            new SecurityEventRecord(
                RejectedEventType,
                null,
                null,
                requestContext.Ip,
                "warning",
                $"Cookie authenticated request origin rejected for {endpointName}.",
                _clock.UtcNow,
                requestContext.TraceId),
            cancellationToken);
    }

    private static IReadOnlySet<string> ReadAllowedOrigins(IConfiguration configuration)
    {
        var origins = configuration.GetSection("Security:AllowedOrigins")
            .GetChildren()
            .Select(child => child.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())
            .ToArray();

        if (origins.Any(origin => origin.Contains('*', StringComparison.Ordinal)))
        {
            throw new InvalidOperationException("Security:AllowedOrigins must not contain wildcard origins.");
        }

        return origins
            .Select(NormalizeConfiguredOrigin)
            .ToHashSet(StringComparer.Ordinal);
    }

    private static string NormalizeConfiguredOrigin(string origin)
    {
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri)
            || !IsHttpScheme(uri)
            || (!string.IsNullOrEmpty(uri.AbsolutePath) && uri.AbsolutePath != "/")
            || !string.IsNullOrEmpty(uri.Query)
            || !string.IsNullOrEmpty(uri.Fragment))
        {
            throw new InvalidOperationException($"Security:AllowedOrigins contains invalid origin '{origin}'.");
        }

        return NormalizeOrigin(uri);
    }

    private static bool TryReadNormalizedHeaderOrigin(string? value, out string origin)
    {
        origin = string.Empty;
        if (string.IsNullOrWhiteSpace(value)
            || value.Contains(',', StringComparison.Ordinal)
            || !Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri)
            || !IsHttpScheme(uri))
        {
            return false;
        }

        origin = NormalizeOrigin(uri);
        return true;
    }

    private static bool TryReadNormalizedRefererOrigin(string? value, out string origin)
    {
        origin = string.Empty;
        if (string.IsNullOrWhiteSpace(value)
            || !Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri)
            || !IsHttpScheme(uri))
        {
            return false;
        }

        origin = NormalizeOrigin(uri);
        return true;
    }

    private static string NormalizeOrigin(Uri uri)
    {
        var builder = new UriBuilder(uri.Scheme, uri.Host, uri.Port);
        return builder.Uri.GetLeftPart(UriPartial.Authority).TrimEnd('/');
    }

    private static bool IsHttpScheme(Uri uri)
    {
        return string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            || string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
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

    private static bool IsDevelopment(IHostEnvironment environment)
    {
        return string.Equals(environment.EnvironmentName, Environments.Development, StringComparison.OrdinalIgnoreCase);
    }
}
