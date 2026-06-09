 namespace WeCms.Modules.System.Dicts;
 
 public sealed record DictTypeItem(long Id, string Code, string Name, string Status);
 public sealed record DictValueItem(long Id, long TypeId, string Code, string Name, string? Value, int Sort, string Status);
 public sealed record CreateDictTypeRequest(string Code, string Name);
 public sealed record CreateDictValueRequest(long TypeId, string Code, string Name, string? Value, int Sort = 0);
