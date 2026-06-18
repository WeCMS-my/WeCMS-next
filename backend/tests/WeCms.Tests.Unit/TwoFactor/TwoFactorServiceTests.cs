using WeCms.Modules.System.TwoFactor;
using WeCms.Shared;

namespace WeCms.Tests.Unit.TwoFactor;

public sealed class TwoFactorServiceTests
{
    [Fact]
    public async Task BeginSetupAsync_StoresProtectedSecretAndReturnsOneTimeRecoveryCodes()
    {
        var repository = new FakeTwoFactorRepository();
        var service = CreateService(repository);

        var setup = await service.BeginSetupAsync(1, "admin", Now, CancellationToken.None);

        Assert.Equal(1, repository.UpsertSetupCalls);
        Assert.NotNull(repository.Record);
        Assert.False(repository.Record.Enabled);
        Assert.NotEqual(setup.Secret, repository.Record.SecretCipher);
        Assert.Contains("otpauth://totp/", setup.OtpAuthUri, StringComparison.Ordinal);
        Assert.Equal(10, setup.RecoveryCodes.Count);
        Assert.Equal(10, repository.Record.RecoveryCodeHashes.Count);
        Assert.DoesNotContain(setup.RecoveryCodes[0], repository.Record.RecoveryCodeHashes[0], StringComparison.Ordinal);
    }

    [Fact]
    public async Task ConfirmSetupAsync_EnablesAndRejectsReplay()
    {
        var repository = new FakeTwoFactorRepository();
        var service = CreateService(repository);
        var setup = await service.BeginSetupAsync(1, "admin", Now, CancellationToken.None);
        var code = new TotpService(Options).GenerateCode(setup.Secret, Now);

        var confirmed = await service.ConfirmSetupAsync(1, code, Now, CancellationToken.None);

        Assert.True(confirmed.Enabled);
        Assert.True(repository.Record!.Enabled);
        Assert.NotNull(repository.Record.LastTotpStep);

        var exception = await Assert.ThrowsAsync<DomainException>(() => service.ConfirmSetupAsync(1, code, Now, CancellationToken.None));

        Assert.Equal(ApiCodes.ValidationError, exception.Code);
    }

    [Fact]
    public async Task UseRecoveryCodeAsync_RemovesUsedHash()
    {
        var repository = new FakeTwoFactorRepository();
        var service = CreateService(repository);
        var setup = await service.BeginSetupAsync(1, "admin", Now, CancellationToken.None);
        var totpCode = new TotpService(Options).GenerateCode(setup.Secret, Now);
        await service.ConfirmSetupAsync(1, totpCode, Now, CancellationToken.None);
        var code = setup.RecoveryCodes[0];

        var result = await service.UseRecoveryCodeAsync(1, code, Now, CancellationToken.None);
        var replay = await service.UseRecoveryCodeAsync(1, code, Now, CancellationToken.None);

        Assert.True(result.Consumed);
        Assert.False(replay.Consumed);
        Assert.Equal(9, repository.Record!.RecoveryCodeHashes.Count);
        Assert.Equal(1, repository.Record.RecoveryCodesUsedCount);
    }

    [Fact]
    public async Task RegenerateRecoveryCodesAsync_ReplacesOldCodes()
    {
        var repository = new FakeTwoFactorRepository();
        var service = CreateService(repository);
        var setup = await service.BeginSetupAsync(1, "admin", Now, CancellationToken.None);
        var totpCode = new TotpService(Options).GenerateCode(setup.Secret, Now);
        await service.ConfirmSetupAsync(1, totpCode, Now, CancellationToken.None);

        var regenerated = await service.RegenerateRecoveryCodesAsync(1, Now.AddMinutes(1), CancellationToken.None);
        var oldCodeResult = await service.UseRecoveryCodeAsync(1, setup.RecoveryCodes[0], Now.AddMinutes(2), CancellationToken.None);
        var newCodeResult = await service.UseRecoveryCodeAsync(1, regenerated.RecoveryCodes[0], Now.AddMinutes(2), CancellationToken.None);

        Assert.Equal(10, regenerated.RecoveryCodes.Count);
        Assert.False(oldCodeResult.Consumed);
        Assert.True(newCodeResult.Consumed);
        Assert.Equal(9, repository.Record!.RecoveryCodeHashes.Count);
        Assert.Equal(1, repository.Record.RecoveryCodesUsedCount);
    }

    private static readonly DateTimeOffset Now = DateTimeOffset.FromUnixTimeSeconds(1_800_000_000);
    private static readonly TwoFactorOptions Options = new("0123456789abcdef0123456789abcdef", "WeCMS", 30, 6, 1, 10);

    private static TwoFactorService CreateService(FakeTwoFactorRepository repository)
    {
        return new TwoFactorService(
            repository,
            new TotpService(Options),
            new SecretProtector(Options),
            new RecoveryCodeService(Options, new FixedEntropy()),
            Options);
    }

    private sealed class FakeTwoFactorRepository : IUserTwoFactorRepository
    {
        public int UpsertSetupCalls { get; private set; }
        public UserTwoFactorRecord? Record { get; private set; }

        public Task<UserTwoFactorRecord?> GetByUserIdAsync(long userId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Record);
        }

        public Task UpsertSetupAsync(UserTwoFactorSetupRecord record, CancellationToken cancellationToken)
        {
            UpsertSetupCalls++;
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
}
