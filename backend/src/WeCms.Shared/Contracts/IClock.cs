namespace WeCms.Shared.Contracts;

public interface IClock
{
    DateTimeOffset UtcNow { get; }
}
