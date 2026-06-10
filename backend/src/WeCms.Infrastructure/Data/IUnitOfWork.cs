namespace WeCms.Infrastructure.Data;

public interface IUnitOfWork : IAsyncDisposable
{
    IDbTransactionFacade Transaction { get; }

    Task BeginAsync(CancellationToken cancellationToken);
    Task CommitAsync(CancellationToken cancellationToken);
    Task RollbackAsync(CancellationToken cancellationToken);
}
