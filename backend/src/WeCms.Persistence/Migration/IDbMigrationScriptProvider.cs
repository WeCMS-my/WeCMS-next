namespace WeCms.Persistence.Migration;

public interface IDbMigrationScriptProvider
{
    IReadOnlyList<DbMigrationScript> GetSchemaMigrations();

    IReadOnlyList<DbMigrationScript> GetSeeds();
}
