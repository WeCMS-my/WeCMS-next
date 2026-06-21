namespace WeCms.Modules.AccessControl.Contracts;

public sealed record AccessProfileDto(
    long PermissionVersion,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    IReadOnlyList<string> Buttons,
    IReadOnlyList<MenuTreeDto> Menus);
