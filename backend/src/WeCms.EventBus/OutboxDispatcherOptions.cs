namespace WeCms.EventBus;

public sealed class OutboxDispatcherOptions
{
    public int BatchSize { get; set; } = 20;

    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMinutes(1);
}
