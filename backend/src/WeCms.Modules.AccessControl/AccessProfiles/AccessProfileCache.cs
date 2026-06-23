using WeCms.Modules.AccessControl.Contracts;

namespace WeCms.Modules.AccessControl.AccessProfiles;

public interface IAccessProfileCache
{
    ValueTask<AccessProfileDto?> GetAsync(
        long userId,
        bool isSuperAdmin,
        long permissionVersion,
        CancellationToken cancellationToken);

    ValueTask SetAsync(
        long userId,
        bool isSuperAdmin,
        AccessProfileDto profile,
        CancellationToken cancellationToken);
}
