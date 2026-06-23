using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;
using WeCms.Api.Json;
using WeCms.Modules.Security;
using WeCms.Shared;
using WeCms.Shared.Security;

namespace WeCms.Api.RateLimiting;

public static class WeCmsRateLimitingExtensions
{
    public static IServiceCollection AddWeCmsRateLimiting(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var settings = RateLimitSettings.FromConfiguration(configuration);
        services.AddSingleton(settings);
        services.AddHostedService<RateLimitSecurityEventFlushHostedService>();
        services.AddRateLimiter(options =>
        {
            AddFixedWindowPolicy(options, RateLimitPolicyNames.AuthLogin, settings.AuthLogin, AuthPartition);
            AddFixedWindowPolicy(options, RateLimitPolicyNames.AuthRefresh, settings.AuthRefresh, AuthPartition);
            AddFixedWindowPolicy(options, RateLimitPolicyNames.AuthTwoFactor, settings.AuthTwoFactor, AuthPartition);
            AddFixedWindowPolicy(options, RateLimitPolicyNames.AdminWrite, settings.AdminWrite, UserEndpointPartition);
            AddFixedWindowPolicy(options, RateLimitPolicyNames.FileUpload, settings.FileUpload, UserEndpointPartition);
            AddFixedWindowPolicy(options, RateLimitPolicyNames.SecurityUnban, settings.SecurityUnban, UserEndpointPartition);
            options.OnRejected = OnRejectedAsync;
        });

        return services;
    }

    private static void AddFixedWindowPolicy(
        RateLimiterOptions options,
        string policyName,
        RateLimitRule rule,
        Func<HttpContext, string> partitionFactory)
    {
        options.AddPolicy(policyName, context =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionFactory(context),
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = rule.PermitLimit,
                    Window = rule.Window,
                    QueueLimit = 0,
                    AutoReplenishment = true
                }));
    }

    private static async ValueTask OnRejectedAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var httpContext = context.HttpContext;
        var policyName = httpContext.GetEndpoint()?.Metadata.GetMetadata<EnableRateLimitingAttribute>()?.PolicyName ?? "unknown";
        TryBufferRateLimitHit(httpContext, policyName);

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            httpContext.Response.Headers.RetryAfter = Math.Ceiling(retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
        }

        httpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        httpContext.Response.ContentType = "application/json; charset=utf-8";
        var result = ApiResult<object>.Error(ApiCodes.TooManyRequests, "Too many requests.", httpContext.TraceIdentifier);
        await JsonSerializer.SerializeAsync(
            httpContext.Response.Body,
            result,
            WeCmsJsonSerializerContext.Default.ApiResultObject,
            cancellationToken);
    }

    private static void TryBufferRateLimitHit(HttpContext httpContext, string policyName)
    {
        try
        {
            var clock = httpContext.RequestServices.GetRequiredService<ISecurityClock>();
            var buffer = httpContext.RequestServices.GetRequiredService<IRateLimitHitBuffer>();
            _ = buffer.TryRecord(
                new RateLimitHitRecord(
                    policyName,
                    httpContext.Request.Method,
                    httpContext.Request.Path.Value ?? "/",
                    TryGetUserId(httpContext),
                    httpContext.User.Identity?.Name,
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    httpContext.Request.Headers.UserAgent.ToString(),
                    httpContext.TraceIdentifier,
                    clock.UtcNow));
        }
        catch (Exception exception)
        {
            httpContext.RequestServices
                .GetService<ILoggerFactory>()?
                .CreateLogger(typeof(WeCmsRateLimitingExtensions).FullName ?? nameof(WeCmsRateLimitingExtensions))
                .LogWarning(exception, "Failed to buffer rate-limit security event.");
        }
    }

    private static string AuthPartition(HttpContext context)
    {
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var path = context.Request.Path.Value ?? "/";
        return $"auth:{ip}:{path}";
    }

    private static string UserEndpointPartition(HttpContext context)
    {
        var actor = TryGetUserId(context)?.ToString(CultureInfo.InvariantCulture)
            ?? context.Connection.RemoteIpAddress?.ToString()
            ?? "unknown";
        return $"user:{actor}";
    }

    private static long? TryGetUserId(HttpContext context)
    {
        var value = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var userId) ? userId : null;
    }
}

public sealed record RateLimitSettings(
    RateLimitRule AuthLogin,
    RateLimitRule AuthRefresh,
    RateLimitRule AuthTwoFactor,
    RateLimitRule AdminWrite,
    RateLimitRule FileUpload,
    RateLimitRule SecurityUnban)
{
    public static RateLimitSettings FromConfiguration(IConfiguration configuration)
    {
        return new RateLimitSettings(
            ReadRule(configuration, "Security:RateLimiting:AuthLogin", 5, 1),
            ReadRule(configuration, "Security:RateLimiting:AuthRefresh", 20, 1),
            ReadRule(configuration, "Security:RateLimiting:AuthTwoFactor", 5, 1),
            ReadRule(configuration, "Security:RateLimiting:AdminWrite", 60, 1),
            ReadRule(configuration, "Security:RateLimiting:FileUpload", 10, 1),
            ReadRule(configuration, "Security:RateLimiting:SecurityUnban", 5, 1));
    }

    private static RateLimitRule ReadRule(IConfiguration configuration, string sectionName, int defaultPermitLimit, int defaultWindowMinutes)
    {
        var permitLimit = ReadPositiveInt(configuration, $"{sectionName}:PermitLimit", defaultPermitLimit);
        var windowMinutes = ReadPositiveInt(configuration, $"{sectionName}:WindowMinutes", defaultWindowMinutes);
        return new RateLimitRule(permitLimit, TimeSpan.FromMinutes(windowMinutes));
    }

    private static int ReadPositiveInt(IConfiguration configuration, string key, int defaultValue)
    {
        var value = configuration[key];
        if (string.IsNullOrWhiteSpace(value))
        {
            return defaultValue;
        }

        if (!int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var result) || result <= 0)
        {
            throw new InvalidOperationException($"{key} must be a positive integer.");
        }

        return result;
    }
}

public sealed record RateLimitRule(int PermitLimit, TimeSpan Window);
