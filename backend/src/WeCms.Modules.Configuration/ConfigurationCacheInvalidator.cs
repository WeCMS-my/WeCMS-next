namespace WeCms.Modules.Configuration;

public interface IConfigurationCacheInvalidator
{
    Task InvalidateSettingsAsync(CancellationToken cancellationToken);

    Task InvalidateDictsAsync(CancellationToken cancellationToken);

    Task InvalidateI18nAsync(CancellationToken cancellationToken);
}

public sealed class NoopConfigurationCacheInvalidator : IConfigurationCacheInvalidator
{
    public Task InvalidateSettingsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    public Task InvalidateDictsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    public Task InvalidateI18nAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }
}
