using WeCms.Modules.AccessControl.Contracts;

namespace WeCms.Modules.AccessControl.Menus;

public static class MenuTreeBuilder
{
    public static IReadOnlyList<MenuTreeDto> Build(IReadOnlyList<MenuSummaryDto> menus)
    {
        return BuildChildren(menus, null);
    }

    private static IReadOnlyList<MenuTreeDto> BuildChildren(IReadOnlyList<MenuSummaryDto> menus, long? parentId)
    {
        return menus
            .Where(menu => menu.ParentId == parentId)
            .OrderBy(menu => menu.Sort)
            .ThenBy(menu => menu.Id)
            .Select(menu => new MenuTreeDto(
                menu.Id,
                menu.ParentId,
                menu.Type,
                menu.Code,
                menu.Path,
                menu.Component,
                menu.Title,
                menu.I18nKey,
                menu.Icon,
                menu.Sort,
                menu.Hidden,
                menu.KeepAlive,
                menu.ExternalUrl,
                menu.PermissionCode,
                menu.Status,
                menu.IsBuiltin,
                BuildChildren(menus, menu.Id)))
            .ToArray();
    }
}
