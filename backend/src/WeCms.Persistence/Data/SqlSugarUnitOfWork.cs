using SqlSugar;
using WeCms.Shared.Data;

namespace WeCms.Persistence.Data;

public sealed class SqlSugarUnitOfWork : IUnitOfWork
{
    private readonly ISqlSugarClient _db;

    public SqlSugarUnitOfWork(ISqlSugarClient db)
    {
        _db = db;
    }

    public Task<ITransactionContext> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _db.Ado.BeginTran();

        return Task.FromResult<ITransactionContext>(new SqlSugarTransactionContext(_db));
    }
}
