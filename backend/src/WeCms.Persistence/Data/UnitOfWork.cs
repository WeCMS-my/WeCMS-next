using System.Data.Common;
using WeCms.Shared.Data;

namespace WeCms.Persistence.Data;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly IDbConnectionFactory _connectionFactory;
    private DbConnection? _connection;
    private DbTransaction? _transaction;
    private bool _transactionCompleted;
    private bool _disposed;

    public IDbTransactionFacade Transaction
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_transaction is null)
                throw new InvalidOperationException(_transactionCompleted
                    ? "UnitOfWork transaction is no longer active."
                    : "UnitOfWork transaction has not been started.");

            if (_connection is null)
                throw new InvalidOperationException("UnitOfWork transaction is no longer active.");

            return new DbTransactionFacade(_connection, _transaction);
        }
    }

    public UnitOfWork(IDbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task BeginAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_transaction is not null)
            throw new InvalidOperationException("UnitOfWork transaction is already active.");

        if (_connection is not null)
            throw new InvalidOperationException("UnitOfWork transaction is no longer active.");

        _connection = await _connectionFactory.OpenAsync(cancellationToken);
        _transaction = await _connection.BeginTransactionAsync(cancellationToken);
        _transactionCompleted = false;
    }

    public async Task CommitAsync(CancellationToken cancellationToken)
    {
        var transaction = GetActiveTransaction();
        await transaction.CommitAsync(cancellationToken);
        await ReleaseTransactionAsync();
    }

    public async Task RollbackAsync(CancellationToken cancellationToken)
    {
        var transaction = GetActiveTransaction();
        await transaction.RollbackAsync(cancellationToken);
        await ReleaseTransactionAsync();
    }

    private DbTransaction GetActiveTransaction()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_transaction is null)
            throw new InvalidOperationException(_transactionCompleted
                ? "UnitOfWork transaction is no longer active."
                : "UnitOfWork transaction has not been started.");

        return _transaction;
    }

    private async ValueTask ReleaseTransactionAsync()
    {
        var transaction = _transaction;
        var connection = _connection;

        _transaction = null;
        _connection = null;
        _transactionCompleted = true;

        if (transaction is not null)
            await transaction.DisposeAsync();

        if (connection is not null)
            await connection.DisposeAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        if (_transaction is not null)
        {
            await _transaction.RollbackAsync(CancellationToken.None);
            await _transaction.DisposeAsync();
        }

        _transaction = null;

        if (_connection is not null)
            await _connection.DisposeAsync();

        _connection = null;
    }
}
