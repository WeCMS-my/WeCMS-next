using System.Text.Json;
using Microsoft.Extensions.Options;
using WeCms.Api.Json;
using WeCms.Api.Security;
using WeCms.Modules.Identity.Services;
using WeCms.Shared;
using WeCms.Shared.Security;

namespace WeCms.Api.Middleware;

public sealed class IpAccessControlMiddleware
{
    private const string DeniedMessage = "IP access is not allowed.";
    private const string DropLogMessage = "Security rejection event was dropped due to full security rejection event buffer.";

    private readonly RequestDelegate _next;
    private readonly IIpRuleMatcher _ipRuleMatcher;
    private readonly ISecurityRejectionEventBuffer _securityRejectionEventBuffer;
    private readonly ILogger<IpAccessControlMiddleware> _logger;
    private readonly IAuthClock _clock;
    private readonly object _settingsLock = new();
    private IpAccessControlSettings _settings;
    private readonly IDisposable? _optionsChange;

    public IpAccessControlMiddleware(
        RequestDelegate next,
        IOptionsMonitor<IpAccessControlOptions> optionsMonitor,
        IIpRuleMatcher ipRuleMatcher,
        ISecurityRejectionEventBuffer securityRejectionEventBuffer,
        IAuthClock clock,
        ILogger<IpAccessControlMiddleware> logger)
    {
        _next = next;
        _ipRuleMatcher = ipRuleMatcher;
        _securityRejectionEventBuffer = securityRejectionEventBuffer;
        _logger = logger;
        _clock = clock;
        _settings = CreateSettings(optionsMonitor.CurrentValue, ipRuleMatcher);
        _optionsChange = optionsMonitor.OnChange((IpAccessControlOptions options, string? _) =>
            UpdateSettings(options, ipRuleMatcher));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var settings = GetSettings();
        if (!settings.Enabled
            || ShouldSkipHealthEndpoint(context, settings)
            || ShouldSkipAuthEndpoint(context, settings))
        {
            await _next(context);
            return;
        }

        if (settings.ParsedRules.Count == 0)
        {
            throw new InvalidOperationException("Security:IpAccessControl:AllowedRules must contain at least one rule when enabled.");
        }

        var remoteIp = context.Connection.RemoteIpAddress;
        if (remoteIp is not null && _ipRuleMatcher.IsMatch(settings.ParsedRules, remoteIp))
        {
            await _next(context);
            return;
        }

        var now = _clock.UtcNow;
        if (!_securityRejectionEventBuffer.TryEnqueue(SecurityRejectionEvent.FromIpAccessDenied(
                new IpAccessDeniedSecurityEvent(
                    remoteIp?.ToString() ?? string.Empty,
                    context.TraceIdentifier,
                    now))))
        {
            _logger.LogWarning(DropLogMessage);
        }

        await WriteForbiddenAsync(context);
    }

    public ValueTask DisposeAsync()
    {
        _optionsChange?.Dispose();
        return ValueTask.CompletedTask;
    }

    private IpAccessControlSettings GetSettings()
    {
        lock (_settingsLock)
        {
            return _settings;
        }
    }

    private void UpdateSettings(IpAccessControlOptions options, IIpRuleMatcher ipRuleMatcher)
    {
        lock (_settingsLock)
        {
            _settings = CreateSettings(options, ipRuleMatcher);
        }
    }

    private static IpAccessControlSettings CreateSettings(IpAccessControlOptions options, IIpRuleMatcher ipRuleMatcher)
    {
        return new IpAccessControlSettings(
            options.Enabled,
            options.SkipHealthEndpoints,
            options.ApplyToAuthEndpoints,
            ipRuleMatcher.ParseRules(options.AllowedRules));
    }

    private bool ShouldSkipHealthEndpoint(HttpContext context, IpAccessControlSettings settings)
    {
        return settings.SkipHealthEndpoints
            && context.Request.Path.StartsWithSegments("/health", StringComparison.OrdinalIgnoreCase);
    }

    private bool ShouldSkipAuthEndpoint(HttpContext context, IpAccessControlSettings settings)
    {
        return !settings.ApplyToAuthEndpoints
            && context.Request.Path.StartsWithSegments("/api/v1/auth", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task WriteForbiddenAsync(HttpContext context)
    {
        if (context.Response.HasStarted)
        {
            return;
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
}

public sealed record IpAccessControlOptions
{
    public const string SectionName = "Security:IpAccessControl";

    public bool Enabled { get; init; }
    public bool SkipHealthEndpoints { get; init; } = true;
    public bool ApplyToAuthEndpoints { get; init; } = true;
    public string[] AllowedRules { get; init; } = [];
}

internal sealed record IpAccessControlSettings(
    bool Enabled,
    bool SkipHealthEndpoints,
    bool ApplyToAuthEndpoints,
    IReadOnlyList<ParsedIpRule> ParsedRules);
