 namespace WeCms.Shared;
 
 public sealed record ApiResult<T>(int Code, string Msg, T? Data)
 {
     public static ApiResult<T> Ok(T data) => new(0, "success", data);
     
     public static ApiResult<T> Fail(int code, string msg) => new(code, msg, default);
 }
