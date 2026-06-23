namespace WeCms.Modules.Security;

public interface ISecurityBanLookupCache
{
    ValueTask<SecurityBanLookupCacheResult?> GetAsync(
        string banType,
        string target,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    ValueTask SetAsync(
        SecurityBanRecord record,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    ValueTask SetMissAsync(
        string banType,
        string target,
        DateTimeOffset now,
        CancellationToken cancellationToken);

    ValueTask RemoveAsync(
        string banType,
        string target,
        CancellationToken cancellationToken);
}

public sealed record SecurityBanLookupCacheResult(SecurityBanRecord? Record)
{
    public static SecurityBanLookupCacheResult Miss { get; } = new((SecurityBanRecord?)null);
}
