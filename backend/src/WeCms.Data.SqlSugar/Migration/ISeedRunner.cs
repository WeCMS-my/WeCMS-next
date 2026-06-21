namespace WeCms.Data.SqlSugar;

public interface ISeedRunner
{
    Task<IReadOnlyList<string>> SeedAsync(
        string seedsDirectory,
        SeedRunnerOptions options,
        CancellationToken cancellationToken = default);
}
