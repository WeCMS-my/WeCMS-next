using WeCms.Modules.AccessControl.Contracts;
using WeCms.Modules.AccessControl.Records;

namespace WeCms.Modules.AccessControl.Menus;

public interface IMenuService
{
    Task<IReadOnlyList<MenuSummaryDto>> ListAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<MenuTreeDto>> TreeAsync(CancellationToken cancellationToken);

    Task<MenuDetailDto> GetAsync(long id, CancellationToken cancellationToken);

    Task<MenuMutationResponse> CreateAsync(CreateMenuRequest request, MenuRequestContext context, CancellationToken cancellationToken);

    Task<MenuMutationResponse> UpdateAsync(long id, UpdateMenuRequest request, MenuRequestContext context, CancellationToken cancellationToken);

    Task SortAsync(SortMenusRequest request, MenuRequestContext context, CancellationToken cancellationToken);

    Task DeleteAsync(long id, MenuRequestContext context, CancellationToken cancellationToken);

    Task EnableAsync(long id, MenuRequestContext context, CancellationToken cancellationToken);

    Task DisableAsync(long id, MenuRequestContext context, CancellationToken cancellationToken);
}
