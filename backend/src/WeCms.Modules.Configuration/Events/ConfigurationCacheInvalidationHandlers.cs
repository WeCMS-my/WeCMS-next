using WeCms.EventBus;

namespace WeCms.Modules.Configuration.Events;

public sealed class SettingChangedCacheHandler(IConfigurationCacheInvalidator cacheInvalidator)
    : IEventHandler<SettingChangedEvent>
{
    public Task HandleAsync(SettingChangedEvent integrationEvent, CancellationToken cancellationToken)
    {
        return cacheInvalidator.InvalidateSettingsAsync(cancellationToken);
    }
}

public sealed class DictChangedCacheHandler(IConfigurationCacheInvalidator cacheInvalidator)
    : IEventHandler<DictChangedEvent>
{
    public Task HandleAsync(DictChangedEvent integrationEvent, CancellationToken cancellationToken)
    {
        return cacheInvalidator.InvalidateDictsAsync(cancellationToken);
    }
}

public sealed class I18nChangedCacheHandler(IConfigurationCacheInvalidator cacheInvalidator)
    : IEventHandler<I18nChangedEvent>
{
    public Task HandleAsync(I18nChangedEvent integrationEvent, CancellationToken cancellationToken)
    {
        return cacheInvalidator.InvalidateI18nAsync(cancellationToken);
    }
}
