using WeCms.Shared.Id;

namespace WeCms.Infrastructure.Id;

public sealed class SystemGuidIdGenerator : IIdGenerator
{
    public Guid NewGuid() => Guid.NewGuid();
}
