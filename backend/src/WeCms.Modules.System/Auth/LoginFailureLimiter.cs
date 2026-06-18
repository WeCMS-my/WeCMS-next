using WeCms.Modules.System.Security;

namespace WeCms.Modules.System.Auth;

public sealed class LoginFailureLimiter : ILoginFailureLimiter
{
    private const string UsernameScope = "username";
    private const string IpScope = "ip";
    private readonly ILoginFailureCounterRepository _repository;
    private readonly ISecurityBanService _securityBanService;
    private readonly LoginFailurePolicyOptions _options;

    public LoginFailureLimiter(
        ILoginFailureCounterRepository repository,
        ISecurityBanService securityBanService,
        LoginFailurePolicyOptions options)
    {
        _repository = repository;
        _securityBanService = securityBanService;
        _options = options;
    }

    public Task<LoginFailureDecision> RecordFailureAsync(LoginFailureContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        return RecordFailureCoreAsync(context, cancellationToken);
    }

    public async Task ResetAsync(string username, string ip, CancellationToken cancellationToken)
    {
        await _repository.ResetAsync(UsernameScope, NormalizeRequired(username, "username", 64), cancellationToken);
        await _repository.ResetAsync(IpScope, NormalizeRequired(ip, "ip", AuthRequestContext.MaxIpLength), cancellationToken);
    }

    private async Task<LoginFailureDecision> RecordFailureCoreAsync(LoginFailureContext context, CancellationToken cancellationToken)
    {
        if (!_options.Enabled)
        {
            return LoginFailureDecision.Allowed;
        }

        var username = NormalizeRequired(context.Username, "username", 64);
        var ip = NormalizeRequired(context.Ip, "ip", AuthRequestContext.MaxIpLength);
        var usernameCounter = await _repository.IncrementAsync(
            new LoginFailureCounterIncrement(UsernameScope, username, context.Now, context.Now, _options.Window),
            cancellationToken);
        var ipCounter = await _repository.IncrementAsync(
            new LoginFailureCounterIncrement(IpScope, ip, context.Now, context.Now, _options.Window),
            cancellationToken);

        var shouldBlock = usernameCounter.FailureCount >= _options.UsernameThreshold
            || ipCounter.FailureCount >= _options.IpThreshold;
        var shouldBan = Math.Max(usernameCounter.FailureCount, ipCounter.FailureCount) >= _options.BanThreshold;

        if (shouldBlock)
        {
            await _repository.RecordSecurityEventAsync(
                new SecurityEventRecord(
                    "auth.login_rate_limited",
                    context.UserId,
                    username,
                    ip,
                    shouldBan ? "critical" : "warning",
                    "Login failure threshold reached.",
                    context.Now),
                cancellationToken);
        }

        if (shouldBan)
        {
            await CreateTemporaryBansAsync(context, username, ip, cancellationToken);
        }

        return shouldBlock ? LoginFailureDecision.Blocked : LoginFailureDecision.Allowed;
    }

    private async Task CreateTemporaryBansAsync(
        LoginFailureContext context,
        string username,
        string ip,
        CancellationToken cancellationToken)
    {
        var expiresAt = context.Now.Add(_options.BanDuration);
        if (context.UserId is { } userId)
        {
            await CreateIfNoActiveBanAsync(
                new CreateSecurityBanRecord(
                    SecurityBanTypes.User,
                    userId.ToString(global::System.Globalization.CultureInfo.InvariantCulture),
                    "Temporary ban after repeated login failures.",
                    "critical",
                    "auth.login_failure",
                    expiresAt,
                    context.Now),
                cancellationToken);
        }

        await CreateIfNoActiveBanAsync(
            new CreateSecurityBanRecord(
                SecurityBanTypes.Ip,
                ip,
                $"Temporary ban after repeated login failures for {username}.",
                "critical",
                "auth.login_failure",
                expiresAt,
                context.Now),
            cancellationToken);
    }

    private async Task CreateIfNoActiveBanAsync(
        CreateSecurityBanRecord record,
        CancellationToken cancellationToken)
    {
        var active = await _securityBanService.FindActiveAsync(record.BanType, record.Target, record.CreatedAt, cancellationToken);
        if (active is null)
        {
            await _securityBanService.CreateTemporaryAsync(record, cancellationToken);
        }
    }

    private static string NormalizeRequired(string value, string parameterName, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"{parameterName} is required for login failure limiting.");
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new InvalidOperationException($"{parameterName} must be {maxLength} characters or fewer.");
        }

        return normalized;
    }
}
