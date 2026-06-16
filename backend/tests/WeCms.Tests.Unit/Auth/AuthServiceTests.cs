using WeCms.Modules.System.Auth;
using WeCms.Shared;
using WeCms.Shared.Data;

namespace WeCms.Tests.Unit.Auth;

public sealed class AuthServiceTests
{
    [Fact]
    public async Task LoginAsync_RejectsEmptyUsernameBeforeRepositoryLookup()
    {
        var repository = new FakeAuthRepository();
        var service = CreateService(repository);

        await Assert.ThrowsAsync<DomainException>(
            () => service.LoginAsync(new LoginRequest("", "password"), RequestContext(), CancellationToken.None));

        Assert.Equal(0, repository.FindUserCalls);
    }

    [Fact]
    public async Task LoginAsync_FailedPasswordWritesAuditWithoutUserDisclosure()
    {
        var repository = new FakeAuthRepository
        {
            User = new AuthUserRecord(1, "admin", "Administrator", PasswordHasher.HashForTest("correct"), "enabled", false)
        };
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.LoginAsync(new LoginRequest("admin", "wrong"), RequestContext(), CancellationToken.None));

        Assert.Equal(ApiCodes.Unauthorized, exception.Code);
        Assert.Equal("Invalid username or password.", exception.Message);
        Assert.Equal(1, repository.FailedLoginCount);
        Assert.Equal(1, repository.SecurityEventCount);
    }

    [Fact]
    public async Task LoginAsync_SuccessStoresRefreshTokenHashOnly()
    {
        var repository = new FakeAuthRepository
        {
            User = new AuthUserRecord(1, "admin", "Administrator", PasswordHasher.HashForTest("correct"), "enabled", true),
            Roles = ["super_admin"],
            Permissions = ["sys:system:secure-ping"]
        };
        var service = CreateService(repository);

        var response = await service.LoginAsync(
            new LoginRequest("admin", "correct"),
            RequestContext(),
            CancellationToken.None);

        Assert.NotEmpty(response.AccessToken);
        Assert.NotEmpty(response.RefreshToken);
        Assert.NotEqual(response.RefreshToken, repository.StoredRefreshTokenHash);
        Assert.Equal(64, repository.StoredRefreshTokenHash.Length);
        Assert.Equal(1, repository.SuccessfulLoginCount);
        Assert.Equal(["super_admin"], response.Roles);
        Assert.Equal(["sys:system:secure-ping"], response.Permissions);
        Assert.Empty(response.Menus);
    }

    [Fact]
    public async Task MeAsync_ReturnsUserRolesPermissionsAndEmptyMenus()
    {
        var repository = new FakeAuthRepository
        {
            User = new AuthUserRecord(1, "admin", "Administrator", PasswordHasher.HashForTest("correct"), "enabled", true),
            Roles = ["super_admin"],
            Permissions = ["sys:system:secure-ping"]
        };
        var service = CreateService(repository);

        var response = await service.MeAsync(1, CancellationToken.None);

        Assert.Equal("admin", response.User.Username);
        Assert.Equal(["super_admin"], response.Roles);
        Assert.Equal(["sys:system:secure-ping"], response.Permissions);
        Assert.Empty(response.Menus);
    }

    [Fact]
    public async Task RefreshAsync_SuccessRotatesRefreshTokenHashOnly()
    {
        var issued = new RefreshTokenService(new FixedAuthTokenEntropy()).Issue(new DateTimeOffset(2026, 6, 15, 0, 0, 0, TimeSpan.Zero));
        var repository = new FakeAuthRepository
        {
            RefreshToken = new RefreshTokenRecord(10, 1, "admin", "Administrator", "enabled", true, issued.Hash, "family-1", new DateTimeOffset(2026, 6, 17, 0, 0, 0, TimeSpan.Zero), null),
            Roles = ["super_admin"],
            Permissions = ["sys:system:secure-ping"]
        };
        var service = CreateService(repository);

        var response = await service.RefreshAsync(new RefreshTokenRequest(issued.Token), RequestContext(), CancellationToken.None);

        Assert.NotEmpty(response.AccessToken);
        Assert.NotEmpty(response.RefreshToken);
        Assert.Equal(10, repository.RotatedOldRefreshTokenId);
        Assert.NotEqual(response.RefreshToken, repository.RotatedNewRefreshTokenHash);
        Assert.Equal(64, repository.RotatedNewRefreshTokenHash.Length);
        Assert.Equal("family-1", repository.RotatedFamilyId);
    }

    [Fact]
    public async Task RefreshAsync_RevokedTokenRevokesFamilyAndWritesSecurityEvent()
    {
        var issued = new RefreshTokenService(new FixedAuthTokenEntropy()).Issue(new DateTimeOffset(2026, 6, 15, 0, 0, 0, TimeSpan.Zero));
        var repository = new FakeAuthRepository
        {
            RefreshToken = new RefreshTokenRecord(10, 1, "admin", "Administrator", "enabled", true, issued.Hash, "family-1", new DateTimeOffset(2026, 6, 17, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 6, 15, 1, 0, 0, TimeSpan.Zero))
        };
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.RefreshAsync(new RefreshTokenRequest(issued.Token), RequestContext(), CancellationToken.None));

        Assert.Equal(ApiCodes.Unauthorized, exception.Code);
        Assert.Equal("family-1", repository.RevokedFamilyId);
        Assert.Equal("auth.refresh_reuse", repository.LastSecurityEventType);
    }

    [Fact]
    public async Task RefreshAsync_ConcurrentAlreadyRevokedFailureRevokesFamilyAndWritesSecurityEvent()
    {
        var issued = new RefreshTokenService(new FixedAuthTokenEntropy()).Issue(new DateTimeOffset(2026, 6, 15, 0, 0, 0, TimeSpan.Zero));
        var repository = new FakeAuthRepository
        {
            RefreshToken = new RefreshTokenRecord(10, 1, "admin", "Administrator", "enabled", true, issued.Hash, "family-1", new DateTimeOffset(2026, 6, 17, 0, 0, 0, TimeSpan.Zero), null),
            ThrowAlreadyRevokedOnRotation = true
        };
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.RefreshAsync(new RefreshTokenRequest(issued.Token), RequestContext(), CancellationToken.None));

        Assert.Equal(ApiCodes.Unauthorized, exception.Code);
        Assert.Equal("family-1", repository.RevokedFamilyId);
        Assert.Equal("auth.refresh_reuse", repository.LastSecurityEventType);
    }

    [Fact]
    public async Task RefreshAsync_AlreadyFullyRevokedFamilyStillWritesReuseSecurityEvent()
    {
        var issued = new RefreshTokenService(new FixedAuthTokenEntropy()).Issue(new DateTimeOffset(2026, 6, 15, 0, 0, 0, TimeSpan.Zero));
        var repository = new FakeAuthRepository
        {
            RefreshToken = new RefreshTokenRecord(10, 1, "admin", "Administrator", "enabled", true, issued.Hash, "family-1", new DateTimeOffset(2026, 6, 17, 0, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 6, 15, 1, 0, 0, TimeSpan.Zero))
        };
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.RefreshAsync(new RefreshTokenRequest(issued.Token), RequestContext(), CancellationToken.None));

        Assert.Equal(ApiCodes.Unauthorized, exception.Code);
        Assert.Equal("family-1", repository.RevokedFamilyId);
        Assert.Equal("auth.refresh_reuse", repository.LastSecurityEventType);
    }

    [Fact]
    public async Task RefreshAsync_ExpiredTokenReturnsUnauthorizedAndWritesSecurityEvent()
    {
        var issued = new RefreshTokenService(new FixedAuthTokenEntropy()).Issue(new DateTimeOffset(2026, 6, 15, 0, 0, 0, TimeSpan.Zero));
        var repository = new FakeAuthRepository
        {
            RefreshToken = new RefreshTokenRecord(10, 1, "admin", "Administrator", "enabled", true, issued.Hash, "family-1", new DateTimeOffset(2026, 6, 15, 1, 0, 0, TimeSpan.Zero), null)
        };
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.RefreshAsync(new RefreshTokenRequest(issued.Token), RequestContext(), CancellationToken.None));

        Assert.Equal(ApiCodes.Unauthorized, exception.Code);
        Assert.Equal("auth.refresh_expired", repository.LastSecurityEventType);
    }

    [Fact]
    public async Task RefreshAsync_DisabledUserReturnsUnauthorizedAndWritesSecurityEvent()
    {
        var issued = new RefreshTokenService(new FixedAuthTokenEntropy()).Issue(new DateTimeOffset(2026, 6, 15, 0, 0, 0, TimeSpan.Zero));
        var repository = new FakeAuthRepository
        {
            RefreshToken = new RefreshTokenRecord(10, 1, "admin", "Administrator", "disabled", true, issued.Hash, "family-1", new DateTimeOffset(2026, 6, 17, 0, 0, 0, TimeSpan.Zero), null)
        };
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.RefreshAsync(new RefreshTokenRequest(issued.Token), RequestContext(), CancellationToken.None));

        Assert.Equal(ApiCodes.Unauthorized, exception.Code);
        Assert.Equal("auth.refresh_user_disabled", repository.LastSecurityEventType);
    }

    private static AuthService CreateService(FakeAuthRepository repository)
    {
        return new AuthService(
            repository,
            new PasswordHasher(),
            new AccessTokenService(new AuthTokenOptions("unit-test-secret-with-more-than-32-characters", "wecms-unit", TimeSpan.FromMinutes(15), TimeSpan.FromDays(7))),
            new RefreshTokenService(new FixedAuthTokenEntropy()),
            new FixedAuthClock(new DateTimeOffset(2026, 6, 16, 0, 0, 0, TimeSpan.Zero)),
            new FakeUnitOfWork());
    }

    private static AuthRequestContext RequestContext()
    {
        return new AuthRequestContext("127.0.0.1", "unit-test");
    }

    private sealed class FakeAuthRepository : IAuthRepository
    {
        public AuthUserRecord? User { get; init; }

        public RefreshTokenRecord? RefreshToken { get; init; }

        public IReadOnlyList<string> Roles { get; init; } = [];

        public IReadOnlyList<string> Permissions { get; init; } = [];

        public int FindUserCalls { get; private set; }

        public int FailedLoginCount { get; private set; }

        public int SecurityEventCount { get; private set; }

        public int SuccessfulLoginCount { get; private set; }

        public string StoredRefreshTokenHash { get; private set; } = string.Empty;

        public long RotatedOldRefreshTokenId { get; private set; }

        public string RotatedNewRefreshTokenHash { get; private set; } = string.Empty;

        public string RotatedFamilyId { get; private set; } = string.Empty;

        public string RevokedFamilyId { get; private set; } = string.Empty;

        public string LastSecurityEventType { get; private set; } = string.Empty;

        public bool ThrowAlreadyRevokedOnRotation { get; init; }

        public Task<AuthUserRecord?> FindUserByUsernameAsync(string username, CancellationToken cancellationToken)
        {
            FindUserCalls++;
            return Task.FromResult(User?.Username == username ? User : null);
        }

        public Task<AuthUserRecord?> FindUserByIdAsync(long userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(User?.Id == userId ? User : null);
        }

        public Task<RefreshTokenRecord?> FindRefreshTokenByHashAsync(string tokenHash, CancellationToken cancellationToken)
        {
            return Task.FromResult(RefreshToken?.TokenHash == tokenHash ? RefreshToken : null);
        }

        public Task<IReadOnlyList<string>> ListRoleCodesAsync(long userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Roles);
        }

        public Task<IReadOnlyList<string>> ListPermissionCodesAsync(long userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Permissions);
        }

        public Task RecordFailedLoginAsync(FailedLoginRecord record, CancellationToken cancellationToken)
        {
            FailedLoginCount++;
            return Task.CompletedTask;
        }

        public Task RecordSecurityEventAsync(SecurityEventRecord record, CancellationToken cancellationToken)
        {
            SecurityEventCount++;
            LastSecurityEventType = record.EventType;
            return Task.CompletedTask;
        }

        public Task CompleteSuccessfulLoginAsync(SuccessfulLoginRecord record, CancellationToken cancellationToken)
        {
            SuccessfulLoginCount++;
            StoredRefreshTokenHash = record.RefreshTokenHash;
            return Task.CompletedTask;
        }

        public Task CompleteRefreshRotationAsync(RefreshRotationRecord record, CancellationToken cancellationToken)
        {
            if (ThrowAlreadyRevokedOnRotation)
            {
                throw new RefreshTokenAlreadyRevokedException(record.FamilyId);
            }

            RotatedOldRefreshTokenId = record.OldRefreshTokenId;
            RotatedNewRefreshTokenHash = record.NewRefreshTokenHash;
            RotatedFamilyId = record.FamilyId;
            return Task.CompletedTask;
        }

        public Task RevokeRefreshTokenFamilyAsync(string familyId, DateTimeOffset revokedAt, CancellationToken cancellationToken)
        {
            RevokedFamilyId = familyId;
            return Task.CompletedTask;
        }
    }

    private sealed class FixedAuthClock : IAuthClock
    {
        public FixedAuthClock(DateTimeOffset utcNow)
        {
            UtcNow = utcNow;
        }

        public DateTimeOffset UtcNow { get; }
    }

    private sealed class FixedAuthTokenEntropy : IAuthTokenEntropy
    {
        public byte[] GetBytes(int count)
        {
            return Enumerable.Range(1, count).Select(value => (byte)value).ToArray();
        }

        public string NewFamilyId()
        {
            return "test-family";
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<ITransactionContext> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<ITransactionContext>(new FakeTransactionContext());
        }
    }

    private sealed class FakeTransactionContext : ITransactionContext
    {
        public ValueTask DisposeAsync()
        {
            return ValueTask.CompletedTask;
        }

        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task RollbackAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
