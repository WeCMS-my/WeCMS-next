namespace WeCms.Modules.System.Files;

public sealed record FileRequestContext(long ActorUserId, string ActorUsername, string Ip, string UserAgent, string TraceId, DateTimeOffset Now);

public sealed record FileListCriteria(int Page, int PageSize, string? Keyword, string? MimeType, string? Status);

public sealed record FileCreateRecord(string StorageProvider, string Bucket, string ObjectKey, string OriginalName, string FileExt, string MimeType, long SizeBytes, string Sha256, string Status, long CreatedBy, DateTimeOffset Now);

public sealed record FileAuditRecord(long ActorUserId, string ActorUsername, string Action, long TargetFileId, string Ip, string UserAgent, string TraceId, string Result, string Detail, DateTimeOffset Now);

public sealed record FileDownloadRecord(string ObjectKey, string OriginalName, string MimeType, long SizeBytes, string Status);
