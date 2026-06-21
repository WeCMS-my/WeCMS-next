using System.Text.RegularExpressions;

namespace WeCms.Modules.AccessControl.Permissions;

public sealed partial record PermissionDefinition
{
    public PermissionDefinition(
        string code,
        string module,
        PermissionKind kind,
        PermissionAction action,
        string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (string.IsNullOrWhiteSpace(code) || !PermissionCodeRegex().IsMatch(code))
        {
            throw new ArgumentException("Permission code must use domain:resource:action format.", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(module) || !ModuleRegex().IsMatch(module))
        {
            throw new ArgumentException("Permission module is invalid.", nameof(module));
        }

        if (!Enum.IsDefined(kind))
        {
            throw new ArgumentOutOfRangeException(nameof(kind));
        }

        ArgumentNullException.ThrowIfNull(action);

        var codeAction = code[(code.LastIndexOf(':') + 1)..];
        if (!StringComparer.Ordinal.Equals(codeAction, action.Value))
        {
            throw new ArgumentException("Permission action must match the permission code action segment.", nameof(action));
        }

        Code = code;
        Module = module;
        Kind = kind;
        Action = action;
        Name = name;
    }

    public string Code { get; }

    public string Module { get; }

    public PermissionKind Kind { get; }

    public PermissionAction Action { get; }

    public string Name { get; }

    [GeneratedRegex("^[a-z][a-z0-9]*:[a-z][a-z0-9]*(?:-[a-z0-9]+)*:[a-z][a-z0-9]*(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex PermissionCodeRegex();

    [GeneratedRegex("^[a-z][a-z0-9]*(?:-[a-z0-9]+)*$", RegexOptions.CultureInvariant)]
    private static partial Regex ModuleRegex();
}
