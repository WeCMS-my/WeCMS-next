namespace WeCms.Modules.AccessControl.Permissions;

public interface IAccessControlClock
{
    DateTimeOffset UtcNow { get; }
}

public sealed class SystemAccessControlClock : IAccessControlClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
