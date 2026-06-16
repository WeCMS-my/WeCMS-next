namespace WeCms.Shared.Data;

public interface IUnitOfWork
{
    Task<ITransactionContext> BeginTransactionAsync(CancellationToken cancellationToken = default);
}
