using WeCms.Modules.Organization;
using WeCms.Modules.AccessControl.Permissions;
using WeCms.Shared;
using WeCms.Shared.Data;

namespace WeCms.Tests.Unit.Users;

public sealed class UserServiceTests
{
    [Fact]
    public async Task ListAsync_RejectsPageSizeGreaterThanOneHundred()
    {
        var service = CreateService(new FakeUserRepository());

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.ListAsync(new UserListQuery(PageSize: 101), CancellationToken.None));

        Assert.Equal(ApiCodes.ValidationError, exception.Code);
    }

    [Fact]
    public async Task DeleteAsync_RejectsSelfDelete()
    {
        var service = CreateService(new FakeUserRepository());

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.DeleteAsync(1, Context(actorUserId: 1), CancellationToken.None));

        Assert.Equal(ApiCodes.BusinessError, exception.Code);
    }

    [Fact]
    public async Task DisableAsync_RejectsLastLockedRoleHolder()
    {
        var repository = new FakeUserRepository { EnabledUsersByLockedRole = new Dictionary<long, int> { [9] = 1 } };
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.DisableAsync(1, Context(actorUserId: 2), CancellationToken.None));

        Assert.Equal(ApiCodes.BusinessError, exception.Code);
        Assert.Equal("Locked role must have at least one enabled user.", exception.Message);
    }

    [Fact]
    public async Task DeleteAsync_RejectsLastEnabledLockedRoleHolder()
    {
        var repository = new FakeUserRepository { EnabledUsersByLockedRole = new Dictionary<long, int> { [9] = 1 } };
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.DeleteAsync(1, Context(actorUserId: 2), CancellationToken.None));

        Assert.Equal(ApiCodes.BusinessError, exception.Code);
        Assert.Equal("Locked role must have at least one enabled user.", exception.Message);
    }

    [Fact]
    public async Task DisableAsync_RejectsLastEnabledLockedRoleHolder()
    {
        var repository = new FakeUserRepository { EnabledUsersByLockedRole = new Dictionary<long, int> { [9] = 1 } };
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.DisableAsync(1, Context(actorUserId: 2), CancellationToken.None));

        Assert.Equal(ApiCodes.BusinessError, exception.Code);
        Assert.Equal("Locked role must have at least one enabled user.", exception.Message);
    }

    [Fact]
    public async Task AssignRolesAsync_RejectsRemovingLockedRoleFromLastHolder()
    {
        var repository = new FakeUserRepository { EnabledUsersByLockedRole = new Dictionary<long, int> { [9] = 1 } };
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.AssignRolesAsync(1, new AssignUserRolesRequest([2]), Context(actorUserId: 2), CancellationToken.None));

        Assert.Equal(ApiCodes.BusinessError, exception.Code);
        Assert.Equal("Locked role must have at least one enabled user.", exception.Message);
    }

    [Fact]
    public async Task AssignRolesAsync_AllowsRemovingLockedRoleWhenAnotherEnabledHolderExists()
    {
        var repository = new FakeUserRepository { EnabledUsersByLockedRole = new Dictionary<long, int> { [9] = 2 } };
        var service = CreateService(repository);

        await service.AssignRolesAsync(1, new AssignUserRolesRequest([2]), Context(actorUserId: 2), CancellationToken.None);

        Assert.True(repository.RolesWereReplaced);
    }

    [Fact]
    public async Task AssignRolesAsync_AllowsAddingLockedRole()
    {
        var repository = new FakeUserRepository
        {
            CurrentLockedRoleIds = [],
            RequestedLockedRoleIds = new HashSet<long> { 9 },
            EnabledUsersByLockedRole = new Dictionary<long, int> { [9] = 1 }
        };
        var service = CreateService(repository);

        await service.AssignRolesAsync(1, new AssignUserRolesRequest([9]), Context(actorUserId: 2), CancellationToken.None);

        Assert.True(repository.RolesWereReplaced);
    }

    [Fact]
    public async Task CreateAsync_UsesOrganizationLookupForDepartmentAndPositions()
    {
        var lookup = new FakeOrganizationLookupService();
        var service = CreateService(new FakeUserRepository(), organizationLookupService: lookup);

        await service.CreateAsync(
            new CreateUserRequest("operator", "Operator", "Password@123", null, null, 7, [], [3, 3, 4]),
            Context(actorUserId: 2),
            CancellationToken.None);

        Assert.Equal(new long[] { 7 }, lookup.CheckedDepartmentIds);
        Assert.Equal(new long[] { 3, 4 }, lookup.CheckedPositionIds);
    }

    [Fact]
    public async Task UpdateAsync_RejectsUnknownDepartmentFromOrganizationLookup()
    {
        var lookup = new FakeOrganizationLookupService { DepartmentExists = false };
        var service = CreateService(new FakeUserRepository(), organizationLookupService: lookup);

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.UpdateAsync(1, new UpdateUserRequest("Operator", null, null, 7), Context(actorUserId: 2), CancellationToken.None));

        Assert.Equal(ApiCodes.ValidationError, exception.Code);
        Assert.Equal(new long[] { 7 }, lookup.CheckedDepartmentIds);
    }

    [Fact]
    public async Task AssignPositionsAsync_RejectsUnknownPositionsFromOrganizationLookup()
    {
        var lookup = new FakeOrganizationLookupService { ExistingPositionIds = new HashSet<long> { 3 } };
        var service = CreateService(new FakeUserRepository(), organizationLookupService: lookup);

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.AssignPositionsAsync(1, new AssignUserPositionsRequest([3, 4]), Context(actorUserId: 2), CancellationToken.None));

        Assert.Equal(ApiCodes.ValidationError, exception.Code);
        Assert.Equal(new long[] { 3, 4 }, lookup.CheckedPositionIds);
    }

    [Fact]
    public async Task DeleteAsync_RevokesRefreshTokensAndCommitsTransaction()
    {
        var operations = new List<string>();
        var repository = new FakeUserRepository(operations);
        var unitOfWork = new FakeUnitOfWork(operations);
        var service = CreateService(repository, unitOfWork: unitOfWork);

        await service.DeleteAsync(1, Context(actorUserId: 2), CancellationToken.None);

        Assert.Equal(1, repository.SoftDeleteCalls);
        Assert.Equal(1, repository.RevokeRefreshTokenCalls);
        Assert.Equal(1, repository.RecordAuditCalls);
        Assert.Equal(1, unitOfWork.BeginTransactionCalls);
        Assert.Equal(1, unitOfWork.CommitCalls);
        Assert.Equal(0, unitOfWork.RollbackCalls);
        AssertHolderCheckWasInsideCommittedTransaction(operations);
    }

    [Fact]
    public async Task DisableAsync_RevokesRefreshTokensAndCommitsTransaction()
    {
        var operations = new List<string>();
        var repository = new FakeUserRepository(operations);
        var unitOfWork = new FakeUnitOfWork(operations);
        var service = CreateService(repository, unitOfWork: unitOfWork);

        await service.DisableAsync(1, Context(actorUserId: 2), CancellationToken.None);

        Assert.Equal(1, repository.UpdateStatusCalls);
        Assert.Equal("disabled", repository.LastUpdatedStatus);
        Assert.Equal(1, repository.RevokeRefreshTokenCalls);
        Assert.Equal(1, repository.RecordAuditCalls);
        Assert.Equal(1, unitOfWork.BeginTransactionCalls);
        Assert.Equal(1, unitOfWork.CommitCalls);
        Assert.Equal(0, unitOfWork.RollbackCalls);
        AssertHolderCheckWasInsideCommittedTransaction(operations);
    }

    [Fact]
    public async Task AssignRolesAsync_ChecksLockedRoleHolderInsideCommittedTransaction()
    {
        var operations = new List<string>();
        var repository = new FakeUserRepository(operations) { EnabledUsersByLockedRole = new Dictionary<long, int> { [9] = 2 } };
        var unitOfWork = new FakeUnitOfWork(operations);
        var service = CreateService(repository, unitOfWork: unitOfWork);

        await service.AssignRolesAsync(1, new AssignUserRolesRequest([2]), Context(actorUserId: 2), CancellationToken.None);

        Assert.True(repository.RolesWereReplaced);
        Assert.Equal(1, unitOfWork.BeginTransactionCalls);
        Assert.Equal(1, unitOfWork.CommitCalls);
        Assert.Equal(0, unitOfWork.RollbackCalls);
        AssertHolderCheckWasInsideCommittedTransaction(operations);
    }

    [Fact]
    public async Task ResetPasswordAsync_RevokesRefreshTokensAndCommitsTransaction()
    {
        var repository = new FakeUserRepository();
        var unitOfWork = new FakeUnitOfWork();
        var service = CreateService(repository, unitOfWork: unitOfWork);

        await service.ResetPasswordAsync(1, new ResetUserPasswordRequest("NewPass@123"), Context(actorUserId: 2), CancellationToken.None);

        Assert.Equal(1, repository.ResetPasswordCalls);
        Assert.Equal(1, repository.RevokeRefreshTokenCalls);
        Assert.Equal(1, repository.RecordAuditCalls);
        Assert.Equal(1, unitOfWork.BeginTransactionCalls);
        Assert.Equal(1, unitOfWork.CommitCalls);
        Assert.Equal(0, unitOfWork.RollbackCalls);
    }

    [Fact]
    public async Task ResetTwoFactorAsync_ClearsTwoFactorRevokesTokensAuditsAndWritesSecurityEvent()
    {
        var repository = new FakeUserRepository();
        var twoFactorService = new FakeTwoFactorService();
        var unitOfWork = new FakeUnitOfWork();
        var service = CreateService(repository, twoFactorService, unitOfWork);

        await service.ResetTwoFactorAsync(1, new ResetUserTwoFactorRequest("Support ticket SEC-42"), Context(actorUserId: 2), CancellationToken.None);

        Assert.Equal(1, twoFactorService.ClearCalls);
        Assert.Equal(1, repository.RevokeRefreshTokenCalls);
        Assert.Equal(1, repository.RecordAuditCalls);
        Assert.Equal(1, repository.RecordSecurityEventCalls);
        Assert.Equal("auth.user_2fa_reset", repository.LastSecurityEventType);
        Assert.Contains("SEC-42", repository.LastAuditDetail, StringComparison.Ordinal);
        Assert.Contains("SEC-42", repository.LastSecurityEventMessage, StringComparison.Ordinal);
        Assert.Equal(1, unitOfWork.BeginTransactionCalls);
        Assert.Equal(1, unitOfWork.CommitCalls);
        Assert.Equal(0, unitOfWork.RollbackCalls);
    }

    [Fact]
    public async Task ResetTwoFactorAsync_RejectsMissingReason()
    {
        var service = CreateService(new FakeUserRepository());

        var exception = await Assert.ThrowsAsync<DomainException>(
            () => service.ResetTwoFactorAsync(1, new ResetUserTwoFactorRequest(" "), Context(actorUserId: 2), CancellationToken.None));

        Assert.Equal(ApiCodes.ValidationError, exception.Code);
    }

    private static UserService CreateService(
        FakeUserRepository repository,
        FakeTwoFactorService? twoFactorService = null,
        FakeUnitOfWork? unitOfWork = null,
        IOrganizationLookupService? organizationLookupService = null)
    {
        return new UserService(
            repository,
            new FakePasswordHasher(),
            unitOfWork ?? new FakeUnitOfWork(),
            twoFactorService ?? new FakeTwoFactorService(),
            new FakePermissionVersionService(),
            organizationLookupService ?? new FakeOrganizationLookupService(),
            new WeCms.Tests.Unit.NullOutboxWriter(),
            new WeCms.Tests.Unit.FixedTestIdGenerator());
    }

    private static UserRequestContext Context(long actorUserId)
    {
        return new UserRequestContext(
            actorUserId,
            "admin",
            "192.168.101.199",
            "unit-test",
            "trace",
            new DateTimeOffset(2026, 6, 16, 0, 0, 0, TimeSpan.Zero));
    }

    private static void AssertHolderCheckWasInsideCommittedTransaction(IReadOnlyList<string> operations)
    {
        var orderedOperations = operations.ToList();
        var beginIndex = orderedOperations.IndexOf("begin");
        var countIndex = orderedOperations.IndexOf("count-locked-role-holders");
        var commitIndex = orderedOperations.IndexOf("commit");

        Assert.True(beginIndex >= 0, string.Join(", ", operations));
        Assert.True(countIndex > beginIndex, string.Join(", ", operations));
        Assert.True(commitIndex > countIndex, string.Join(", ", operations));
    }

    private sealed class FakePasswordHasher : IPasswordHasher
    {
        public string Hash(string password)
        {
            return $"hash:{password}";
        }

        public bool Verify(string password, string passwordHash)
        {
            return passwordHash == Hash(password);
        }
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly List<string>? _operations;
        public int SoftDeleteCalls { get; private set; }
        public int UpdateStatusCalls { get; private set; }
        public string LastUpdatedStatus { get; private set; } = string.Empty;
        public int ResetPasswordCalls { get; private set; }
        public int RevokeRefreshTokenCalls { get; private set; }
        public int RecordAuditCalls { get; private set; }
        public int RecordSecurityEventCalls { get; private set; }
        public string LastAuditDetail { get; private set; } = string.Empty;
        public string LastSecurityEventType { get; private set; } = string.Empty;
        public string LastSecurityEventMessage { get; private set; } = string.Empty;
        public IReadOnlyList<long> CurrentLockedRoleIds { get; init; } = [9];
        public IReadOnlySet<long> RequestedLockedRoleIds { get; init; } = new HashSet<long>();
        public IReadOnlyDictionary<long, int> EnabledUsersByLockedRole { get; init; } = new Dictionary<long, int> { [9] = 2 };
        public bool RolesWereReplaced { get; private set; }

        public FakeUserRepository()
        {
        }

        public FakeUserRepository(List<string> operations)
        {
            _operations = operations;
        }

        public Task<PagedResult<UserSummaryDto>> ListAsync(UserListCriteria criteria, CancellationToken cancellationToken)
        {
            return Task.FromResult(new PagedResult<UserSummaryDto>([], criteria.Page, criteria.PageSize, 0));
        }

        public Task<UserDetailDto?> GetAsync(long id, CancellationToken cancellationToken)
        {
            return Task.FromResult<UserDetailDto?>(new UserDetailDto(
                id,
                "admin",
                "Administrator",
                null,
                null,
                null,
                "enabled",
                true,
                0,
                null,
                [1],
                [],
                DateTimeOffset.UnixEpoch,
                DateTimeOffset.UnixEpoch));
        }

        public Task<bool> UsernameExistsAsync(string username, long? exceptUserId, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<bool> EmailExistsAsync(string email, long? exceptUserId, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<bool> PhoneExistsAsync(string phone, long? exceptUserId, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<IReadOnlySet<long>> ExistingRoleIdsAsync(IReadOnlyList<long> roleIds, CancellationToken cancellationToken) => Task.FromResult<IReadOnlySet<long>>(roleIds.ToHashSet());
        public Task<long> CreateAsync(UserCreateRecord record, CancellationToken cancellationToken) => Task.FromResult(2L);
        public Task UpdateAsync(UserUpdateRecord record, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task SoftDeleteAsync(long id, DateTimeOffset now, CancellationToken cancellationToken)
        {
            _operations?.Add("soft-delete");
            SoftDeleteCalls++;
            return Task.CompletedTask;
        }

        public Task SetStatusAsync(long id, string status, DateTimeOffset now, CancellationToken cancellationToken)
        {
            _operations?.Add($"set-status:{status}");
            UpdateStatusCalls++;
            LastUpdatedStatus = status;
            return Task.CompletedTask;
        }

        public Task ResetPasswordAsync(long id, string passwordHash, DateTimeOffset now, CancellationToken cancellationToken)
        {
            ResetPasswordCalls++;
            return Task.CompletedTask;
        }

        public Task RevokeUserRefreshTokensAsync(long userId, DateTimeOffset now, CancellationToken cancellationToken)
        {
            _operations?.Add("revoke-refresh-tokens");
            RevokeRefreshTokenCalls++;
            return Task.CompletedTask;
        }
        public Task ReplaceRolesAsync(long id, IReadOnlyList<long> roleIds, DateTimeOffset now, CancellationToken cancellationToken)
        {
            _operations?.Add("replace-roles");
            RolesWereReplaced = true;
            return Task.CompletedTask;
        }

        public Task ReplacePositionsAsync(long id, IReadOnlyList<long> positionIds, DateTimeOffset now, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<IReadOnlyList<long>> ListLockedRoleIdsByUserAsync(long userId, CancellationToken cancellationToken) => Task.FromResult(CurrentLockedRoleIds);
        public Task<IReadOnlySet<long>> ExistingLockedRoleIdsAsync(IReadOnlyList<long> roleIds, CancellationToken cancellationToken) => Task.FromResult(RequestedLockedRoleIds);
        public Task<int> CountEnabledUsersByRoleForUpdateAsync(long roleId, CancellationToken cancellationToken)
        {
            _operations?.Add("count-locked-role-holders");
            return Task.FromResult(EnabledUsersByLockedRole.GetValueOrDefault(roleId));
        }
        public Task RecordAuditAsync(UserAuditRecord record, CancellationToken cancellationToken)
        {
            _operations?.Add("audit");
            RecordAuditCalls++;
            LastAuditDetail = record.Detail;
            return Task.CompletedTask;
        }

        public Task RecordSecurityEventAsync(UserSecurityEventRecord record, CancellationToken cancellationToken)
        {
            RecordSecurityEventCalls++;
            LastSecurityEventType = record.EventType;
            LastSecurityEventMessage = record.Message;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeOrganizationLookupService : IOrganizationLookupService
    {
        public bool DepartmentExists { get; init; } = true;
        public IReadOnlySet<long>? ExistingPositionIds { get; init; }
        public List<long> CheckedDepartmentIds { get; } = [];
        public IReadOnlyList<long> CheckedPositionIds { get; private set; } = [];

        public Task<bool> DepartmentExistsAsync(long departmentId, CancellationToken cancellationToken)
        {
            CheckedDepartmentIds.Add(departmentId);
            return Task.FromResult(DepartmentExists);
        }

        public Task<IReadOnlySet<long>> ExistingPositionIdsAsync(IReadOnlyList<long> positionIds, CancellationToken cancellationToken)
        {
            CheckedPositionIds = positionIds.ToArray();
            return Task.FromResult(ExistingPositionIds ?? positionIds.ToHashSet());
        }
    }

    private sealed class FakeTwoFactorService : ITwoFactorService
    {
        public int ClearCalls { get; private set; }

        public Task<TwoFactorSetupResult> BeginSetupAsync(long userId, string accountName, DateTimeOffset now, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<TwoFactorConfirmResult> ConfirmSetupAsync(long userId, string code, DateTimeOffset now, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<TwoFactorRecoveryCodeUseResult> UseRecoveryCodeAsync(long userId, string code, DateTimeOffset now, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<TwoFactorRecoveryCodeRegenerationResult> RegenerateRecoveryCodesAsync(long userId, DateTimeOffset now, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task<TwoFactorVerificationResult> VerifyCodeAsync(long userId, string code, DateTimeOffset now, CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }

        public Task ClearAsync(long userId, DateTimeOffset now, CancellationToken cancellationToken)
        {
            ClearCalls++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakePermissionVersionService : IIdentityPermissionVersionService
    {
        public Task BumpUserAsync(long userId, DateTimeOffset now, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task BumpUsersByRoleAsync(long roleId, DateTimeOffset now, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task BumpUsersByPermissionAsync(long permissionId, DateTimeOffset now, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task BumpUsersByMenuAsync(long menuId, DateTimeOffset now, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task BumpUsersByMenusAsync(IReadOnlyList<long> menuIds, DateTimeOffset now, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        private readonly List<string>? _operations;
        public int BeginTransactionCalls { get; private set; }
        public int CommitCalls { get; private set; }
        public int RollbackCalls { get; private set; }

        public FakeUnitOfWork()
        {
        }

        public FakeUnitOfWork(List<string> operations)
        {
            _operations = operations;
        }

        public Task<ITransactionContext> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            _operations?.Add("begin");
            BeginTransactionCalls++;
            return Task.FromResult<ITransactionContext>(new FakeTransactionContext(this));
        }

        private void AddCommit()
        {
            _operations?.Add("commit");
            CommitCalls++;
        }

        private void AddRollback()
        {
            _operations?.Add("rollback");
            RollbackCalls++;
        }

        private sealed class FakeTransactionContext : ITransactionContext
        {
            private readonly FakeUnitOfWork _unitOfWork;

            public FakeTransactionContext(FakeUnitOfWork unitOfWork)
            {
                _unitOfWork = unitOfWork;
            }

            public ValueTask DisposeAsync()
            {
                return ValueTask.CompletedTask;
            }

            public Task CommitAsync(CancellationToken cancellationToken = default)
            {
                _unitOfWork.AddCommit();
                return Task.CompletedTask;
            }

            public Task RollbackAsync(CancellationToken cancellationToken = default)
            {
                _unitOfWork.AddRollback();
                return Task.CompletedTask;
            }
        }
    }
}
