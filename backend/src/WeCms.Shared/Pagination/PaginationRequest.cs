namespace WeCms.Shared.Pagination;

public sealed record PaginationRequest(int Page = 1, int PageSize = 20)
{
    public static readonly PaginationRequest FirstPage = new(1, 20);
}
