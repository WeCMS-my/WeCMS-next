using SqlSugar;
using WeCms.Shared.Data;

namespace WeCms.Persistence.Data;

public sealed class SqlSugarUnitOfWork : IUnitOfWork
{
    public SqlSugarUnitOfWork(ISqlSugarClientFactory clientFactory)
    {
        Client = clientFactory.CreateClient();
    }

    public ISqlSugarClient Client { get; }

    public Task BeginAsync(CancellationToken cancellationToken)
        => Client.Ado.BeginTranAsync();

    public Task CommitAsync(CancellationToken cancellationToken)
        => Client.Ado.CommitTranAsync();

    public Task RollbackAsync(CancellationToken cancellationToken)
        => Client.Ado.RollbackTranAsync();

    public void Dispose()
    {
        if (Client is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}
