using System.Text.Json;
using Microsoft.Extensions.Configuration;
using WeCms.Api.Json;
using WeCms.Modules.System.Auth;
using WeCms.Shared;
using WeCms.Shared.Security;

namespace WeCms.Api.Middleware;

public sealed class IpAccessControlMiddleware
{
    private const string DeniedEventType = "security.ip_access_denied";
    private const string DeniedMessage = "IP access is not allowed.";

    private readonly RequestDelegate _next;

    public IpAccessControlMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IConfiguration configuration,
        IIpRuleMatcher ipRuleMatcher,
        IAuthRepository authRepository,
        IAuthClock clock)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(ipRuleMatcher);
        ArgumentNullException.ThrowIfNull(authRepository);
        ArgumentNullException.ThrowIfNull(clock);

        if (!ReadBool(configuration, "Security:IpAccessControl:Enabled", defaultValue: false)
            || ShouldSkipHealthEndpoint(context, configuration)
            || ShouldSkipAuthEndpoint(context, configuration))
        {
            await _next(context);
            return;
        }

        var rules = ReadRules(configuration);
        if (string.IsNullOrWhiteSpace(rules))
        {
            throw new InvalidOperationException("Security:IpAccessControl:AllowedRules must contain at least one rule when enabled.");
        }

        var remoteIp = context.Connection.RemoteIpAddress;
        if (remoteIp is not null && ipRuleMatcher.IsMatch(rules, remoteIp))
        {
            await _next(context);
            return;
        }

        await authRepository.RecordSecurityEventAsync(
            new SecurityEventRecord(
                DeniedEventType,
                null,
                null,
                remoteIp?.ToString() ?? string.Empty,
                "warning",
                DeniedMessage,
                clock.UtcNow),
            context.RequestAborted);

        await WriteForbiddenAsync(context);
    }

    private static bool ShouldSkipHealthEndpoint(HttpContext context, IConfiguration configuration)
    {
        return ReadBool(configuration, "Security:IpAccessControl:SkipHealthEndpoints", defaultValue: true)
            && context.Request.Path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase);
    }

    private static bool ShouldSkipAuthEndpoint(HttpContext context, IConfiguration configuration)
    {
        return !ReadBool(configuration, "Security:IpAccessControl:ApplyToAuthEndpoints", defaultValue: true)
            && context.Request.Path.StartsWithSegments("/api/v1/auth", StringComparison.OrdinalIgnoreCase);
    }

    private static string ReadRules(IConfiguration configuration)
    {
        var values = configuration.GetSection("Security:IpAccessControl:AllowedRules")
            .GetChildren()
            .Select(child => child.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value!.Trim())
            .ToArray();

        if (values.Length == 0)
        {
            var scalarValue = configuration["Security:IpAccessControl:AllowedRules"];
            return scalarValue?.Trim() ?? string.Empty;
        }

        return string.Join('\n', values);
    }

    private static async Task WriteForbiddenAsync(HttpContext context)
    {
        if (context.Response.HasStarted)
        {
            throw new InvalidOperationException("Cannot write API error response after the response has started.");
        }

        context.Response.Clear();
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/json; charset=utf-8";

        var result = ApiResult<object>.Error(ApiCodes.Forbidden, DeniedMessage, context.TraceIdentifier);
        await JsonSerializer.SerializeAsync(
            context.Response.Body,
            result,
            WeCmsJsonSerializerContext.Default.ApiResultObject,
            context.RequestAborted);
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
}
