using WeCms.Modules.System.Auth;
using WeCms.Modules.System.Menus;
using WeCms.Modules.System.TwoFactor;
using WeCms.Shared;

namespace WeCms.Tests.Unit.Auth;

public sealed class AccountTwoFactorServiceTests
{
    [Fact]
    public async Task StatusAsync_ReturnsDisabledWhenRecordMissing()
    {
        var service = CreateService();

        var status = await service.StatusAsync(User.Id, CancellationToken.None);

        Assert.False(status.Enabled);
        Assert.Equal(0, status.RecoveryCodesRemaining);
    }

    [Fact]
    public async Task SetupAndConfirmAsync_EnableTwoFactorAndWriteEvidence()
    {
        var repository = new FakeUserTwoFactorRepository();
        var authRepository = new FakeAuthRepository();
        var service = CreateService(repository, authRepository);

        var setup = await service.BeginSetupAsync(User.Id, RequestContext(), CancellationToken.None);
        var code = new TotpService(Options).GenerateCode(setup.Secret, Now);
        var status = await service.ConfirmAsync(User.Id, new AccountTwoFactorConfirmRequest(code), RequestContext(), CancellationToken.None);

        Assert.True(status.Enabled);
        Assert.Equal(10, status.RecoveryCodesRemaining);
        Assert.Equal("account-2fa-confirm", authRepository.LastAuditAction);
        Assert.Equal("auth.account_2fa_enabled", authRepository.LastSecurityEventType);
    }

    [Fact]
    public async Task DisableAsync_RejectsInvalidPasswordAndWritesEvidence()
    {
        var authRepository = new FakeAuthRepository();
        var service = CreateService(new FakeUserTwoFactorRepository(), authRepository);

        var exception = await Assert.ThrowsAsync<DomainException>(() => service.DisableAsync(
            User.Id,
            new AccountTwoFactorDisableRequest("wrong-password", null),
            RequestContext(),
            CancellationToken.None));

        Assert.Equal(ApiCodes.Unauthorized, exception.Code);
        Assert.Equal("account-2fa-disable", authRepository.LastAuditAction);
        Assert.Equal("blocked", authRepository.LastAuditResult);
        Assert.Equal("auth.account_2fa_sensitive_operation_rejected", authRepository.LastSecurityEventType);
    }

    [Fact]
    public async Task DisableAsync_WithCurrentPasswordClearsSensitiveData()
    {
        var repository = new FakeUserTwoFactorRepository();
        var service = CreateService(repository);
        var setup = await service.BeginSetupAsync(User.Id, RequestContext(), CancellationToken.None);
        var code = new TotpService(Options).GenerateCode(setup.Secret, Now);
        await service.ConfirmAsync(User.Id, new AccountTwoFactorConfirmRequest(code), RequestContext(), CancellationToken.None);

        await service.DisableAsync(
            User.Id,
            new AccountTwoFactorDisableRequest(CurrentPassword, null),
            RequestContext(),
            CancellationToken.None);

        Assert.False(repository.Record!.Enabled);
        Assert.Empty(repository.Record.SecretCipher);
        Assert.Empty(repository.Record.RecoveryCodeHashes);
        Assert.True(repository.Record.ResetRequired);
    }

    [Fact]
    public async Task RegenerateRecoveryCodesAsync_ReplacesOldCodesAndWritesEvidence()
    {
        var repository = new FakeUserTwoFactorRepository();
        var authRepository = new FakeAuthRepository();
        var twoFactorService = CreateTwoFactorService(repository);
        var service = CreateService(repository, authRepository, twoFactorService);
        var setup = await service.BeginSetupAsync(User.Id, RequestContext(), CancellationToken.None);
        var code = new TotpService(Options).GenerateCode(setup.Secret, Now);
        await service.ConfirmAsync(User.Id, new AccountTwoFactorConfirmRequest(code), RequestContext(), CancellationToken.None);

        var regenerated = await service.RegenerateRecoveryCodesAsync(
            User.Id,
            new AccountTwoFactorRegenerateRecoveryCodesRequest(CurrentPassword, null),
            RequestContext(),
            CancellationToken.None);
        var oldCode = await twoFactorService.UseRecoveryCodeAsync(User.Id, setup.RecoveryCodes[0], Now.AddMinutes(2), CancellationToken.None);
        var newCode = await twoFactorService.UseRecoveryCodeAsync(User.Id, regenerated.RecoveryCodes[0], Now.AddMinutes(2), CancellationToken.None);

        Assert.False(oldCode.Consumed);
        Assert.True(newCode.Consumed);
        Assert.Equal("account-2fa-recovery-codes-regenerate", authRepository.LastAuditAction);
        Assert.Equal("auth.account_2fa_recovery_codes_regenerated", authRepository.LastSecurityEventType);
    }

    private const string CurrentPassword = "CorrectHorseBatteryStaple1!";
    private static readonly DateTimeOffset Now = DateTimeOffset.FromUnixTimeSeconds(1_800_000_000);
    private static readonly TwoFactorOptions Options = new("0123456789abcdef0123456789abcdef", "WeCMS", 30, 6, 1, 10);
    private static readonly AuthUserRecord User = new(
        100,
        "admin",
        "Administrator",
        PasswordHasher.HashForTest(CurrentPassword),
        "enabled",
        true);

    private static AccountTwoFactorService CreateService(
        FakeUserTwoFactorRepository? repository = null,
        FakeAuthRepository? authRepository = null,
        ITwoFactorService? twoFactorService = null)
    {
        repository ??= new FakeUserTwoFactorRepository();
        authRepository ??= new FakeAuthRepository();
        twoFactorService ??= CreateTwoFactorService(repository);
        return new AccountTwoFactorService(
            authRepository,
            repository,
            twoFactorService,
            new PasswordHasher(),
            new FixedAuthClock());
    }

    private static TwoFactorService CreateTwoFactorService(FakeUserTwoFactorRepository repository)
    {
        return new TwoFactorService(
            repository,
            new TotpService(Options),
            new SecretProtector(Options),
            new RecoveryCodeService(Options, new FixedEntropy()),
            Options);
    }

    private static AuthRequestContext RequestContext()
    {
        return new AuthRequestContext("192.168.101.199", "unit-test", "trace-account-2fa");
    }

    private sealed class FakeAuthRepository : IAuthRepository
    {
        public string? LastAuditAction { get; private set; }
        public string? LastAuditResult { get; private set; }
        public string? LastSecurityEventType { get; private set; }

        public Task<AuthUserRecord?> FindUserByUsernameAsync(string username, CancellationToken cancellationToken) => Task.FromResult<AuthUserRecord?>(User);

        public Task<AuthUserRecord?> FindUserByIdAsync(long userId, CancellationToken cancellationToken) => Task.FromResult<AuthUserRecord?>(User);

        public Task<RefreshTokenRecord?> FindRefreshTokenByHashAsync(string tokenHash, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task<IReadOnlyList<string>> ListRoleCodesAsync(long userId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<string>>([]);

        public Task<IReadOnlyList<string>> ListPermissionCodesAsync(long userId, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<string>>([]);

        public Task<IReadOnlyList<MenuSummaryDto>> ListVisibleMenusAsync(long userId, bool isSuperAdmin, CancellationToken cancellationToken) => Task.FromResult<IReadOnlyList<MenuSummaryDto>>([]);

        public Task RecordFailedLoginAsync(FailedLoginRecord record, CancellationToken cancellationToken) => Task.CompletedTask;

        public Task RecordSecurityEventAsync(SecurityEventRecord record, CancellationToken cancellationToken)
        {
            LastSecurityEventType = record.EventType;
            return Task.CompletedTask;
        }

        public Task RecordAuditLogAsync(AuditLogRecord record, CancellationToken cancellationToken)
        {
            LastAuditAction = record.Action;
            LastAuditResult = record.Result;
            return Task.CompletedTask;
        }

        public Task CompleteSuccessfulLoginAsync(SuccessfulLoginRecord record, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task CompleteRefreshRotationAsync(RefreshRotationRecord record, CancellationToken cancellationToken) => throw new NotSupportedException();

        public Task RevokeRefreshTokenFamilyAsync(string familyId, DateTimeOffset revokedAt, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class FakeUserTwoFactorRepository : IUserTwoFactorRepository
    {
        public UserTwoFactorRecord? Record { get; private set; }

        public Task<UserTwoFactorRecord?> GetByUserIdAsync(long userId, CancellationToken cancellationToken) => Task.FromResult(Record);

        public Task UpsertSetupAsync(UserTwoFactorSetupRecord record, CancellationToken cancellationToken)
        {
            Record = new UserTwoFactorRecord(
                1,
                record.UserId,
                false,
                record.SecretCipher,
                null,
                null,
                record.RecoveryCodeHashes,
                0,
                false,
                record.Now,
                record.Now);
            return Task.CompletedTask;
        }

        public Task EnableAsync(UserTwoFactorEnableRecord record, CancellationToken cancellationToken)
        {
            Record = Record! with
            {
                Enabled = true,
                ConfirmedAt = record.Now,
                LastTotpStep = record.LastTotpStep,
                UpdatedAt = record.Now
            };
            return Task.CompletedTask;
        }

        public Task UpdateRecoveryCodesAsync(UserTwoFactorRecoveryCodeUpdateRecord record, CancellationToken cancellationToken)
        {
            Record = Record! with
            {
                RecoveryCodeHashes = record.RecoveryCodeHashes,
                RecoveryCodesUsedCount = record.RecoveryCodesUsedCount,
                UpdatedAt = record.Now
            };
            return Task.CompletedTask;
        }

        public Task UpdateLastTotpStepAsync(UserTwoFactorTotpStepUpdateRecord record, CancellationToken cancellationToken)
        {
            Record = Record! with
            {
                LastTotpStep = record.LastTotpStep,
                UpdatedAt = record.Now
            };
            return Task.CompletedTask;
        }

        public Task ClearAsync(long userId, DateTimeOffset now, CancellationToken cancellationToken)
        {
            Record = Record! with
            {
                Enabled = false,
                SecretCipher = string.Empty,
                ConfirmedAt = null,
                LastTotpStep = null,
                RecoveryCodeHashes = [],
                RecoveryCodesUsedCount = 0,
                ResetRequired = true,
                UpdatedAt = now
            };
            return Task.CompletedTask;
        }
    }

    private sealed class FixedEntropy : IRecoveryCodeEntropy, ITotpSecretEntropy
    {
        private byte _next = 1;

        public byte[] GetBytes(int count)
        {
            var bytes = new byte[count];
            for (var index = 0; index < bytes.Length; index++)
            {
                bytes[index] = _next++;
            }

            return bytes;
        }
    }

    private sealed class FixedAuthClock : IAuthClock
    {
        public DateTimeOffset UtcNow => Now;
    }
}
