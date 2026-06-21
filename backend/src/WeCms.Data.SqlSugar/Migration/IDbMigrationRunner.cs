namespace WeCms.Data.SqlSugar;

public interface IDbMigrationRunner
{
    Task<IReadOnlyList<string>> MigrateAsync(string migrationsDirectory, CancellationToken cancellationToken = default);
}
