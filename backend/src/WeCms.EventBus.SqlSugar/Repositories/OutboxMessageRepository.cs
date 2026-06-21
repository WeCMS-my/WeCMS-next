using SqlSugar;
using WeCms.EventBus;

namespace WeCms.EventBus.SqlSugar.Repositories;

public sealed class OutboxMessageRepository : IOutboxMessageRepository
{
    private const int MaximumBatchSize = 100;
    private readonly ISqlSugarClient _db;

    public OutboxMessageRepository(ISqlSugarClient db)
    {
        _db = db;
    }

    public async Task WriteAsync(OutboxMessageWriteRecord record, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var affectedRows = await _db.Ado.ExecuteCommandAsync(
            """
            INSERT INTO sys_outbox_message
              (event_id, event_type, aggregate_type, aggregate_id, payload_json, status, retry_count, available_at, locked_at, processed_at, error, created_at)
            VALUES
              (@eventId, @eventType, @aggregateType, @aggregateId, @payloadJson, @status, 0, @availableAt, NULL, NULL, NULL, @createdAt)
            """,
            new SugarParameter("@eventId", record.EventId.ToString("D")),
            new SugarParameter("@eventType", record.EventType),
            new SugarParameter("@aggregateType", record.AggregateType),
            new SugarParameter("@aggregateId", record.AggregateId),
            new SugarParameter("@payloadJson", record.PayloadJson),
            new SugarParameter("@status", OutboxMessageStatus.Pending),
            new SugarParameter("@availableAt", record.AvailableAt.UtcDateTime),
            new SugarParameter("@createdAt", record.CreatedAt.UtcDateTime));

        if (affectedRows != 1)
        {
            throw new InvalidOperationException($"Expected to write one outbox message row, affected {affectedRows}.");
        }
    }

    public async Task<IReadOnlyList<OutboxMessageRecord>> LockPendingMessagesAsync(
        int batchSize,
        DateTimeOffset now,
        string lockToken,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(lockToken))
        {
            throw new ArgumentException("Outbox lock token must not be empty.", nameof(lockToken));
        }

        if (batchSize < 1 || batchSize > MaximumBatchSize)
        {
            throw new ArgumentOutOfRangeException(nameof(batchSize), $"Outbox batch size must be between 1 and {MaximumBatchSize}.");
        }

        var candidateRows = await _db.Ado.SqlQueryAsync<OutboxMessageIdRow>(
            """
            SELECT id AS Id
            FROM sys_outbox_message
            WHERE status IN (@pendingStatus, @failedStatus)
              AND available_at <= @now
            ORDER BY available_at ASC, id ASC
            LIMIT @batchSize
            """,
            new SugarParameter("@pendingStatus", OutboxMessageStatus.Pending),
            new SugarParameter("@failedStatus", OutboxMessageStatus.Failed),
            new SugarParameter("@now", now.UtcDateTime),
            new SugarParameter("@batchSize", batchSize));

        var ids = candidateRows.Select(static row => row.Id).ToArray();
        if (ids.Length == 0)
        {
            return [];
        }

        var parameters = new List<SugarParameter>
        {
            new("@processingStatus", OutboxMessageStatus.Processing),
            new("@pendingStatus", OutboxMessageStatus.Pending),
            new("@failedStatus", OutboxMessageStatus.Failed),
            new("@lockedAt", now.UtcDateTime),
            new("@lockToken", lockToken)
        };
        var placeholders = new List<string>(ids.Length);
        for (var index = 0; index < ids.Length; index++)
        {
            var name = $"@id{index}";
            placeholders.Add(name);
            parameters.Add(new SugarParameter(name, ids[index]));
        }

        await _db.Ado.ExecuteCommandAsync(
            $"""
            UPDATE sys_outbox_message
            SET status = @processingStatus,
                locked_at = @lockedAt,
                lock_token = @lockToken
            WHERE id IN ({string.Join(", ", placeholders)})
              AND status IN (@pendingStatus, @failedStatus)
              AND available_at <= @lockedAt
            """,
            parameters);

        return await LoadByLockTokenAsync(lockToken, cancellationToken);
    }

    public async Task MarkProcessedAsync(long id, DateTimeOffset processedAt, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var affectedRows = await _db.Ado.ExecuteCommandAsync(
            """
            UPDATE sys_outbox_message
            SET status = @processedStatus,
                processed_at = @processedAt,
                lock_token = NULL,
                error = NULL
            WHERE id = @id
              AND status = @processingStatus
            """,
            new SugarParameter("@id", id),
            new SugarParameter("@processedStatus", OutboxMessageStatus.Processed),
            new SugarParameter("@processedAt", processedAt.UtcDateTime),
            new SugarParameter("@processingStatus", OutboxMessageStatus.Processing));

        if (affectedRows != 1)
        {
            throw new InvalidOperationException($"Expected to mark one outbox message processed, affected {affectedRows}.");
        }
    }

    public async Task MarkFailedAsync(long id, string error, DateTimeOffset nextAvailableAt, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(error))
        {
            throw new ArgumentException("Outbox error must not be empty.", nameof(error));
        }

        var affectedRows = await _db.Ado.ExecuteCommandAsync(
            """
            UPDATE sys_outbox_message
            SET status = @failedStatus,
                retry_count = retry_count + 1,
                available_at = @availableAt,
                lock_token = NULL,
                error = @error
            WHERE id = @id
              AND status = @processingStatus
            """,
            new SugarParameter("@id", id),
            new SugarParameter("@failedStatus", OutboxMessageStatus.Failed),
            new SugarParameter("@availableAt", nextAvailableAt.UtcDateTime),
            new SugarParameter("@error", error),
            new SugarParameter("@processingStatus", OutboxMessageStatus.Processing));

        if (affectedRows != 1)
        {
            throw new InvalidOperationException($"Expected to mark one outbox message failed, affected {affectedRows}.");
        }
    }

    private async Task<IReadOnlyList<OutboxMessageRecord>> LoadByLockTokenAsync(
        string lockToken,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var rows = await _db.Ado.SqlQueryAsync<OutboxMessageRow>(
            """
            SELECT id AS Id,
                   event_id AS EventId,
                   event_type AS EventType,
                   aggregate_type AS AggregateType,
                   aggregate_id AS AggregateId,
                   payload_json AS PayloadJson,
                   status AS Status,
                   retry_count AS RetryCount,
                   available_at AS AvailableAt,
                   locked_at AS LockedAt,
                   processed_at AS ProcessedAt,
                   error AS Error,
                   created_at AS CreatedAt
            FROM sys_outbox_message
            WHERE lock_token = @lockToken
              AND status = @processingStatus
            ORDER BY available_at ASC, id ASC
            """,
            new SugarParameter("@lockToken", lockToken),
            new SugarParameter("@processingStatus", OutboxMessageStatus.Processing));

        return rows.Select(row => row.ToRecord(lockToken)).ToArray();
    }

    private sealed class OutboxMessageIdRow
    {
        public long Id { get; set; }
    }

    private sealed class OutboxMessageRow
    {
        public long Id { get; set; }

        public string EventId { get; set; } = string.Empty;

        public string EventType { get; set; } = string.Empty;

        public string? AggregateType { get; set; }

        public string? AggregateId { get; set; }

        public string PayloadJson { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;

        public int RetryCount { get; set; }

        public DateTime AvailableAt { get; set; }

        public DateTime? LockedAt { get; set; }

        public DateTime? ProcessedAt { get; set; }

        public string? Error { get; set; }

        public DateTime CreatedAt { get; set; }

        public OutboxMessageRecord ToRecord(string lockToken)
        {
            return new OutboxMessageRecord(
                Id,
                Guid.Parse(EventId),
                EventType,
                AggregateType,
                AggregateId,
                PayloadJson,
                Status,
                RetryCount,
                ToUtc(AvailableAt),
                LockedAt is null ? null : ToUtc(LockedAt.Value),
                lockToken,
                ProcessedAt is null ? null : ToUtc(ProcessedAt.Value),
                Error,
                ToUtc(CreatedAt));
        }

        private static DateTimeOffset ToUtc(DateTime value)
        {
            return new DateTimeOffset(DateTime.SpecifyKind(value, DateTimeKind.Utc));
        }
    }
}
