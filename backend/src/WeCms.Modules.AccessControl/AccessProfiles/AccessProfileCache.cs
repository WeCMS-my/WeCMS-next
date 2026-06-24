using WeCms.Modules.AccessControl.Contracts;

namespace WeCms.Modules.AccessControl.AccessProfiles;

public interface IAccessProfileCache
{
    ValueTask<AccessProfileDto?> GetAsync(
        long userId,
        long permissionVersion,
        CancellationToken cancellationToken);

    ValueTask SetAsync(
        long userId,
        AccessProfileDto profile,
        CancellationToken cancellationToken);
}
