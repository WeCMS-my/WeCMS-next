namespace WeCms.Shared.Time;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
