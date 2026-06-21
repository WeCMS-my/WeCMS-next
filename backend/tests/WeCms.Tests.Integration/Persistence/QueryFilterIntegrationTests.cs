using SqlSugar;
using WeCms.Data.SqlSugar;
using WeCms.Shared.Data;
using WeCms.Tests.Integration;

namespace WeCms.Tests.Integration.Persistence;

[Collection(nameof(SharedMySqlCollection))]
public sealed class QueryFilterIntegrationTests : PerTestDatabaseResetBase
{
    [DbFact]
    public void SoftDeletedRowsHiddenByDefault()
    {
        using var db = CreateFilteredClient(new QueryFilterContext(TenantId: 1, DataScopeUserIds: [10, 20]));
        CreateProbeTable(db);
        InsertProbe(db, 1, 1, 10, null);
        InsertProbe(db, 2, 1, 10, DateTime.UtcNow);

        var ids = db.Queryable<QueryFilterProbeEntity>().Select(row => row.Id).ToList();

        Assert.Equal([1], ids);
    }

    [DbFact]
    public void TenantRowsAreIsolated()
    {
        using var db = CreateFilteredClient(new QueryFilterContext(TenantId: 1, DataScopeUserIds: [10, 20]));
        CreateProbeTable(db);
        InsertProbe(db, 1, 1, 10, null);
        InsertProbe(db, 2, 2, 10, null);

        var ids = db.Queryable<QueryFilterProbeEntity>().Select(row => row.Id).ToList();

        Assert.Equal([1], ids);
    }

    [DbFact]
    public void DataScopeFiltersRows()
    {
        using var db = CreateFilteredClient(new QueryFilterContext(TenantId: 1, DataScopeUserIds: [10]));
        CreateProbeTable(db);
        InsertProbe(db, 1, 1, 10, null);
        InsertProbe(db, 2, 1, 20, null);

        var ids = db.Queryable<QueryFilterProbeEntity>().Select(row => row.Id).ToList();

        Assert.Equal([1], ids);
    }

    private static void CreateProbeTable(ISqlSugarClient db)
    {
        db.Ado.ExecuteCommand(
            """
            CREATE TABLE s10_query_filter_probe (
              id BIGINT NOT NULL PRIMARY KEY,
              tenant_id BIGINT NOT NULL,
              created_by_user_id BIGINT NOT NULL,
              deleted_at DATETIME(6) NULL
            ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci
            """);
    }

    private static void InsertProbe(ISqlSugarClient db, long id, long tenantId, long createdByUserId, DateTime? deletedAt)
    {
        db.Ado.ExecuteCommand(
            "INSERT INTO s10_query_filter_probe (id, tenant_id, created_by_user_id, deleted_at) VALUES (@id, @tenantId, @createdByUserId, @deletedAt)",
            new SugarParameter("@id", id),
            new SugarParameter("@tenantId", tenantId),
            new SugarParameter("@createdByUserId", createdByUserId),
            new SugarParameter("@deletedAt", deletedAt));
    }

    private ISqlSugarClient CreateFilteredClient(QueryFilterContext context)
    {
        var registrar = new QueryFilterRegistrar(
            new CodeFirstModelRegistry([new TestModelProvider(typeof(QueryFilterProbeEntity))]),
            new StaticQueryFilterContextAccessor(context),
            new QueryFilterBypass(new NullQueryFilterBypassAuditSink()));

        return new SqlSugarClientFactory(
            IntegrationTestDatabase.GetConnectionString(),
            DatabasePlatformOptions.DefaultCommandTimeoutSeconds,
            queryFilterRegistrars: [registrar],
            auditRegistrars: []).Create();
    }

    private sealed class TestModelProvider : ICodeFirstModelProvider
    {
        private readonly IReadOnlyCollection<Type> _modelTypes;

        public TestModelProvider(params Type[] modelTypes)
        {
            _modelTypes = modelTypes;
        }

        public IReadOnlyCollection<Type> GetModelTypes()
        {
            return _modelTypes;
        }
    }

    private sealed class StaticQueryFilterContextAccessor : IQueryFilterContextAccessor
    {
        public StaticQueryFilterContextAccessor(QueryFilterContext current)
        {
            Current = current;
        }

        public QueryFilterContext Current { get; }
    }

    [SugarTable("s10_query_filter_probe")]
    private sealed class QueryFilterProbeEntity : ISoftDeleteEntity, ITenantEntity, IDataScopedEntity
    {
        [SugarColumn(IsPrimaryKey = true, ColumnName = "id")]
        public long Id { get; set; }

        [SugarColumn(ColumnName = "tenant_id")]
        public long TenantId { get; set; }

        [SugarColumn(ColumnName = "created_by_user_id")]
        public long CreatedByUserId { get; set; }

        [SugarColumn(ColumnName = "deleted_at", IsNullable = true)]
        public DateTime? DeletedAt { get; set; }
    }
}
