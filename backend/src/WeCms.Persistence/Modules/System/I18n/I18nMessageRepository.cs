using SqlSugar;
using WeCms.Modules.System.I18n;
using WeCms.Shared;

namespace WeCms.Persistence.Modules.System.I18n;

public sealed class I18nMessageRepository : II18nMessageRepository
{
    private readonly ISqlSugarClient _db;

    public I18nMessageRepository(ISqlSugarClient db)
    {
        _db = db;
    }

    public async Task<PagedResult<I18nMessageSummaryDto>> ListAsync(I18nMessageListCriteria criteria, CancellationToken cancellationToken)
    {
        var clauses = new List<string> { "deleted_at IS NULL" };
        var parameters = new List<SugarParameter>();

        if (!string.IsNullOrWhiteSpace(criteria.Locale))
        {
            clauses.Add("locale = @locale");
            parameters.Add(new SugarParameter("@locale", criteria.Locale));
        }

        if (!string.IsNullOrWhiteSpace(criteria.Module))
        {
            clauses.Add("module = @module");
            parameters.Add(new SugarParameter("@module", criteria.Module));
        }

        if (!string.IsNullOrWhiteSpace(criteria.Status))
        {
            clauses.Add("status = @status");
            parameters.Add(new SugarParameter("@status", criteria.Status));
        }

        if (!string.IsNullOrWhiteSpace(criteria.Keyword))
        {
            clauses.Add("(message_key LIKE @keyword OR message_value LIKE @keyword)");
            parameters.Add(new SugarParameter("@keyword", $"%{criteria.Keyword}%"));
        }

        var where = string.Join(" AND ", clauses);
        var total = Convert.ToInt64(await _db.Ado.GetScalarAsync($"SELECT COUNT(1) FROM sys_i18n_message WHERE {where}", parameters), global::System.Globalization.CultureInfo.InvariantCulture);
        var offset = (criteria.Page - 1) * criteria.PageSize;
        parameters.Add(new SugarParameter("@offset", offset));
        parameters.Add(new SugarParameter("@pageSize", criteria.PageSize));

        var rows = await _db.Ado.SqlQueryAsync<I18nMessageRow>($"""
            SELECT id AS Id,
                   locale AS Locale,
                   module AS Module,
                   message_key AS MessageKey,
                   message_value AS MessageValue,
                   remark AS Remark,
                   status AS Status,
                   created_at AS CreatedAt,
                   updated_at AS UpdatedAt
            FROM sys_i18n_message
            WHERE {where}
            ORDER BY locale, module, message_key, id
            LIMIT @offset, @pageSize
            """, parameters);

        return new PagedResult<I18nMessageSummaryDto>(rows.Select(row => row.ToSummary()).ToArray(), criteria.Page, criteria.PageSize, total);
    }

    public async Task<I18nMessageDetailDto?> GetAsync(long id, CancellationToken cancellationToken)
    {
        var rows = await _db.Ado.SqlQueryAsync<I18nMessageRow>("""
            SELECT id AS Id,
                   locale AS Locale,
                   module AS Module,
                   message_key AS MessageKey,
                   message_value AS MessageValue,
                   remark AS Remark,
                   status AS Status,
                   created_at AS CreatedAt,
                   updated_at AS UpdatedAt
            FROM sys_i18n_message
            WHERE id = @id AND deleted_at IS NULL
            LIMIT 1
            """, new SugarParameter("@id", id));

        return rows.FirstOrDefault()?.ToDetail();
    }

    public async Task<bool> ExistsAsync(string locale, string messageKey, long? exceptId, CancellationToken cancellationToken)
    {
        var clauses = new List<string> { "locale = @locale", "message_key = @messageKey", "deleted_at IS NULL" };
        var parameters = new List<SugarParameter>
        {
            new("@locale", locale),
            new("@messageKey", messageKey)
        };

        if (exceptId is not null)
        {
            clauses.Add("id <> @exceptId");
            parameters.Add(new SugarParameter("@exceptId", exceptId.Value));
        }

        var count = Convert.ToInt64(await _db.Ado.GetScalarAsync($"SELECT COUNT(1) FROM sys_i18n_message WHERE {string.Join(" AND ", clauses)}", parameters), global::System.Globalization.CultureInfo.InvariantCulture);
        return count > 0;
    }

    public async Task<long> CreateAsync(I18nMessageCreateRecord record, CancellationToken cancellationToken)
    {
        var id = Convert.ToInt64(await _db.Ado.GetScalarAsync("""
            INSERT INTO sys_i18n_message (locale, module, message_key, message_value, remark, status, created_at, updated_at, deleted_at)
            VALUES (@locale, @module, @messageKey, @messageValue, @remark, @status, @createdAt, @createdAt, NULL);
            SELECT LAST_INSERT_ID();
            """,
            new SugarParameter("@locale", record.Locale),
            new SugarParameter("@module", record.Module),
            new SugarParameter("@messageKey", record.MessageKey),
            new SugarParameter("@messageValue", record.MessageValue),
            new SugarParameter("@remark", record.Remark),
            new SugarParameter("@status", record.Status),
            new SugarParameter("@createdAt", record.Now.UtcDateTime)), global::System.Globalization.CultureInfo.InvariantCulture);

        return id;
    }

    public async Task UpdateAsync(I18nMessageUpdateRecord record, CancellationToken cancellationToken)
    {
        var affectedRows = await _db.Ado.ExecuteCommandAsync("""
            UPDATE sys_i18n_message
            SET module = @module,
                message_value = @messageValue,
                remark = @remark,
                status = @status,
                updated_at = @updatedAt
            WHERE id = @id AND deleted_at IS NULL
            """,
            new SugarParameter("@id", record.Id),
            new SugarParameter("@module", record.Module),
            new SugarParameter("@messageValue", record.MessageValue),
            new SugarParameter("@remark", record.Remark),
            new SugarParameter("@status", record.Status),
            new SugarParameter("@updatedAt", record.Now.UtcDateTime));

        if (affectedRows != 1)
        {
            throw new InvalidOperationException($"Expected to update one i18n message row, affected {affectedRows}.");
        }
    }

    public async Task SoftDeleteAsync(long id, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var affectedRows = await _db.Ado.ExecuteCommandAsync("""
            UPDATE sys_i18n_message
            SET deleted_at = @deletedAt,
                updated_at = @deletedAt
            WHERE id = @id AND deleted_at IS NULL
            """,
            new SugarParameter("@id", id),
            new SugarParameter("@deletedAt", now.UtcDateTime));

        if (affectedRows != 1)
        {
            throw new InvalidOperationException($"Expected to delete one i18n message row, affected {affectedRows}.");
        }
    }

    public async Task<IReadOnlyList<I18nPublicMessageRecord>> ListPublicMessagesAsync(string locale, string status, CancellationToken cancellationToken)
    {
        var rows = await _db.Ado.SqlQueryAsync<I18nPublicMessageRow>("""
            SELECT message_key AS MessageKey,
                   message_value AS MessageValue
            FROM sys_i18n_message
            WHERE locale = @locale
              AND status = @status
              AND deleted_at IS NULL
            ORDER BY module, message_key, id
            """,
            new SugarParameter("@locale", locale),
            new SugarParameter("@status", status));

        return rows.Select(row => new I18nPublicMessageRecord(row.MessageKey, row.MessageValue)).ToArray();
    }

    public async Task RecordAuditAsync(I18nAuditRecord record, CancellationToken cancellationToken)
    {
        var insertedRows = await _db.Ado.ExecuteCommandAsync("""
            INSERT INTO sys_audit_log (user_id, username, module, resource, action, target_id, request_method, request_path, ip, user_agent, trace_id, result, detail, created_at)
            VALUES (@userId, @username, 'system', @resource, @action, @targetId, 'POST', @requestPath, @ip, @userAgent, @traceId, @result, @detail, @createdAt)
            """,
            new SugarParameter("@userId", record.ActorUserId),
            new SugarParameter("@username", record.ActorUsername),
            new SugarParameter("@resource", record.Resource),
            new SugarParameter("@action", record.Action),
            new SugarParameter("@targetId", record.TargetId),
            new SugarParameter("@requestPath", "/api/v1/system/i18n/messages"),
            new SugarParameter("@ip", record.Ip),
            new SugarParameter("@userAgent", record.UserAgent),
            new SugarParameter("@traceId", record.TraceId),
            new SugarParameter("@result", record.Result),
            new SugarParameter("@detail", record.Detail),
            new SugarParameter("@createdAt", record.CreatedAt.UtcDateTime));

        if (insertedRows != 1)
        {
            throw new InvalidOperationException($"Expected to insert one audit log row, inserted {insertedRows}.");
        }
    }

    private sealed class I18nMessageRow
    {
        public long Id { get; set; }
        public string Locale { get; set; } = string.Empty;
        public string Module { get; set; } = string.Empty;
        public string MessageKey { get; set; } = string.Empty;
        public string MessageValue { get; set; } = string.Empty;
        public string? Remark { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public I18nMessageSummaryDto ToSummary()
        {
            return new I18nMessageSummaryDto(Id, Locale, Module, MessageKey, MessageValue, Status, ToOffset(UpdatedAt));
        }

        public I18nMessageDetailDto ToDetail()
        {
            return new I18nMessageDetailDto(Id, Locale, Module, MessageKey, MessageValue, Remark, Status, ToOffset(CreatedAt), ToOffset(UpdatedAt));
        }
    }

    private sealed class I18nPublicMessageRow
    {
        public string MessageKey { get; set; } = string.Empty;
        public string MessageValue { get; set; } = string.Empty;
    }

    private static DateTimeOffset ToOffset(DateTime dateTime)
    {
        return new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc));
    }
}

