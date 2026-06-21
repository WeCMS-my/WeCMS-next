using WeCms.Data.SqlSugar;

namespace WeCms.Tests.Unit.Persistence;

public sealed class SqlSugarTransactionContextTests
{
    [Fact]
    public async Task SqlSugarTransactionContext_CommitsOnce()
    {
        var commitCount = 0;
        var rollbackCount = 0;
        var transaction = new SqlSugarTransactionContext(
            _ =>
            {
                commitCount++;
                return ValueTask.CompletedTask;
            },
            _ =>
            {
                rollbackCount++;
                return ValueTask.CompletedTask;
            });

        await transaction.CommitAsync(TestContext.Current.CancellationToken);
        await transaction.CommitAsync(TestContext.Current.CancellationToken);
        await transaction.DisposeAsync();

        Assert.Equal(1, commitCount);
        Assert.Equal(0, rollbackCount);
    }

    [Fact]
    public async Task SqlSugarTransactionContext_RollsBackOnDispose_WhenNotCommitted()
    {
        var rollbackCount = 0;
        var transaction = new SqlSugarTransactionContext(
            _ => ValueTask.CompletedTask,
            _ =>
            {
                rollbackCount++;
                return ValueTask.CompletedTask;
            });

        await transaction.DisposeAsync();

        Assert.Equal(1, rollbackCount);
    }

    [Fact]
    public async Task SqlSugarUnitOfWork_BeginsTransaction()
    {
        var beginCount = 0;
        var transaction = new SqlSugarTransactionContext(
            _ => ValueTask.CompletedTask,
            _ => ValueTask.CompletedTask);
        var unitOfWork = new SqlSugarUnitOfWork(
            _ =>
            {
                beginCount++;
                return ValueTask.CompletedTask;
            },
            _ => transaction);

        var result = await unitOfWork.BeginTransactionAsync(TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(1, beginCount);
    }

    [Fact]
    public async Task SqlSugarUnitOfWork_UsesSingleConnectionPerScope()
    {
        var beginCount = 0;
        var transaction = new SqlSugarTransactionContext(
            _ => ValueTask.CompletedTask,
            _ => ValueTask.CompletedTask);
        var unitOfWork = new SqlSugarUnitOfWork(
            _ =>
            {
                beginCount++;
                return ValueTask.CompletedTask;
            },
            _ => transaction);

        var first = await unitOfWork.BeginTransactionAsync(TestContext.Current.CancellationToken);
        var second = await unitOfWork.BeginTransactionAsync(TestContext.Current.CancellationToken);

        Assert.Same(first, second);
        Assert.Equal(1, beginCount);
    }

    [Fact]
    public async Task SqlSugarUnitOfWork_StartsNewTransactionAfterCompletion()
    {
        var beginCount = 0;
        var unitOfWork = new SqlSugarUnitOfWork(
            _ =>
            {
                beginCount++;
                return ValueTask.CompletedTask;
            },
            _ => new SqlSugarTransactionContext(
                _ => ValueTask.CompletedTask,
                _ => ValueTask.CompletedTask));

        var first = await unitOfWork.BeginTransactionAsync(TestContext.Current.CancellationToken);
        await first.CommitAsync(TestContext.Current.CancellationToken);
        var second = await unitOfWork.BeginTransactionAsync(TestContext.Current.CancellationToken);

        Assert.NotSame(first, second);
        Assert.Equal(2, beginCount);
    }

    [Fact]
    public async Task SqlSugarUnitOfWork_DoesNotUseDistributedTransactions()
    {
        var source = await File.ReadAllTextAsync(
            RepoPath("backend", "src", "WeCms.Data.SqlSugar", "Transactions", "SqlSugarUnitOfWork.cs"),
            TestContext.Current.CancellationToken);

        Assert.DoesNotContain("TransactionScope", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Distributed", source, StringComparison.Ordinal);
        Assert.DoesNotContain("Create(", source, StringComparison.Ordinal);
        Assert.DoesNotContain("ChangeDatabase", source, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SqlSugarTransactionContext_RollsBackOnDispose_WhenCommitThrows()
    {
        var rollbackCount = 0;
        var transaction = new SqlSugarTransactionContext(
            _ => throw new InvalidOperationException("commit failed"),
            _ =>
            {
                rollbackCount++;
                return ValueTask.CompletedTask;
            });

        await Assert.ThrowsAsync<InvalidOperationException>(() => transaction.CommitAsync(TestContext.Current.CancellationToken));
        await transaction.DisposeAsync();

        Assert.Equal(1, rollbackCount);
    }

    private static string RepoPath(params string[] segments)
    {
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "..", segments.Aggregate(Path.Combine)));
    }
}
