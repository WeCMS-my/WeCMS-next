namespace WeCms.Shared;

public sealed record PagedResult<T>(
    IReadOnlyList<T> Records,
    int Page,
    int PageSize,
    long Total);
