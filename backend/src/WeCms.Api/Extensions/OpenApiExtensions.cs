namespace WeCms.Api.Extensions;

public static class OpenApiExtensions
{
    private const string ExportArg = "--export-openapi";
    private const string ArtifactRelativePath = "artifacts/openapi/wecms-api-v1.json";

    public static bool IsExportMode(string[] args)
        => args.Length >= 2 && args[0] == ExportArg;

    public static string GetExportPath(string[] args)
        => args[1];

    public static Task ExportOpenApiAsync(this WebApplication app, string outputPath)
    {
        var repoRoot = FindRepositoryRoot();
        var artifactPath = Path.Combine(repoRoot, ArtifactRelativePath);

        if (!File.Exists(artifactPath))
        {
            throw new FileNotFoundException($"OpenAPI artifact not found: {artifactPath}", artifactPath);
        }

        var dir = Path.GetDirectoryName(outputPath);
        if (dir is not null)
        {
            Directory.CreateDirectory(dir);
        }

        File.Copy(artifactPath, outputPath, overwrite: true);

        Console.WriteLine($"OpenAPI document exported to: {outputPath}");
        return Task.CompletedTask;
    }

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "AGENTS.md")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new InvalidOperationException("Unable to locate repository root.");
    }
}
