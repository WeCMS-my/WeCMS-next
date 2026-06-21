namespace WeCms.Modules.Security;

public interface ISecurityClock
{
    DateTimeOffset UtcNow { get; }
}

public sealed class SystemSecurityClock : ISecurityClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
