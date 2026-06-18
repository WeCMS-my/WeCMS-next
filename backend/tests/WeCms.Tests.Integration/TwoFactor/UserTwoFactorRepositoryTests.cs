using SqlSugar;
using WeCms.Modules.System.TwoFactor;
using WeCms.Persistence.Data;
using WeCms.Persistence.Migration;
using WeCms.Persistence.Modules.System.TwoFactor;

namespace WeCms.Tests.Integration.TwoFactor;

[Collection(nameof(SharedMySqlCollection))]
public sealed class UserTwoFactorRepositoryTests : PerTestDatabaseResetBase
{
    [DbFact]
    public async Task SetupEnableAndRecoveryCodeUpdate_UseUserTwoFactorSchema()
    {
        using var db = new SqlSugarClientFactory(IntegrationTestDatabase.GetConnectionString()).Create();
        await PrepareDatabaseAsync(db);
        var repository = new UserTwoFactorRepository(db);
        var now = new DateTimeOffset(2026, 6, 18, 0, 0, 0, TimeSpan.Zero);
        var userId = await InsertUserAsync(db, "two-factor-user");

        await repository.UpsertSetupAsync(
            new UserTwoFactorSetupRecord(
                userId,
                "cipher-text",
                ["hash-1", "hash-2"],
                now),
            CancellationToken.None);

        var setup = await repository.GetByUserIdAsync(userId, CancellationToken.None);
        Assert.NotNull(setup);
        Assert.False(setup.Enabled);
        Assert.Equal("cipher-text", setup.SecretCipher);
        Assert.Equal(["hash-1", "hash-2"], setup.RecoveryCodeHashes);
        Assert.Null(setup.LastTotpStep);

        await repository.EnableAsync(new UserTwoFactorEnableRecord(userId, 60000000, now.AddMinutes(1)), CancellationToken.None);

        var enabled = await repository.GetByUserIdAsync(userId, CancellationToken.None);
        Assert.NotNull(enabled);
        Assert.True(enabled.Enabled);
        Assert.Equal(60000000, enabled.LastTotpStep);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => repository.EnableAsync(new UserTwoFactorEnableRecord(userId, 60000000, now.AddMinutes(2)), CancellationToken.None));

        await repository.UpdateRecoveryCodesAsync(
            new UserTwoFactorRecoveryCodeUpdateRecord(userId, ["hash-2"], 1, now.AddMinutes(3)),
            CancellationToken.None);

        var updated = await repository.GetByUserIdAsync(userId, CancellationToken.None);
        Assert.NotNull(updated);
        Assert.Equal(["hash-2"], updated.RecoveryCodeHashes);
        Assert.Equal(1, updated.RecoveryCodesUsedCount);

        await repository.UpdateLastTotpStepAsync(new UserTwoFactorTotpStepUpdateRecord(userId, 60000001, now.AddMinutes(4)), CancellationToken.None);
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => repository.UpdateLastTotpStepAsync(new UserTwoFactorTotpStepUpdateRecord(userId, 60000001, now.AddMinutes(5)), CancellationToken.None));
    }

    [DbFact]
    public async Task ClearAsync_RemovesSensitiveTwoFactorState()
    {
        using var db = new SqlSugarClientFactory(IntegrationTestDatabase.GetConnectionString()).Create();
        await PrepareDatabaseAsync(db);
        var repository = new UserTwoFactorRepository(db);
        var now = new DateTimeOffset(2026, 6, 18, 0, 0, 0, TimeSpan.Zero);
        var userId = await InsertUserAsync(db, "clear-two-factor-user");

        await repository.UpsertSetupAsync(new UserTwoFactorSetupRecord(userId, "cipher-text", ["hash-1"], now), CancellationToken.None);
        await repository.EnableAsync(new UserTwoFactorEnableRecord(userId, 60000000, now), CancellationToken.None);

        await repository.ClearAsync(userId, now.AddMinutes(1), CancellationToken.None);

        var cleared = await repository.GetByUserIdAsync(userId, CancellationToken.None);
        Assert.NotNull(cleared);
        Assert.False(cleared.Enabled);
        Assert.Empty(cleared.SecretCipher);
        Assert.Null(cleared.ConfirmedAt);
        Assert.Null(cleared.LastTotpStep);
        Assert.Empty(cleared.RecoveryCodeHashes);
        Assert.True(cleared.ResetRequired);
    }

    private static async Task<long> InsertUserAsync(ISqlSugarClient db, string username)
    {
        await db.Ado.ExecuteCommandAsync(
            """
            INSERT INTO sys_user (username, display_name, password_hash, status, is_super_admin, must_change_password, security_stamp, permission_version, created_at, updated_at, deleted_at)
            VALUES (@username, @displayName, 'x', 'enabled', FALSE, FALSE, 'stamp', 0, UTC_TIMESTAMP(6), UTC_TIMESTAMP(6), NULL)
            """,
            new SugarParameter("@username", username),
            new SugarParameter("@displayName", username));

        return Convert.ToInt64(
            await db.Ado.GetScalarAsync("SELECT id FROM sys_user WHERE username = @username", new SugarParameter("@username", username)),
            global::System.Globalization.CultureInfo.InvariantCulture);
    }

    private static string RepoPath(params string[] segments)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "backend", "WeCms.slnx")))
            {
                return Path.Combine([directory.FullName, .. segments]);
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }
}
