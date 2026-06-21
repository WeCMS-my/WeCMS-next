namespace WeCms.Modules.Configuration;

public interface IConfigurationClock
{
    DateTimeOffset UtcNow { get; }
}

public sealed class SystemConfigurationClock : IConfigurationClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
