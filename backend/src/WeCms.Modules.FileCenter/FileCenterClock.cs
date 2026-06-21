namespace WeCms.Modules.FileCenter;

public interface IFileCenterClock
{
    DateTimeOffset UtcNow { get; }
}

public sealed class SystemFileCenterClock : IFileCenterClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
