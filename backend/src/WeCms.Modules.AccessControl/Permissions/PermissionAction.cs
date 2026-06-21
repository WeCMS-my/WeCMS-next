using System.Text.RegularExpressions;

namespace WeCms.Modules.AccessControl.Permissions;

public sealed partial record PermissionAction
{
    public static readonly PermissionAction Page = new("page");
    public static readonly PermissionAction List = new("list");
    public static readonly PermissionAction Tree = new("tree");
    public static readonly PermissionAction Detail = new("detail");
    public static readonly PermissionAction Create = new("create");
    public static readonly PermissionAction Update = new("update");
    public static readonly PermissionAction Delete = new("delete");
    public static readonly PermissionAction Enable = new("enable");
    public static readonly PermissionAction Disable = new("disable");
    public static readonly PermissionAction Sort = new("sort");
    public static readonly PermissionAction AssignPermission = new("assign-permission");
    public static readonly PermissionAction AssignMenu = new("assign-menu");
    public static readonly PermissionAction SecurePing = new("secure-ping");

    private PermissionAction(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static PermissionAction From(string value)
    {
        if (!IsValid(value))
        {
            throw new ArgumentException("Permission action is invalid.", nameof(value));
        }

        return new PermissionAction(value);
    }

    public override string ToString()
    {
        return Value;
    }

    internal static bool IsValid(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) && PermissionActionRegex().IsMatch(value);
    }

    [GeneratedRegex("^[a-z][a-z0-9]*(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex PermissionActionRegex();
}
