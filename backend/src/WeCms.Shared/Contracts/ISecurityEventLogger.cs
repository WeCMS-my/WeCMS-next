 namespace WeCms.Shared.Contracts;
 
 public interface ISecurityEventLogger
 {
     Task LogAsync(string eventType, string severity, long? userId, string? username, string? ip, string? detail, CancellationToken ct);
 }
