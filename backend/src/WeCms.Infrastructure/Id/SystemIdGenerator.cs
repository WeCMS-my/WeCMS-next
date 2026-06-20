using WeCms.Shared.Id;

namespace WeCms.Infrastructure.Id;

public sealed class SystemIdGenerator : IIdGenerator
{
    public string NewId()
    {
        return Guid.NewGuid().ToString("N");
    }
}
