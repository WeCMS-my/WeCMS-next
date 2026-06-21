namespace WeCms.Modules.Identity.Services;

public interface ILoginFailureLimiter
{
    Task<LoginFailureDecision> RecordFailureAsync(LoginFailureContext context, CancellationToken cancellationToken);

    Task ResetAsync(string username, string ip, CancellationToken cancellationToken);
}
