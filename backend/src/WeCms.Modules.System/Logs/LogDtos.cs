 namespace WeCms.Modules.System.Logs;
 
 public sealed record LoginLogItem(long Id, long? UserId, string Username, string Status, string? Ip, DateTime CreatedAt);
 public sealed record AuditLogItem(long Id, long? UserId, string? Username, string Module, string Action, string? Ip, DateTime CreatedAt);
