using WeCms.Modules.AccessControl.Contracts;

namespace WeCms.Modules.AccessControl.AccessProfiles;

public interface IAccessProfileService
{
    Task<AccessProfileDto> GetAsync(long userId, CancellationToken cancellationToken);
}
