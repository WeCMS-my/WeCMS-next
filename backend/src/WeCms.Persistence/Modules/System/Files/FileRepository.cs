using SqlSugar;
using WeCms.Modules.System.Files;
using WeCms.Shared;

namespace WeCms.Persistence.Modules.System.Files;

public sealed class FileRepository : IFileRepository
{
    private readonly ISqlSugarClient _db;

    public FileRepository(ISqlSugarClient db) => _db = db;

    public async Task<PagedResult<FileSummaryDto>> ListAsync(FileListCriteria criteria, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var where = "WHERE deleted_at IS NULL";
        var parameters = new List<SugarParameter>();
        if (!string.IsNullOrWhiteSpace(criteria.Keyword))
        {
            where += " AND original_name LIKE @keyword";
            parameters.Add(new SugarParameter("@keyword", $"%{criteria.Keyword}%"));
        }

        if (!string.IsNullOrWhiteSpace(criteria.MimeType))
        {
            where += " AND mime_type = @mimeType";
            parameters.Add(new SugarParameter("@mimeType", criteria.MimeType));
        }

        if (!string.IsNullOrWhiteSpace(criteria.Status))
        {
            where += " AND status = @status";
            parameters.Add(new SugarParameter("@status", criteria.Status));
        }

        var total = Convert.ToInt64(await _db.Ado.GetScalarAsync($"SELECT COUNT(1) FROM sys_file {where}", parameters), global::System.Globalization.CultureInfo.InvariantCulture);
        var offset = (criteria.Page - 1) * criteria.PageSize;
        parameters.Add(new SugarParameter("@offset", offset));
        parameters.Add(new SugarParameter("@pageSize", criteria.PageSize));
        var rows = await _db.Ado.SqlQueryAsync<FileRow>(
            $"""
            SELECT id AS Id,
                   original_name AS OriginalName,
                   file_ext AS FileExt,
                   mime_type AS MimeType,
                   size_bytes AS SizeBytes,
                   sha256 AS Sha256,
                   status AS Status,
                   created_by AS CreatedBy,
                   created_at AS CreatedAt
            FROM sys_file
            {where}
            ORDER BY created_at DESC, id DESC
            LIMIT @pageSize OFFSET @offset
            """,
            parameters);

        return new PagedResult<FileSummaryDto>(rows.Select(row => row.ToSummaryDto()).ToArray(), criteria.Page, criteria.PageSize, total);
    }

    public async Task<FileDetailDto?> GetAsync(long id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var row = await _db.Ado.SqlQuerySingleAsync<FileRow>(
            """
            SELECT id AS Id,
                   original_name AS OriginalName,
                   file_ext AS FileExt,
                   mime_type AS MimeType,
                   size_bytes AS SizeBytes,
                   sha256 AS Sha256,
                   status AS Status,
                   created_by AS CreatedBy,
                   created_at AS CreatedAt
            FROM sys_file
            WHERE id = @id
              AND deleted_at IS NULL
            LIMIT 1
            """,
            new SugarParameter("@id", id));

        return row?.ToDetailDto();
    }

    public async Task<long> CreateAsync(FileCreateRecord record, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _db.Ado.ExecuteCommandAsync(
            """
            INSERT INTO sys_file (storage_provider, bucket, object_key, original_name, file_ext, mime_type, size_bytes, sha256, status, created_by, created_at, deleted_at)
            VALUES (@storageProvider, @bucket, @objectKey, @originalName, @fileExt, @mimeType, @sizeBytes, @sha256, @status, @createdBy, @createdAt, NULL)
            """,
            new SugarParameter("@storageProvider", record.StorageProvider),
            new SugarParameter("@bucket", record.Bucket),
            new SugarParameter("@objectKey", record.ObjectKey),
            new SugarParameter("@originalName", record.OriginalName),
            new SugarParameter("@fileExt", record.FileExt),
            new SugarParameter("@mimeType", record.MimeType),
            new SugarParameter("@sizeBytes", record.SizeBytes),
            new SugarParameter("@sha256", record.Sha256),
            new SugarParameter("@status", record.Status),
            new SugarParameter("@createdBy", record.CreatedBy),
            new SugarParameter("@createdAt", record.Now.UtcDateTime));

        return Convert.ToInt64(await _db.Ado.GetScalarAsync("SELECT LAST_INSERT_ID()"), global::System.Globalization.CultureInfo.InvariantCulture);
    }

    public Task SoftDeleteAsync(long id, DateTimeOffset now, CancellationToken cancellationToken) => ExpectOneAsync(
        """
        UPDATE sys_file
        SET deleted_at = @deletedAt,
            status = 'deleted'
        WHERE id = @id
          AND deleted_at IS NULL
        """,
        cancellationToken,
        new SugarParameter("@deletedAt", now.UtcDateTime),
        new SugarParameter("@id", id));

    public Task RecordAuditAsync(FileAuditRecord record, CancellationToken cancellationToken) => ExpectOneAsync(
        """
        INSERT INTO sys_audit_log (user_id, username, module, resource, action, target_id, request_method, request_path, ip_address, user_agent, trace_id, result, detail, created_at)
        VALUES (@userId, @username, 'system', 'file', @action, @targetId, @requestMethod, @requestPath, @ipAddress, @userAgent, @traceId, @result, @detail, @createdAt)
        """,
        cancellationToken,
        new SugarParameter("@userId", record.ActorUserId),
        new SugarParameter("@username", record.ActorUsername),
        new SugarParameter("@action", record.Action),
        new SugarParameter("@targetId", record.TargetFileId.ToString(global::System.Globalization.CultureInfo.InvariantCulture)),
        new SugarParameter("@requestMethod", string.Empty),
        new SugarParameter("@requestPath", "/api/v1/system/files"),
        new SugarParameter("@ipAddress", record.Ip),
        new SugarParameter("@userAgent", record.UserAgent),
        new SugarParameter("@traceId", record.TraceId),
        new SugarParameter("@result", record.Result),
        new SugarParameter("@detail", record.Detail),
        new SugarParameter("@createdAt", record.Now.UtcDateTime));

    private async Task ExpectOneAsync(string sql, CancellationToken cancellationToken, params SugarParameter[] parameters)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var rows = await _db.Ado.ExecuteCommandAsync(sql, parameters);
        if (rows != 1)
        {
            throw new InvalidOperationException($"Expected one affected row, got {rows}.");
        }
    }

    private sealed class FileRow
    {
        public long Id { get; set; }
        public string OriginalName { get; set; } = string.Empty;
        public string FileExt { get; set; } = string.Empty;
        public string MimeType { get; set; } = string.Empty;
        public long SizeBytes { get; set; }
        public string Sha256 { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public long CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; }

        public FileSummaryDto ToSummaryDto() => new(Id, OriginalName, FileExt, MimeType, SizeBytes, Sha256, Status, CreatedBy, ToOffset(CreatedAt));
        public FileDetailDto ToDetailDto() => new(Id, OriginalName, FileExt, MimeType, SizeBytes, Sha256, Status, CreatedBy, ToOffset(CreatedAt));
        private static DateTimeOffset ToOffset(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));
    }
}
