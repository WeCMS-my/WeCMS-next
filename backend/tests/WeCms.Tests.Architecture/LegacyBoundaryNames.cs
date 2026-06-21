namespace WeCms.Tests.Architecture;

internal static class LegacyBoundaryNames
{
    public static string SystemModule => string.Concat("WeCms.Modules.", "System");

    public static string Persistence => string.Concat("WeCms.", "Persistence");

    public static string SystemProject => string.Concat(SystemModule, ".csproj");

    public static string PersistenceProject => string.Concat(Persistence, ".csproj");

    public static string SystemNamespace(string suffix)
    {
        return string.Concat(SystemModule, ".", suffix);
    }

    public static string PersistenceSystemNamespace(string suffix)
    {
        return string.Concat(Persistence, ".Modules.", "System.", suffix);
    }

    public static string SystemSourcePath(string suffix)
    {
        return string.Concat("backend/src/", SystemModule, "/", suffix);
    }

    public static string PersistenceSystemSourcePath(string suffix)
    {
        return string.Concat("backend/src/", Persistence, "/Modules/", "System/", suffix);
    }
}
