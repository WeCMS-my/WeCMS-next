
using WeCms.Modules.AccessControl.Contracts;

namespace WeCms.Modules.Identity.Services;

internal static class AuthAccessProfileMapper
{
    public static IReadOnlyList<AuthMenuTreeDto> ToAuthMenuTree(IReadOnlyList<MenuTreeDto> menus)
    {
        return menus
            .Select(menu => new AuthMenuTreeDto(
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
                ToAuthMenuTree(menu.Children)))
            .ToArray();
    }
}
