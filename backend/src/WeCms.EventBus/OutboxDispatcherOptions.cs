namespace WeCms.EventBus;

public sealed class OutboxDispatcherOptions
{
    public int BatchSize { get; set; } = 20;

    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMinutes(1);

    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(5);

    public TimeSpan IdlePollInterval { get; set; } = TimeSpan.FromSeconds(15);

    public TimeSpan FailurePollInterval { get; set; } = TimeSpan.FromSeconds(30);
}
