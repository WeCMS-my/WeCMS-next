using WeCms.Persistence.Data;

namespace WeCms.Tests.Unit.Persistence;

public sealed class SqlSugarTransactionContextTests
{
    [Fact]
    public async Task DisposeAsync_RollsBackWhenNoTerminalActionWasAttempted()
    {
        var rollbackCount = 0;
        var transaction = new SqlSugarTransactionContext(() => { }, () => rollbackCount++);

        await transaction.DisposeAsync();

        Assert.Equal(1, rollbackCount);
    }

    [Fact]
    public async Task CommitAsync_WhenCommitThrows_DoesNotRollbackDuringDispose()
    {
        var rollbackCount = 0;
        var transaction = new SqlSugarTransactionContext(
            () => throw new InvalidOperationException("commit failed"),
            () => rollbackCount++);

        await Assert.ThrowsAsync<InvalidOperationException>(() => transaction.CommitAsync());
        await transaction.DisposeAsync();

        Assert.Equal(0, rollbackCount);
    }

    [Fact]
    public async Task RollbackAsync_WhenRollbackThrows_DoesNotRollbackAgainDuringDispose()
    {
        var rollbackCount = 0;
        var transaction = new SqlSugarTransactionContext(
            () => { },
            () =>
            {
                rollbackCount++;
                throw new InvalidOperationException("rollback failed");
            });

        await Assert.ThrowsAsync<InvalidOperationException>(() => transaction.RollbackAsync());
        await transaction.DisposeAsync();

        Assert.Equal(1, rollbackCount);
    }
}
