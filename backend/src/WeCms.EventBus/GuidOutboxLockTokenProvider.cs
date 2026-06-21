using WeCms.Shared.Id;

namespace WeCms.EventBus;

public sealed class GuidOutboxLockTokenProvider(IIdGenerator idGenerator) : IOutboxLockTokenProvider
{
    public string CreateLockToken()
    {
        return idGenerator.NewId();
    }
}
