using SqlSugar;
using WeCms.Shared.Data;

namespace WeCms.Data.SqlSugar;

public sealed class SqlSugarTransactionContext : ITransactionContext
{
    private readonly Func<CancellationToken, ValueTask> _commit;
    private readonly Func<CancellationToken, ValueTask> _rollback;
    private bool _completed;

    public SqlSugarTransactionContext(ISqlSugarClient db)
        : this(
            _ => new ValueTask(db.Ado.CommitTranAsync()),
            _ => new ValueTask(db.Ado.RollbackTranAsync()))
    {
        ArgumentNullException.ThrowIfNull(db);
    }

    internal SqlSugarTransactionContext(
        Func<CancellationToken, ValueTask> commit,
        Func<CancellationToken, ValueTask> rollback)
    {
        _commit = commit;
        _rollback = rollback;
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_completed)
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            await _commit(cancellationToken);
            _completed = true;
        }
        catch
        {
            if (!_completed)
            {
                _completed = true;
                await _rollback(CancellationToken.None);
            }

            throw;
        }
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_completed)
        {
            return;
        }

        cancellationToken.ThrowIfCancellationRequested();
        _completed = true;
        await _rollback(cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        if (_completed)
        {
            return;
        }

        _completed = true;
        await _rollback(CancellationToken.None);
    }
}
