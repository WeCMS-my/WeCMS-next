namespace WeCms.Modules.Organization;

public interface IOrganizationClock
{
    DateTimeOffset UtcNow { get; }
}

public sealed class SystemOrganizationClock : IOrganizationClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
