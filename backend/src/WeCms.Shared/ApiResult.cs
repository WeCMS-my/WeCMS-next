namespace WeCms.Shared;

public sealed record ApiResult<T>(
    int Code,
    string Msg,
    T? Data,
    string? TraceId = null,
    IReadOnlyDictionary<string, IReadOnlyList<string>>? FieldErrors = null)
{
    public static ApiResult<T> Ok(T data, string? traceId = null)
        => new(ApiCodes.Success, "success", data, traceId);

    public static ApiResult<T> Fail(
        int code,
        string msg,
        string? traceId = null,
        IReadOnlyDictionary<string, IReadOnlyList<string>>? fieldErrors = null)
        => new(code, msg, default, traceId, fieldErrors);
}
