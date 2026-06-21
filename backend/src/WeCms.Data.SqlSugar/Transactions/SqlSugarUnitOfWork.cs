using SqlSugar;
using WeCms.Shared.Data;

namespace WeCms.Data.SqlSugar;

public sealed class SqlSugarUnitOfWork : IUnitOfWork
{
    private readonly Func<CancellationToken, ValueTask> _beginTransaction;
    private readonly Func<CancellationToken, ITransactionContext> _createTransaction;
    private ITransactionContext? _currentTransaction;

    public SqlSugarUnitOfWork(ISqlSugarClient db)
        : this(
            _ => new ValueTask(db.Ado.BeginTranAsync()),
            _ => new SqlSugarTransactionContext(db))
    {
        ArgumentNullException.ThrowIfNull(db);
    }

    internal SqlSugarUnitOfWork(
        Func<CancellationToken, ValueTask> beginTransaction,
        Func<CancellationToken, ITransactionContext> createTransaction)
    {
        _beginTransaction = beginTransaction;
        _createTransaction = createTransaction;
    }

    public async Task<ITransactionContext> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (_currentTransaction is not null)
        {
            return _currentTransaction;
        }

        await _beginTransaction(cancellationToken);

        _currentTransaction = new UnitOfWorkTransactionContext(
            _createTransaction(cancellationToken),
            () => _currentTransaction = null);
        return _currentTransaction;
    }

    private sealed class UnitOfWorkTransactionContext : ITransactionContext
    {
        private readonly ITransactionContext _inner;
        private readonly Action _clear;
        private bool _cleared;

        public UnitOfWorkTransactionContext(ITransactionContext inner, Action clear)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
            _clear = clear ?? throw new ArgumentNullException(nameof(clear));
        }

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await _inner.CommitAsync(cancellationToken);
            }
            finally
            {
                Clear();
            }
        }

        public async Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                await _inner.RollbackAsync(cancellationToken);
            }
            finally
            {
                Clear();
            }
        }

        public async ValueTask DisposeAsync()
        {
            try
            {
                await _inner.DisposeAsync();
            }
            finally
            {
                Clear();
            }
        }

        private void Clear()
        {
            if (_cleared)
            {
                return;
            }

            _cleared = true;
            _clear();
        }
    }
}
