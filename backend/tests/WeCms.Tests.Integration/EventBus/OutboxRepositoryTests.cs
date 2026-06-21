using System.Text.Json;
using SqlSugar;
using WeCms.Data.SqlSugar;
using WeCms.EventBus;
using global::WeCms.EventBus.SqlSugar;
using global::WeCms.EventBus.SqlSugar.Repositories;

namespace WeCms.Tests.Integration.EventBus;

[Collection(nameof(SharedMySqlCollection))]
public sealed class OutboxRepositoryTests : PerTestDatabaseResetBase
{
    [DbFact]
    public async Task OutboxWriter_WritesMessage()
    {
        using var db = new SqlSugarClientFactory(IntegrationTestDatabase.GetConnectionString()).Create();
        await PrepareDatabaseAsync(db);
        var repository = new OutboxMessageRepository(db);
        var writer = new SqlSugarOutboxWriter(repository);
        var integrationEvent = TestIntegrationEvent.Create("outbox.writer");

        await writer.WriteAsync(integrationEvent, TestContext.Current.CancellationToken);

        var row = await db.Ado.SqlQuerySingleAsync<OutboxRow>(
            """
            SELECT event_id AS EventId,
                   event_type AS EventType,
                   payload_json AS PayloadJson,
                   status AS Status,
                   retry_count AS RetryCount,
                   available_at AS AvailableAt,
                   created_at AS CreatedAt
            FROM sys_outbox_message
            WHERE event_id = @eventId
            LIMIT 1
            """,
            new SugarParameter("@eventId", integrationEvent.Id.ToString("D")));

        Assert.NotNull(row);
        Assert.Equal(integrationEvent.Id.ToString("D"), row.EventId);
        Assert.Equal(integrationEvent.Type, row.EventType);
        Assert.Equal(OutboxMessageStatus.Pending, row.Status);
        Assert.Equal(0, row.RetryCount);
        Assert.Equal(integrationEvent.OccurredAt.UtcDateTime, row.AvailableAt);
        Assert.Equal(integrationEvent.OccurredAt.UtcDateTime, row.CreatedAt);
        Assert.Equal("outbox.writer", PayloadName(row.PayloadJson));
    }

    [DbFact]
    public async Task OutboxWriter_UsesCurrentDatabaseTransaction()
    {
        using var db = new SqlSugarClientFactory(IntegrationTestDatabase.GetConnectionString()).Create();
        await PrepareDatabaseAsync(db);
        var repository = new OutboxMessageRepository(db);
        var writer = new SqlSugarOutboxWriter(repository);
        var integrationEvent = TestIntegrationEvent.Create("outbox.rollback");

        db.Ado.BeginTran();
        try
        {
            await writer.WriteAsync(integrationEvent, TestContext.Current.CancellationToken);
        }
        finally
        {
            db.Ado.RollbackTran();
        }

        var count = await db.Ado.GetIntAsync(
            """
            SELECT COUNT(1)
            FROM sys_outbox_message
            WHERE event_id = @eventId
            """,
            new SugarParameter("@eventId", integrationEvent.Id.ToString("D")));

        Assert.Equal(0, count);
    }

    [DbFact]
    public async Task OutboxRepository_LocksPendingMessages()
    {
        using var db = new SqlSugarClientFactory(IntegrationTestDatabase.GetConnectionString()).Create();
        await PrepareDatabaseAsync(db);
        var repository = new OutboxMessageRepository(db);
        var now = new DateTimeOffset(2026, 6, 21, 3, 0, 0, TimeSpan.Zero);
        await repository.WriteAsync(CreateWriteRecord("ready", now.AddMinutes(-1)), TestContext.Current.CancellationToken);
        await repository.WriteAsync(CreateWriteRecord("future", now.AddMinutes(5)), TestContext.Current.CancellationToken);

        var locked = await repository.LockPendingMessagesAsync(10, now, "lock-a", TestContext.Current.CancellationToken);

        Assert.Single(locked);
        Assert.Equal("ready", PayloadName(locked.Single().PayloadJson));
        Assert.Equal(OutboxMessageStatus.Processing, locked.Single().Status);
        Assert.Equal(now.UtcDateTime, locked.Single().LockedAt?.UtcDateTime);
        Assert.Equal("lock-a", locked.Single().LockToken);
    }

    [DbFact]
    public async Task OutboxRepository_DoesNotReturnRowsLockedByAnotherToken()
    {
        using var db = new SqlSugarClientFactory(IntegrationTestDatabase.GetConnectionString()).Create();
        await PrepareDatabaseAsync(db);
        var repository = new OutboxMessageRepository(db);
        var now = new DateTimeOffset(2026, 6, 21, 3, 0, 0, TimeSpan.Zero);
        await repository.WriteAsync(CreateWriteRecord("single-lock-owner", now.AddMinutes(-1)), TestContext.Current.CancellationToken);

        var firstLocked = await repository.LockPendingMessagesAsync(10, now, "lock-owner-a", TestContext.Current.CancellationToken);
        var secondLocked = await repository.LockPendingMessagesAsync(10, now, "lock-owner-b", TestContext.Current.CancellationToken);

        Assert.Single(firstLocked);
        Assert.Empty(secondLocked);
        Assert.Equal("lock-owner-a", firstLocked.Single().LockToken);
    }

    [DbFact]
    public async Task OutboxRepository_MarksProcessed()
    {
        using var db = new SqlSugarClientFactory(IntegrationTestDatabase.GetConnectionString()).Create();
        await PrepareDatabaseAsync(db);
        var repository = new OutboxMessageRepository(db);
        var now = new DateTimeOffset(2026, 6, 21, 3, 0, 0, TimeSpan.Zero);
        await repository.WriteAsync(CreateWriteRecord("processed", now), TestContext.Current.CancellationToken);
        var message = (await repository.LockPendingMessagesAsync(1, now, "lock-processed", TestContext.Current.CancellationToken)).Single();

        await repository.MarkProcessedAsync(message.Id, now.AddMinutes(1), TestContext.Current.CancellationToken);

        var row = await LoadRowAsync(db, message.Id);
        Assert.Equal(OutboxMessageStatus.Processed, row.Status);
        Assert.Equal(now.AddMinutes(1).UtcDateTime, row.ProcessedAt);
    }

    [DbFact]
    public async Task OutboxRepository_MarksFailedWithRetry()
    {
        using var db = new SqlSugarClientFactory(IntegrationTestDatabase.GetConnectionString()).Create();
        await PrepareDatabaseAsync(db);
        var repository = new OutboxMessageRepository(db);
        var now = new DateTimeOffset(2026, 6, 21, 3, 0, 0, TimeSpan.Zero);
        await repository.WriteAsync(CreateWriteRecord("failed", now), TestContext.Current.CancellationToken);
        var message = (await repository.LockPendingMessagesAsync(1, now, "lock-failed", TestContext.Current.CancellationToken)).Single();

        await repository.MarkFailedAsync(message.Id, "boom", now.AddMinutes(2), TestContext.Current.CancellationToken);

        var row = await LoadRowAsync(db, message.Id);
        Assert.Equal(OutboxMessageStatus.Failed, row.Status);
        Assert.Equal(1, row.RetryCount);
        Assert.Equal("boom", row.Error);
        Assert.Equal(now.AddMinutes(2).UtcDateTime, row.AvailableAt);
    }

    private static OutboxMessageWriteRecord CreateWriteRecord(string name, DateTimeOffset availableAt)
    {
        var integrationEvent = TestIntegrationEvent.Create(name, availableAt);
        return new OutboxMessageWriteRecord(
            integrationEvent.Id,
            integrationEvent.Type,
            null,
            null,
            JsonSerializer.Serialize(integrationEvent),
            availableAt,
            availableAt);
    }

    private static async Task<OutboxRow> LoadRowAsync(ISqlSugarClient db, long id)
    {
        var row = await db.Ado.SqlQuerySingleAsync<OutboxRow>(
            """
            SELECT id AS Id,
                   status AS Status,
                   retry_count AS RetryCount,
                   available_at AS AvailableAt,
                   processed_at AS ProcessedAt,
                   error AS Error
            FROM sys_outbox_message
            WHERE id = @id
            LIMIT 1
            """,
            new SugarParameter("@id", id));

        return row ?? throw new InvalidOperationException("Outbox row was not found.");
    }

    private static string? PayloadName(string payloadJson)
    {
        using var document = JsonDocument.Parse(payloadJson);
        return document.RootElement.GetProperty("Name").GetString();
    }

    private sealed record TestIntegrationEvent(
        Guid Id,
        string Type,
        DateTimeOffset OccurredAt,
        string? TraceId,
        string? TenantId,
        string Name) : IntegrationEventBase(Id, Type, OccurredAt, TraceId, TenantId)
    {
        public static TestIntegrationEvent Create(string name)
        {
            return Create(name, new DateTimeOffset(2026, 6, 21, 2, 0, 0, TimeSpan.Zero));
        }

        public static TestIntegrationEvent Create(string name, DateTimeOffset occurredAt)
        {
            return new TestIntegrationEvent(
                Guid.NewGuid(),
                "test.outbox",
                occurredAt,
                "trace-outbox",
                "tenant-outbox",
                name);
        }
    }

    private sealed class OutboxRow
    {
        public long Id { get; set; }

        public string EventId { get; set; } = string.Empty;

        public string EventType { get; set; } = string.Empty;

        public string PayloadJson { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public int RetryCount { get; set; }

        public DateTime AvailableAt { get; set; }

        public DateTime? LockedAt { get; set; }

        public DateTime? ProcessedAt { get; set; }

        public string? Error { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
