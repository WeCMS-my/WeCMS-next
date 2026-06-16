namespace WeCms.Tests.Architecture;

internal static class TestPaths
{
    public static string RepoRoot { get; } = FindRepoRoot();

    public static string BackendRoot => Path.Combine(RepoRoot, "backend");

    public static string SourceRoot => Path.Combine(BackendRoot, "src");

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "backend", "WeCms.slnx")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root from test output directory.");
    }
}
