using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using WeCms.Modules.System.Auth;
using WeCms.Modules.System.Files;
using WeCms.Modules.System.TwoFactor;
using WeCms.Shared;
using WeCms.Shared.Data;

namespace WeCms.Tests.Unit.Auth;

public sealed class AccountProfileServiceTests
{
    [Fact]
    public async Task UpdateProfileAsync_UpdatesWhitelistedFieldsAndWritesEvidence()
    {
        var repository = new FakeAccountProfileRepository();
        var service = CreateService(repository);

        var profile = await service.UpdateProfileAsync(
            new UpdateAccountProfileRequest(" Alice Admin ", " alice@example.com ", " 18800001111 "),
            Context(),
            CancellationToken.None);

        Assert.Equal("Alice Admin", profile.DisplayName);
        Assert.Equal("alice@example.com", repository.Profile.Email);
        Assert.Equal("18800001111", repository.Profile.Phone);
        Assert.Equal("profile-update", repository.AuditActions.Single());
        Assert.Equal("auth.account_profile_updated", repository.SecurityEvents.Single());
    }

    [Fact]
    public async Task UpdateProfileAsync_RejectsDuplicateEmailBeforeMutation()
    {
        var repository = new FakeAccountProfileRepository { ExistingEmail = "alice@example.com" };
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<DomainException>(() => service.UpdateProfileAsync(
            new UpdateAccountProfileRequest("Alice", "alice@example.com", null),
            Context(),
            CancellationToken.None));

        Assert.Equal(ApiCodes.ValidationError, exception.Code);
        Assert.Empty(repository.AuditActions);
    }

    [Fact]
    public async Task ChangePasswordAsync_RequiresOldPasswordAndRevokesRefreshTokens()
    {
        var repository = new FakeAccountProfileRepository();
        var unitOfWork = new FakeUnitOfWork();
        var service = CreateService(repository, unitOfWork: unitOfWork);

        await service.ChangePasswordAsync(
            new ChangeAccountPasswordRequest(CurrentPassword, "NewPassword1!"),
            Context(),
            CancellationToken.None);

        Assert.True(repository.PasswordUpdated);
        Assert.True(repository.RefreshTokensRevoked);
        Assert.Equal("password-change", repository.AuditActions.Single());
        Assert.Equal("auth.account_password_changed", repository.SecurityEvents.Single());
        Assert.Equal(1, unitOfWork.CommitCalls);
        Assert.Equal(0, unitOfWork.RollbackCalls);
    }

    [Fact]
    public async Task ChangePasswordAsync_RejectsWrongOldPasswordAndWritesSecurityEvent()
    {
        var repository = new FakeAccountProfileRepository();
        var service = CreateService(repository);

        var exception = await Assert.ThrowsAsync<DomainException>(() => service.ChangePasswordAsync(
            new ChangeAccountPasswordRequest("wrong", "NewPassword1!"),
            Context(),
            CancellationToken.None));

        Assert.Equal(ApiCodes.Unauthorized, exception.Code);
        Assert.False(repository.PasswordUpdated);
        Assert.False(repository.RefreshTokensRevoked);
        Assert.Equal("auth.account_password_change_rejected", repository.SecurityEvents.Single());
    }

    [Fact]
    public async Task UploadAvatarAsync_ValidatesMetadataStoresAvatarAndDeletesOldObject()
    {
        var repository = new FakeAccountProfileRepository();
        var storage = new FakeFileStorage();
        var service = CreateService(repository, storage: storage);
        var bytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D };

        var response = await service.UploadAvatarAsync(
            new AccountAvatarUploadRequest("avatar.png", "image/png", bytes.Length, ComputeSha256(bytes)),
            CreateFormFile("avatar.png", "image/png", bytes),
            Context(),
            CancellationToken.None);

        Assert.Equal("/api/v1/account/avatar/content", response.AvatarUrl);
        Assert.Equal("2026/06/avatar.png", repository.Profile.AvatarObjectKey);
        Assert.Contains("avatars/old.png", storage.DeletedObjectKeys);
        Assert.Equal("avatar-update", repository.AuditActions.Single());
        Assert.Equal("auth.account_avatar_updated", repository.SecurityEvents.Single());
    }

    [Fact]
    public async Task UploadAvatarAsync_DeletesNewObjectWhenDatabaseUpdateFails()
    {
        var repository = new FakeAccountProfileRepository { ThrowOnAvatarUpdate = true };
        var storage = new FakeFileStorage();
        var service = CreateService(repository, storage: storage);
        var bytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D };

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UploadAvatarAsync(
            new AccountAvatarUploadRequest("avatar.png", "image/png", bytes.Length, ComputeSha256(bytes)),
            CreateFormFile("avatar.png", "image/png", bytes),
            Context(),
            CancellationToken.None));

        Assert.Contains("2026/06/avatar.png", storage.DeletedObjectKeys);
        Assert.DoesNotContain("avatars/old.png", storage.DeletedObjectKeys);
    }

    [Fact]
    public async Task UploadAvatarAsync_RejectsOversizedAvatarBeforeStorage()
    {
        var storage = new FakeFileStorage();
        var service = CreateService(storage: storage);
        var bytes = new byte[512 * 1024 + 1];

        var exception = await Assert.ThrowsAsync<DomainException>(() => service.UploadAvatarAsync(
            new AccountAvatarUploadRequest("avatar.png", "image/png", bytes.Length, ComputeSha256(bytes)),
            CreateFormFile("avatar.png", "image/png", bytes),
            Context(),
            CancellationToken.None));

        Assert.Equal(ApiCodes.ValidationError, exception.Code);
        Assert.Equal(0, storage.StoreCalls);
    }

    [Fact]
    public async Task GetSecurityAsync_ReturnsAccountAndTwoFactorState()
    {
        var twoFactorRepository = new FakeUserTwoFactorRepository
        {
            Record = new UserTwoFactorRecord(1, UserId, true, "cipher", Now, null, ["hash"], 0, true, Now, Now)
        };
        var service = CreateService(twoFactorRepository: twoFactorRepository);

        var security = await service.GetSecurityAsync(Context(), CancellationToken.None);

        Assert.True(security.TwoFactorEnabled);
        Assert.True(security.TwoFactorResetRequired);
        Assert.False(security.MustChangePassword);
        Assert.Equal("192.168.101.1", security.LastLoginIp);
    }

    private const long UserId = 100;
    private const string CurrentPassword = "CorrectHorseBatteryStaple1!";
    private static readonly DateTimeOffset Now = new(2026, 6, 18, 0, 0, 0, TimeSpan.Zero);

    private static AccountProfileService CreateService(
        FakeAccountProfileRepository? repository = null,
        FakeFileStorage? storage = null,
        FakeUnitOfWork? unitOfWork = null,
        FakeUserTwoFactorRepository? twoFactorRepository = null)
    {
        return new AccountProfileService(
            repository ?? new FakeAccountProfileRepository(),
            twoFactorRepository ?? new FakeUserTwoFactorRepository(),
            new PasswordHasher(),
            storage ?? new FakeFileStorage(),
            new FakeObjectKeyGenerator(),
            unitOfWork ?? new FakeUnitOfWork());
    }

    private static AccountRequestContext Context()
    {
        return new AccountRequestContext(UserId, "admin", "192.168.101.199", "unit-test", "trace-account", Now);
    }

    private static IFormFile CreateFormFile(string fileName, string contentType, byte[] content)
    {
        return new FormFile(new MemoryStream(content), 0, content.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    private static string ComputeSha256(byte[] bytes)
    {
        using var sha = SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(bytes)).ToLowerInvariant();
    }

    private sealed class FakeAccountProfileRepository : IAccountProfileRepository
    {
        public AccountProfileRecord Profile { get; private set; } = new(
            UserId,
            "admin",
            "Administrator",
            PasswordHasher.HashForTest(CurrentPassword),
            "admin@example.com",
            "18800000000",
            "avatars/old.png",
            "image/png",
            ".png",
            false,
            Now.AddHours(-1),
            "192.168.101.1");

        public string? ExistingEmail { get; init; }
        public string? ExistingPhone { get; init; }
        public bool ThrowOnAvatarUpdate { get; init; }
        public bool PasswordUpdated { get; private set; }
        public bool RefreshTokensRevoked { get; private set; }
        public List<string> AuditActions { get; } = [];
        public List<string> SecurityEvents { get; } = [];

        public Task<AccountProfileRecord?> GetAsync(long userId, CancellationToken cancellationToken)
        {
            return Task.FromResult<AccountProfileRecord?>(userId == Profile.Id ? Profile : null);
        }

        public Task<bool> EmailExistsAsync(string email, long exceptUserId, CancellationToken cancellationToken)
        {
            return Task.FromResult(string.Equals(email, ExistingEmail, StringComparison.OrdinalIgnoreCase));
        }

        public Task<bool> PhoneExistsAsync(string phone, long exceptUserId, CancellationToken cancellationToken)
        {
            return Task.FromResult(string.Equals(phone, ExistingPhone, StringComparison.OrdinalIgnoreCase));
        }

        public Task UpdateProfileAsync(AccountProfileUpdateRecord record, CancellationToken cancellationToken)
        {
            Profile = Profile with
            {
                DisplayName = record.DisplayName,
                Email = record.Email,
                Phone = record.Phone
            };
            return Task.CompletedTask;
        }

        public Task UpdatePasswordAsync(AccountPasswordUpdateRecord record, CancellationToken cancellationToken)
        {
            PasswordUpdated = true;
            Profile = Profile with { PasswordHash = record.PasswordHash };
            return Task.CompletedTask;
        }

        public Task RevokeRefreshTokensAsync(long userId, DateTimeOffset now, CancellationToken cancellationToken)
        {
            RefreshTokensRevoked = true;
            return Task.CompletedTask;
        }

        public Task UpdateAvatarAsync(AccountAvatarUpdateRecord record, CancellationToken cancellationToken)
        {
            if (ThrowOnAvatarUpdate)
            {
                throw new InvalidOperationException("database unavailable");
            }

            Profile = Profile with
            {
                AvatarObjectKey = record.ObjectKey,
                AvatarMimeType = record.MimeType,
                AvatarFileExt = record.FileExt
            };
            return Task.CompletedTask;
        }

        public Task RecordAuditAsync(AccountAuditRecord record, CancellationToken cancellationToken)
        {
            AuditActions.Add(record.Action);
            return Task.CompletedTask;
        }

        public Task RecordSecurityEventAsync(AccountSecurityEventRecord record, CancellationToken cancellationToken)
        {
            SecurityEvents.Add(record.EventType);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUserTwoFactorRepository : IUserTwoFactorRepository
    {
        public UserTwoFactorRecord? Record { get; init; }
        public Task<UserTwoFactorRecord?> GetByUserIdAsync(long userId, CancellationToken cancellationToken) => Task.FromResult(Record);
        public Task UpsertSetupAsync(UserTwoFactorSetupRecord record, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task EnableAsync(UserTwoFactorEnableRecord record, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task UpdateRecoveryCodesAsync(UserTwoFactorRecoveryCodeUpdateRecord record, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task UpdateLastTotpStepAsync(UserTwoFactorTotpStepUpdateRecord record, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task ClearAsync(long userId, DateTimeOffset now, CancellationToken cancellationToken) => throw new NotSupportedException();
    }

    private sealed class FakeFileStorage : IFileStorage
    {
        public int StoreCalls { get; private set; }
        public List<string> DeletedObjectKeys { get; } = [];

        public async Task<FileStorageResult> StoreAsync(Stream source, string objectKey, string fileExt, long maxSizeBytes, CancellationToken cancellationToken)
        {
            StoreCalls++;
            await using var memoryStream = new MemoryStream();
            await source.CopyToAsync(memoryStream, cancellationToken);
            if (memoryStream.Length > maxSizeBytes)
            {
                throw new DomainException(ApiCodes.ValidationError, "file size must not exceed max limit.");
            }

            var bytes = memoryStream.ToArray();
            var mimeType = fileExt switch
            {
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".webp" => "image/webp",
                _ => "application/octet-stream"
            };
            return new FileStorageResult(bytes.Length, ComputeSha256(bytes), mimeType);
        }

        public Task<Stream> OpenReadAsync(string objectKey, CancellationToken cancellationToken)
        {
            return Task.FromResult<Stream>(new MemoryStream([1, 2, 3]));
        }

        public Task DeleteAsync(string objectKey, CancellationToken cancellationToken)
        {
            DeletedObjectKeys.Add(objectKey);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeObjectKeyGenerator : IFileObjectKeyGenerator
    {
        public string GenerateObjectKey(DateTimeOffset now, string fileExt)
        {
            return $"2026/06/avatar{fileExt}";
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int CommitCalls { get; private set; }
        public int RollbackCalls { get; private set; }

        public Task<ITransactionContext> BeginTransactionAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<ITransactionContext>(new FakeTransactionContext(this));
        }

        private sealed class FakeTransactionContext : ITransactionContext
        {
            private readonly FakeUnitOfWork _unitOfWork;

            public FakeTransactionContext(FakeUnitOfWork unitOfWork)
            {
                _unitOfWork = unitOfWork;
            }

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;

            public Task CommitAsync(CancellationToken cancellationToken = default)
            {
                _unitOfWork.CommitCalls++;
                return Task.CompletedTask;
            }

            public Task RollbackAsync(CancellationToken cancellationToken = default)
            {
                _unitOfWork.RollbackCalls++;
                return Task.CompletedTask;
            }
        }
    }
}
