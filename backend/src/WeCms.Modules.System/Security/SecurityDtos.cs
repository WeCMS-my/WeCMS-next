 namespace WeCms.Modules.System.Security;
 
 public sealed record SecurityEventItem(long Id, string EventType, string Severity, long? UserId, string? Username, string? Ip, string? Detail, DateTime CreatedAt);
