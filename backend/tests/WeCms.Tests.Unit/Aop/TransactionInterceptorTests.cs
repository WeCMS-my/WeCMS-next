using WeCms.Aop;
using WeCms.Shared.Data;

namespace WeCms.Tests.Unit.Aop;

public sealed class TransactionInterceptorTests
{
    [Fact]
    public async Task TransactionInterceptor_CommitsOnSuccess()
    {
        var unitOfWork = new FakeUnitOfWork();
        var interceptor = new TransactionInterceptor(unitOfWork);
        var operationCalled = false;

        await interceptor.InvokeAsync(cancellationToken =>
        {
            operationCalled = !cancellationToken.IsCancellationRequested;
            return Task.CompletedTask;
        }, CancellationToken.None);

        Assert.True(operationCalled);
        Assert.Equal(1, unitOfWork.BeginCalls);
        Assert.Equal(1, unitOfWork.CommitCalls);
        Assert.Equal(0, unitOfWork.RollbackCalls);
        Assert.Equal(1, unitOfWork.DisposeCalls);
    }

    [Fact]
    public async Task TransactionInterceptor_CommitsOnSuccess_ForTaskOfT()
    {
        var unitOfWork = new FakeUnitOfWork();
        var interceptor = new TransactionInterceptor(unitOfWork);

        var result = await interceptor.InvokeAsync(_ => Task.FromResult("saved"), CancellationToken.None);

        Assert.Equal("saved", result);
        Assert.Equal(1, unitOfWork.BeginCalls);
        Assert.Equal(1, unitOfWork.CommitCalls);
        Assert.Equal(0, unitOfWork.RollbackCalls);
    }

    [Fact]
    public async Task TransactionInterceptor_RollsBackAndRethrowsOnException()
    {
        var unitOfWork = new FakeUnitOfWork();
        var interceptor = new TransactionInterceptor(unitOfWork);
        var expected = new InvalidOperationException("mutation failed");

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            interceptor.InvokeAsync(_ => Task.FromException(expected), CancellationToken.None));

        Assert.Same(expected, actual);
        Assert.Equal(1, unitOfWork.BeginCalls);
        Assert.Equal(0, unitOfWork.CommitCalls);
        Assert.Equal(1, unitOfWork.RollbackCalls);
        Assert.Equal(1, unitOfWork.DisposeCalls);
    }

    [Fact]
    public async Task TransactionInterceptor_PassesCancellationToken()
    {
        var unitOfWork = new FakeUnitOfWork();
        var interceptor = new TransactionInterceptor(unitOfWork);
        using var cancellationTokenSource = new CancellationTokenSource();
        var expectedToken = cancellationTokenSource.Token;
        CancellationToken observedToken = default;

        await interceptor.InvokeAsync(cancellationToken =>
        {
            observedToken = cancellationToken;
            return Task.CompletedTask;
        }, expectedToken);

        Assert.Equal(expectedToken, observedToken);
        Assert.Equal(expectedToken, unitOfWork.BeginToken);
        Assert.Equal(expectedToken, unitOfWork.CommitToken);
    }

    [Fact]
    public async Task TransactionInterceptor_NestedInvocation_ReusesAmbientTransaction()
    {
        var unitOfWork = new FakeUnitOfWork();
        var interceptor = new TransactionInterceptor(unitOfWork);

        await interceptor.InvokeAsync(_ =>
            interceptor.InvokeAsync(__ => Task.CompletedTask, CancellationToken.None), CancellationToken.None);

        Assert.Equal(1, unitOfWork.BeginCalls);
        Assert.Equal(1, unitOfWork.CommitCalls);
        Assert.Equal(0, unitOfWork.RollbackCalls);
    }

    [Fact]
    public async Task TransactionInterceptor_NestedException_RollsBackOnlyOuterTransaction()
    {
        var unitOfWork = new FakeUnitOfWork();
        var interceptor = new TransactionInterceptor(unitOfWork);
        var expected = new InvalidOperationException("nested mutation failed");

        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            interceptor.InvokeAsync(_ =>
                interceptor.InvokeAsync(__ => Task.FromException(expected), CancellationToken.None), CancellationToken.None));

        Assert.Same(expected, actual);
        Assert.Equal(1, unitOfWork.BeginCalls);
        Assert.Equal(0, unitOfWork.CommitCalls);
        Assert.Equal(1, unitOfWork.RollbackCalls);
        Assert.Equal(1, unitOfWork.DisposeCalls);
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int BeginCalls { get; private set; }
        public int CommitCalls { get; private set; }
        public int RollbackCalls { get; private set; }
        public int DisposeCalls { get; private set; }
        public CancellationToken BeginToken { get; private set; }
        public CancellationToken CommitToken { get; private set; }

        public Task<ITransactionContext> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            BeginCalls++;
            BeginToken = cancellationToken;
            return Task.FromResult<ITransactionContext>(new FakeTransactionContext(this));
        }

        private sealed class FakeTransactionContext(FakeUnitOfWork unitOfWork) : ITransactionContext
        {
            public Task CommitAsync(CancellationToken cancellationToken = default)
            {
                unitOfWork.CommitCalls++;
                unitOfWork.CommitToken = cancellationToken;
                return Task.CompletedTask;
            }

            public Task RollbackAsync(CancellationToken cancellationToken = default)
            {
                unitOfWork.RollbackCalls++;
                return Task.CompletedTask;
            }

            public ValueTask DisposeAsync()
            {
                unitOfWork.DisposeCalls++;
                return ValueTask.CompletedTask;
            }
        }
    }
}
