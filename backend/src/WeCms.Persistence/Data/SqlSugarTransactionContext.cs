using SqlSugar;
using WeCms.Shared.Data;

namespace WeCms.Persistence.Data;

public sealed class SqlSugarTransactionContext : ITransactionContext
{
    private readonly Action _commit;
    private readonly Action _rollback;
    private bool _terminalAttempted;

    public SqlSugarTransactionContext(ISqlSugarClient db)
        : this(db.Ado.CommitTran, db.Ado.RollbackTran)
    {
    }

    internal SqlSugarTransactionContext(Action commit, Action rollback)
    {
        _commit = commit;
        _rollback = rollback;
    }

    public Task CommitAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _terminalAttempted = true;
        _commit();

        return Task.CompletedTask;
    }

    public Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _terminalAttempted = true;
        _rollback();

        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        if (!_terminalAttempted)
        {
            _terminalAttempted = true;
            _rollback();
        }

        return ValueTask.CompletedTask;
    }
}
