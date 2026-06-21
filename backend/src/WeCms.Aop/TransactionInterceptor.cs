using WeCms.Shared.Data;

namespace WeCms.Aop;

public sealed class TransactionInterceptor
{
    private readonly AsyncLocal<int> _transactionDepth = new();
    private readonly IUnitOfWork _unitOfWork;

    public TransactionInterceptor(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task InvokeAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        if (_transactionDepth.Value > 0)
        {
            await operation(cancellationToken);
            return;
        }

        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        var previousDepth = _transactionDepth.Value;
        _transactionDepth.Value = previousDepth + 1;

        try
        {
            await operation(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
            _transactionDepth.Value = previousDepth;
        }
    }

    public async Task<TResult> InvokeAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operation);

        if (_transactionDepth.Value > 0)
        {
            return await operation(cancellationToken);
        }

        await using var transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
        var previousDepth = _transactionDepth.Value;
        _transactionDepth.Value = previousDepth + 1;

        try
        {
            var result = await operation(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
        finally
        {
            _transactionDepth.Value = previousDepth;
        }
    }
}
