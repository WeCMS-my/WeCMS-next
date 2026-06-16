namespace WeCms.Shared;

public sealed record ApiResult<T>(
    int Code,
    string Msg,
    T? Data,
    string? TraceId = null,
    IReadOnlyDictionary<string, string[]>? FieldErrors = null)
{
    public static ApiResult<T> Ok(T? data)
    {
        return new ApiResult<T>(ApiCodes.Success, "success", data);
    }

    public static ApiResult<T> Error(
        int code,
        string msg,
        string traceId,
        IReadOnlyDictionary<string, string[]>? fieldErrors = null)
    {
        return new ApiResult<T>(code, msg, default, traceId, fieldErrors);
    }
}
