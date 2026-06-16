namespace WeCms.Persistence.Migration;

public interface IDbMigrationRunner
{
    Task<IReadOnlyList<string>> MigrateAsync(string migrationsDirectory, CancellationToken cancellationToken = default);
}
