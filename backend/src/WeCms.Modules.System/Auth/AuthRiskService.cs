using WeCms.Shared.Time;

namespace WeCms.Modules.System.Auth;

public interface IAuthRiskService
{
    Task<AuthRiskDecision> EvaluateLoginAsync(
        string username,
        string ipAddress,
        CancellationToken cancellationToken);

    Task<int> GetRefreshTokenReuseSeverityAsync(
        long userId,
        string ipAddress,
        CancellationToken cancellationToken);
}

public sealed record AuthRiskDecision(
    bool IsBlocked,
    bool RequiresCaptcha,
    string EventType,
    string Description,
    int Severity);

public sealed record AuthRiskOptions(
    int WindowMinutes,
    int CaptchaUsernameIpFailures,
    int MaxUsernameIpFailures,
    int MaxUsernameFailures,
    int MaxIpFailures,
    int EscalatedRefreshReuseFailures)
{
    public static AuthRiskOptions Default { get; } = new(15, 3, 5, 10, 20, 3);
}

public sealed class AuthRiskService : IAuthRiskService
{
    private readonly IAuthRepository _repository;
    private readonly IClock _clock;
    private readonly AuthRiskOptions _options;

    public AuthRiskService(IAuthRepository repository, IClock clock)
        : this(repository, clock, AuthRiskOptions.Default)
    {
    }

    public AuthRiskService(IAuthRepository repository, IClock clock, AuthRiskOptions options)
    {
        _repository = repository;
        _clock = clock;
        _options = options;
    }

    public async Task<AuthRiskDecision> EvaluateLoginAsync(
        string username,
        string ipAddress,
        CancellationToken cancellationToken)
    {
        var since = _clock.UtcNow.AddMinutes(-_options.WindowMinutes);

        var usernameIpFailures = await _repository.CountRecentFailedLoginAttemptsAsync(
            null,
            username,
            ipAddress,
            since,
            cancellationToken);
        if (usernameIpFailures >= _options.MaxUsernameIpFailures)
        {
            return new AuthRiskDecision(
                true,
                false,
                "login_rate_limited_username_ip",
                "username + IP 登录失败过多",
                3);
        }

        if (usernameIpFailures >= _options.CaptchaUsernameIpFailures)
        {
            return new AuthRiskDecision(
                false,
                true,
                "login_captcha_required",
                "username + IP 登录失败达到验证码阈值",
                1);
        }

        var usernameFailures = await _repository.CountRecentFailedLoginAttemptsAsync(
            null,
            username,
            null,
            since,
            cancellationToken);
        if (usernameFailures >= _options.MaxUsernameFailures)
        {
            return new AuthRiskDecision(
                true,
                false,
                "login_rate_limited_username",
                "username 登录失败过多",
                3);
        }

        var ipFailures = await _repository.CountRecentFailedLoginAttemptsAsync(
            null,
            null,
            ipAddress,
            since,
            cancellationToken);
        if (ipFailures >= _options.MaxIpFailures)
        {
            return new AuthRiskDecision(
                true,
                false,
                "login_rate_limited_ip",
                "IP 登录失败过多",
                3);
        }

        return new AuthRiskDecision(false, false, "", "", 0);
    }

    public async Task<int> GetRefreshTokenReuseSeverityAsync(
        long userId,
        string ipAddress,
        CancellationToken cancellationToken)
    {
        var since = _clock.UtcNow.AddMinutes(-_options.WindowMinutes);
        var reuseEvents = await _repository.CountRecentSecurityEventsAsync(
            null,
            "token_reuse",
            userId,
            ipAddress,
            since,
            cancellationToken);

        return reuseEvents + 1 >= _options.EscalatedRefreshReuseFailures ? 4 : 3;
    }
}
