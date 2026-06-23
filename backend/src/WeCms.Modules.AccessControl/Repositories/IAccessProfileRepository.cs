using WeCms.Modules.AccessControl.Contracts;

namespace WeCms.Modules.AccessControl.Repositories;

public interface IAccessProfileRepository
{
    Task<long> GetPermissionVersionAsync(long userId, CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> ListRoleCodesAsync(long userId, CancellationToken cancellationToken);

    Task<IReadOnlyList<string>> ListPermissionCodesAsync(long userId, CancellationToken cancellationToken);

    Task<IReadOnlyList<MenuSummaryDto>> ListVisibleMenusAsync(long userId, CancellationToken cancellationToken);
}
