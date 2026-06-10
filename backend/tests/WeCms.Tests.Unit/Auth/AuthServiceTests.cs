using System.Data.Common;
using WeCms.Shared.Data;
using WeCms.Shared.Security;
using WeCms.Modules.System.Auth;
using WeCms.Shared;
using WeCms.Shared.Time;

namespace WeCms.Tests.Unit.Auth;

public sealed class AuthServiceTests
{
    [Fact]
    public async Task LoginAsync_ShouldExecutePersistenceStepsInSameTransaction()
    {
        var repository = new TrackingAuthRepository();
        var hasher = new FakePasswordHasher();
        var tokenService = new FakeTokenService();
        var tokenGenerator = new FakeTokenGenerator();
        var tokenHasher = new FakeRefreshTokenHasher();
        var unitOfWork = new TrackingUnitOfWork();
        var clock = new FixedClock(new DateTimeOffset(2026, 06, 10, 10, 0, 0, TimeSpan.Zero));

        repository.UserByUsernameResult = new UserRow(
            1,
            "admin",
            "Admin",
            "hash-admin",
            1,
            "stamp",
            1);

        var service = new AuthService(
            repository,
            hasher,
            tokenService,
            tokenGenerator,
            tokenHasher,
            unitOfWork,
            clock);

        hasher.Verifications["hash-admin:Admin@123"] = true;
        repository.InsertRefreshTokenResult = 100;
        repository.UpdateUserLastLoginResult = 1;
        repository.InsertLoginLogResult = 999;

        var response = await service.LoginAsync(
            new LoginRequest("admin", "Admin@123"),
            "127.0.0.1",
            "agent",
            default);

        Assert.Equal("access-token", response.AccessToken);
        Assert.Equal("refresh-token-1", response.RefreshToken);

        Assert.Equal(1, unitOfWork.BeginCount);
        Assert.Equal(1, unitOfWork.CommitCount);
        Assert.Equal(0, unitOfWork.RollbackCount);

        var refreshCallTx = repository.Transactions["InsertRefreshTokenAsync"];
        Assert.NotNull(refreshCallTx);
        Assert.Same(refreshCallTx, repository.Transactions["UpdateUserLastLoginAsync"]);
        Assert.Same(refreshCallTx, repository.Transactions["InsertLoginLogAsync"]);
    }

    [Fact]
    public async Task RefreshAsync_ShouldQueryAndRotateTokenInSameTransaction()
    {
        var repository = new TrackingAuthRepository();
        var hasher = new FakePasswordHasher();
        var tokenService = new FakeTokenService();
        var tokenGenerator = new FakeTokenGenerator();
        var tokenHasher = new FakeRefreshTokenHasher();
        var unitOfWork = new TrackingUnitOfWork();
        var clock = new FixedClock(new DateTimeOffset(2026, 06, 10, 10, 0, 0, TimeSpan.Zero));

        repository.GetRefreshTokenByHashResult = new RefreshTokenRow(
            11,
            1,
            "old-hash",
            "family-id",
            clock.UtcNow.AddHours(1),
            null,
            null);
        repository.UserByIdResult = new UserRow(
            1,
            "admin",
            "Admin",
            "hash-admin",
            1,
            "stamp",
            1);
        repository.InsertRefreshTokenResult = 200;
        repository.RevokeRefreshTokenResult = 1;

        var service = new AuthService(
            repository,
            hasher,
            tokenService,
            tokenGenerator,
            tokenHasher,
            unitOfWork,
            clock);

        var response = await service.RefreshAsync(
            new RefreshRequest("refresh-token"),
            "127.0.0.1",
            "agent",
            default);

        Assert.Equal("access-token", response.AccessToken);
        Assert.Equal("refresh-token-1", response.RefreshToken);

        Assert.Equal(1, unitOfWork.BeginCount);
        Assert.Equal(1, unitOfWork.CommitCount);
        Assert.Equal(0, unitOfWork.RollbackCount);

        var queryTx = repository.Transactions["GetRefreshTokenByHashAsync"];
        Assert.NotNull(queryTx);
        Assert.Same(queryTx, repository.Transactions["GetUserByIdAsync"]);
        Assert.Same(queryTx, repository.Transactions["InsertRefreshTokenAsync"]);
        Assert.Same(queryTx, repository.Transactions["RevokeRefreshTokenAsync"]);
    }

    [Fact]
    public async Task RefreshAsync_WhenRevokeRowsIsZero_ShouldRollbackAndThrowUnauthorized()
    {
        var repository = new TrackingAuthRepository();
        var hasher = new FakePasswordHasher();
        var tokenService = new FakeTokenService();
        var tokenGenerator = new FakeTokenGenerator();
        var tokenHasher = new FakeRefreshTokenHasher();
        var unitOfWork = new TrackingUnitOfWork();
        var clock = new FixedClock(new DateTimeOffset(2026, 06, 10, 10, 0, 0, TimeSpan.Zero));

        repository.GetRefreshTokenByHashResult = new RefreshTokenRow(
            11,
            1,
            "old-hash",
            "family-id",
            clock.UtcNow.AddHours(1),
            null,
            null);
        repository.UserByIdResult = new UserRow(
            1,
            "admin",
            "Admin",
            "hash-admin",
            1,
            "stamp",
            1);
        repository.InsertRefreshTokenResult = 200;
        repository.RevokeRefreshTokenResult = 0;

        var service = new AuthService(
            repository,
            hasher,
            tokenService,
            tokenGenerator,
            tokenHasher,
            unitOfWork,
            clock);

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => service.RefreshAsync(new RefreshRequest("refresh-token"), "127.0.0.1", "agent", default));

        Assert.Equal(ApiCodes.Unauthorized, ex.Code);
        Assert.Equal(1, unitOfWork.BeginCount);
        Assert.Equal(1, unitOfWork.RollbackCount);
        Assert.Equal(0, unitOfWork.CommitCount);
    }

    [Fact]
    public async Task LoginAsync_WhenInsertRefreshTokenFails_ShouldRollbackAndThrowSystemError()
    {
        var repository = new TrackingAuthRepository();
        var hasher = new FakePasswordHasher();
        var tokenService = new FakeTokenService();
        var tokenGenerator = new FakeTokenGenerator();
        var tokenHasher = new FakeRefreshTokenHasher();
        var unitOfWork = new TrackingUnitOfWork();
        var clock = new FixedClock(new DateTimeOffset(2026, 06, 10, 10, 0, 0, TimeSpan.Zero));

        repository.UserByUsernameResult = new UserRow(
            1,
            "admin",
            "Admin",
            "hash-admin",
            1,
            "stamp",
            1);
        repository.InsertRefreshTokenResult = 0;
        hasher.Verifications["hash-admin:Admin@123"] = true;

        var service = new AuthService(
            repository,
            hasher,
            tokenService,
            tokenGenerator,
            tokenHasher,
            unitOfWork,
            clock);

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => service.LoginAsync(new LoginRequest("admin", "Admin@123"), "127.0.0.1", "agent", default));

        Assert.Equal(ApiCodes.SystemError, ex.Code);
        Assert.Equal("登录会话创建失败", ex.Message);
        Assert.Equal(1, unitOfWork.BeginCount);
        Assert.Equal(1, unitOfWork.RollbackCount);
        Assert.Equal(0, unitOfWork.CommitCount);
    }

    [Fact]
    public async Task RefreshAsync_WhenInsertNewTokenFails_ShouldRollbackAndThrowSystemError()
    {
        var repository = new TrackingAuthRepository();
        var hasher = new FakePasswordHasher();
        var tokenService = new FakeTokenService();
        var tokenGenerator = new FakeTokenGenerator();
        var tokenHasher = new FakeRefreshTokenHasher();
        var unitOfWork = new TrackingUnitOfWork();
        var clock = new FixedClock(new DateTimeOffset(2026, 06, 10, 10, 0, 0, TimeSpan.Zero));

        repository.GetRefreshTokenByHashResult = new RefreshTokenRow(
            11,
            1,
            "old-hash",
            "family-id",
            clock.UtcNow.AddHours(1),
            null,
            null);
        repository.UserByIdResult = new UserRow(
            1,
            "admin",
            "Admin",
            "hash-admin",
            1,
            "stamp",
            1);
        repository.InsertRefreshTokenResult = 0;

        var service = new AuthService(
            repository,
            hasher,
            tokenService,
            tokenGenerator,
            tokenHasher,
            unitOfWork,
            clock);

        var ex = await Assert.ThrowsAsync<DomainException>(
            () => service.RefreshAsync(new RefreshRequest("refresh-token"), "127.0.0.1", "agent", default));

        Assert.Equal(ApiCodes.SystemError, ex.Code);
        Assert.Equal("刷新令牌写入失败", ex.Message);
        Assert.Equal(1, unitOfWork.BeginCount);
        Assert.Equal(1, unitOfWork.RollbackCount);
        Assert.Equal(0, unitOfWork.CommitCount);
    }

    private sealed class TrackingAuthRepository : IAuthRepository
    {
        public readonly Dictionary<string, IDbTransactionFacade?> Transactions = new();

        public UserRow? UserByUsernameResult;
        public UserRow? UserByIdResult;
        public RefreshTokenRow? GetRefreshTokenByHashResult;
        public long InsertRefreshTokenResult;
        public int UpdateUserLastLoginResult;
        public long InsertLoginLogResult;
        public long InsertSecurityEventResult = 1;
        public int RevokeRefreshTokenResult;

        public Task<UserRow?> GetUserByUsernameAsync(
            IDbTransactionFacade? transaction,
            string username,
            CancellationToken cancellationToken)
        {
            Transactions[nameof(GetUserByUsernameAsync)] = transaction;
            return Task.FromResult(UserByUsernameResult);
        }

        public Task<UserRow?> GetUserByIdAsync(
            IDbTransactionFacade? transaction,
            long id,
            CancellationToken cancellationToken)
        {
            Transactions[nameof(GetUserByIdAsync)] = transaction;
            return Task.FromResult(UserByIdResult);
        }

        public Task<long> InsertRefreshTokenAsync(
            IDbTransactionFacade? transaction,
            RefreshTokenInsertRow row,
            CancellationToken cancellationToken)
        {
            Transactions[nameof(InsertRefreshTokenAsync)] = transaction;
            return Task.FromResult(InsertRefreshTokenResult);
        }

        public Task<RefreshTokenRow?> GetRefreshTokenByHashAsync(
            IDbTransactionFacade? transaction,
            string tokenHash,
            CancellationToken cancellationToken)
        {
            Transactions[nameof(GetRefreshTokenByHashAsync)] = transaction;
            return Task.FromResult(GetRefreshTokenByHashResult);
        }

        public Task<int> RevokeRefreshTokenAsync(
            IDbTransactionFacade? transaction,
            long tokenId,
            DateTimeOffset revokedAt,
            long? replacedByTokenId,
            CancellationToken cancellationToken)
        {
            Transactions[nameof(RevokeRefreshTokenAsync)] = transaction;
            return Task.FromResult(RevokeRefreshTokenResult);
        }

        public Task<int> RevokeRefreshTokenFamilyAsync(
            IDbTransactionFacade? transaction,
            string familyId,
            long exceptTokenId,
            DateTimeOffset revokedAt,
            CancellationToken cancellationToken)
        {
            Transactions[nameof(RevokeRefreshTokenFamilyAsync)] = transaction;
            return Task.FromResult(0);
        }

        public Task<long> InsertLoginLogAsync(
            IDbTransactionFacade? transaction,
            LoginLogInsertRow row,
            CancellationToken cancellationToken)
        {
            Transactions[nameof(InsertLoginLogAsync)] = transaction;
            return Task.FromResult(InsertLoginLogResult);
        }

        public Task<long> InsertSecurityEventAsync(
            IDbTransactionFacade? transaction,
            SecurityEventInsertRow row,
            CancellationToken cancellationToken)
        {
            Transactions[nameof(InsertSecurityEventAsync)] = transaction;
            return Task.FromResult(InsertSecurityEventResult);
        }

        public Task<IReadOnlyList<string>> GetUserRoleCodesAsync(
            IDbTransactionFacade? transaction,
            long userId,
            CancellationToken cancellationToken)
            => Task.FromResult(Array.Empty<string>() as IReadOnlyList<string>);

        public Task<IReadOnlyList<string>> GetUserPermissionCodesAsync(
            IDbTransactionFacade? transaction,
            long userId,
            CancellationToken cancellationToken)
            => Task.FromResult(Array.Empty<string>() as IReadOnlyList<string>);

        public Task<int> UpdateUserLastLoginAsync(
            IDbTransactionFacade? transaction,
            long userId,
            DateTimeOffset loginAt,
            string ip,
            CancellationToken cancellationToken)
        {
            Transactions[nameof(UpdateUserLastLoginAsync)] = transaction;
            return Task.FromResult(UpdateUserLastLoginResult);
        }
    }

    private sealed class TrackingUnitOfWork : IUnitOfWork
    {
        private IDbTransactionFacade _transaction = new TrackingTransactionFacade();
        public int BeginCount { get; private set; }
        public int CommitCount { get; private set; }
        public int RollbackCount { get; private set; }

        public IDbTransactionFacade Transaction => _transaction;

        public Task BeginAsync(CancellationToken cancellationToken) { BeginCount++; return Task.CompletedTask; }
        public Task CommitAsync(CancellationToken cancellationToken) { CommitCount++; return Task.CompletedTask; }
        public Task RollbackAsync(CancellationToken cancellationToken) { RollbackCount++; return Task.CompletedTask; }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class TrackingTransactionFacade : IDbTransactionFacade
    {
        public DbConnection Connection => null!;
        public DbTransaction? Inner => null;
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public Dictionary<string, bool> Verifications { get; } = new();

        public string Hash(string password) => password;

        public bool Verify(string password, string hash) =>
            Verifications.TryGetValue($"{hash}:{password}", out var ok) && ok;
    }

    private sealed class FakeTokenService : ITokenService
    {
        public string GenerateAccessToken(CurrentUser user) => "access-token";

        public TokenValidationResult ValidateAccessToken(string token) => new(false);
    }

    private sealed class FakeTokenGenerator : ITokenGenerator
    {
        private int _next = 1;

        public string GenerateRefreshToken() => $"refresh-token-{_next++}";
    }

    private sealed class FakeRefreshTokenHasher : IRefreshTokenHasher
    {
        public string Hash(string token) => $"hashed:{token}";
    }

    private sealed class FixedClock : IClock
    {
        private readonly DateTimeOffset _utcNow;

        public FixedClock(DateTimeOffset utcNow) => _utcNow = utcNow;

        public DateTimeOffset UtcNow => _utcNow;
    }
}
