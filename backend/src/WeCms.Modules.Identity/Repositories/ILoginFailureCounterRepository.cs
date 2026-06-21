namespace WeCms.Modules.Identity.Repositories;

public interface ILoginFailureCounterRepository
{
    Task<LoginFailureCounterRecord> IncrementAsync(
        LoginFailureCounterIncrement record,
        CancellationToken cancellationToken);

    Task ResetAsync(
        string scope,
        string target,
        CancellationToken cancellationToken);

    Task RecordSecurityEventAsync(
        SecurityEventRecord record,
        CancellationToken cancellationToken);
}
