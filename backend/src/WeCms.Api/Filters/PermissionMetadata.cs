 namespace WeCms.Api.Filters;
 
 public sealed class PermissionMetadata(string code)
 {
     public string Code { get; } = code;
 }
