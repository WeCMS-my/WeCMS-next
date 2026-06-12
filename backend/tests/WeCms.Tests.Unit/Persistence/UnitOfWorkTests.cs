using System.Data;
using System.Data.Common;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using WeCms.Persistence.Data;
using WeCms.Shared.Data;

namespace WeCms.Tests.Unit.Persistence;

public sealed class UnitOfWorkTests
{
    [Fact]
    public void Transaction_ShouldThrow_WhenBeginWasNotCalled()
    {
        var unitOfWork = new UnitOfWork(new TrackingDbConnectionFactory());

        var ex = Assert.Throws<InvalidOperationException>(() => unitOfWork.Transaction);

        Assert.Equal("UnitOfWork transaction has not been started.", ex.Message);
    }

    [Fact]
    public async Task Transaction_ShouldThrow_WhenCommitAlreadyCompleted()
    {
        var factory = new TrackingDbConnectionFactory();
        var unitOfWork = new UnitOfWork(factory);

        await unitOfWork.BeginAsync(default);
        await unitOfWork.CommitAsync(default);

        var ex = Assert.Throws<InvalidOperationException>(() => unitOfWork.Transaction);

        Assert.Equal("UnitOfWork transaction is no longer active.", ex.Message);
        Assert.True(factory.Connection!.IsDisposed);
    }

    [Fact]
    public async Task Transaction_ShouldThrow_WhenRollbackAlreadyCompleted()
    {
        var factory = new TrackingDbConnectionFactory();
        var unitOfWork = new UnitOfWork(factory);

        await unitOfWork.BeginAsync(default);
        await unitOfWork.RollbackAsync(default);

        var ex = Assert.Throws<InvalidOperationException>(() => unitOfWork.Transaction);

        Assert.Equal("UnitOfWork transaction is no longer active.", ex.Message);
        Assert.True(factory.Connection!.IsDisposed);
    }

    [Fact]
    public async Task Transaction_ShouldReturnActiveFacade_WhenTransactionStarted()
    {
        var factory = new TrackingDbConnectionFactory();
        var unitOfWork = new UnitOfWork(factory);

        await unitOfWork.BeginAsync(default);

        var transaction = unitOfWork.Transaction;

        Assert.Same(factory.Connection, transaction.Connection);
        Assert.Same(factory.Connection!.Transaction, transaction.Inner);
    }

    private sealed class TrackingDbConnectionFactory : IDbConnectionFactory
    {
        public TrackingDbConnection? Connection { get; private set; }

        public Task<DbConnection> OpenAsync(CancellationToken cancellationToken = default)
        {
            Connection = new TrackingDbConnection();
            return Task.FromResult<DbConnection>(Connection);
        }
    }

    private sealed class TrackingDbConnection : DbConnection
    {
        private ConnectionState _state = ConnectionState.Open;

        public bool IsDisposed { get; private set; }
        public TrackingDbTransaction? Transaction { get; private set; }

        [AllowNull]
        public override string ConnectionString
        {
            get => "";
            set { }
        }
        public override string Database => "wecms_test";
        public override string DataSource => "tracking";
        public override string ServerVersion => "1.0";
        public override ConnectionState State => _state;

        public override void ChangeDatabase(string databaseName)
        {
        }

        public override void Close() => _state = ConnectionState.Closed;

        public override void Open() => _state = ConnectionState.Open;

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            Transaction = new TrackingDbTransaction(this, isolationLevel);
            return Transaction;
        }

        protected override ValueTask<DbTransaction> BeginDbTransactionAsync(
            IsolationLevel isolationLevel,
            CancellationToken cancellationToken)
        {
            Transaction = new TrackingDbTransaction(this, isolationLevel);
            return ValueTask.FromResult<DbTransaction>(Transaction);
        }

        protected override DbCommand CreateDbCommand() => new TrackingDbCommand();

        public override ValueTask DisposeAsync()
        {
            IsDisposed = true;
            _state = ConnectionState.Closed;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class TrackingDbTransaction : DbTransaction
    {
        private readonly TrackingDbConnection _connection;

        public TrackingDbTransaction(TrackingDbConnection connection, IsolationLevel isolationLevel)
        {
            _connection = connection;
            IsolationLevel = isolationLevel;
        }

        public bool Disposed { get; private set; }
        public bool Committed { get; private set; }
        public bool RolledBack { get; private set; }
        public override IsolationLevel IsolationLevel { get; }
        protected override DbConnection DbConnection => _connection;

        public override void Commit() => Committed = true;

        public override Task CommitAsync(CancellationToken cancellationToken = default)
        {
            Committed = true;
            return Task.CompletedTask;
        }

        public override void Rollback() => RolledBack = true;

        public override Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            RolledBack = true;
            return Task.CompletedTask;
        }

        public override ValueTask DisposeAsync()
        {
            Disposed = true;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class TrackingDbCommand : DbCommand
    {
        [AllowNull]
        public override string CommandText
        {
            get => "";
            set { }
        }
        public override int CommandTimeout { get; set; }
        public override CommandType CommandType { get; set; }
        public override bool DesignTimeVisible { get; set; }
        public override UpdateRowSource UpdatedRowSource { get; set; }
        protected override DbConnection? DbConnection { get; set; }
        protected override DbParameterCollection DbParameterCollection { get; } = new TrackingDbParameterCollection();
        protected override DbTransaction? DbTransaction { get; set; }

        public override void Cancel()
        {
        }

        public override int ExecuteNonQuery() => 0;

        public override object? ExecuteScalar() => null;

        public override void Prepare()
        {
        }

        protected override DbParameter CreateDbParameter() => throw new NotSupportedException();

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior) => throw new NotSupportedException();
    }

    private sealed class TrackingDbParameterCollection : DbParameterCollection
    {
        public override int Count => 0;
        public override object SyncRoot { get; } = new();

        public override int Add(object value) => throw new NotSupportedException();
        public override void AddRange(Array values) => throw new NotSupportedException();
        public override void Clear() { }
        public override bool Contains(object value) => false;
        public override bool Contains(string value) => false;
        public override void CopyTo(Array array, int index) { }
        public override IEnumerator GetEnumerator() => Array.Empty<object>().GetEnumerator();
        public override int IndexOf(object value) => -1;
        public override int IndexOf(string parameterName) => -1;
        public override void Insert(int index, object value) => throw new NotSupportedException();
        public override void Remove(object value) { }
        public override void RemoveAt(int index) { }
        public override void RemoveAt(string parameterName) { }
        protected override DbParameter GetParameter(int index) => throw new NotSupportedException();
        protected override DbParameter GetParameter(string parameterName) => throw new NotSupportedException();
        protected override void SetParameter(int index, DbParameter value) => throw new NotSupportedException();
        protected override void SetParameter(string parameterName, DbParameter value) => throw new NotSupportedException();
    }
}
