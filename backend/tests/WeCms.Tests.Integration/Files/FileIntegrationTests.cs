using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using SqlSugar;
using Microsoft.Extensions.Logging.Abstractions;
using WeCms.Infrastructure.Files;
using WeCms.Modules.System.Files;
using WeCms.Modules.System.Auth;
using WeCms.Persistence.Data;
using WeCms.Persistence.Migration;
using WeCms.Persistence.Modules.System.Files;
using WeCms.Shared;
using WeCms.Shared.Security;

namespace WeCms.Tests.Integration.Files;

[Collection(nameof(SharedMySqlCollection))]
public sealed class FileIntegrationTests : PerTestDatabaseResetBase
{
    [DbFact]
    public async Task CreateAsync_StoresAndDownloadsFileWithValidatedMetadata()
    {
        var baseConnectionString = RequiredConnectionString();


        var fixedClock = new FixedClock(new DateTimeOffset(2026, 6, 16, 0, 0, 0, TimeSpan.Zero));
        var storageDirectory = Path.Combine(AppContext.BaseDirectory, "storage", "files");

        try
        {
            using var db = new SqlSugarClientFactory(baseConnectionString).Create();
            await PrepareDatabaseAsync(db);

            var actorUserId = Scalar<long>(db, "SELECT id FROM sys_user WHERE username = 'admin' LIMIT 1");
            if (actorUserId <= 0)
            {
                db.Ado.ExecuteCommand(
                    "INSERT INTO sys_user (username, display_name, password_hash, status, is_super_admin, must_change_password, security_stamp, permission_version, created_at, updated_at, deleted_at) VALUES ('admin', 'admin', 'x', 'enabled', true, false, 'seed', 0, NOW(6), NOW(6), NULL)");
                actorUserId = Scalar<long>(db, "SELECT id FROM sys_user WHERE username = 'admin' LIMIT 1");
            }

            var service = new FileService(
                new FileRepository(db, new SecurityEventClassifier()),
                new LocalFileStorage(),
                new DeterministicObjectKeyGenerator(),
                new FileUploadPolicyResolver([new AvatarUploadPolicy(), new ImageUploadPolicy(), new DocumentUploadPolicy()]), NullLogger<FileService>.Instance);

            var content = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x32, 0x0A };
            var formFile = CreateFormFile("invoice.pdf", content);
            var request = new CreateFileRequest(
                "invoice.pdf",
                "application/pdf",
                content.Length,
                ComputeSha256(content));

            var result = await service.CreateAsync(
                request,
                formFile,
                new FileRequestContext(actorUserId, "admin", "192.168.101.199", "integration", "it", fixedClock.UtcNow),
                CancellationToken.None);

            Assert.True(result.Id > 0);

            var storedCount = Scalar<int>(db, "SELECT COUNT(1) FROM sys_file WHERE id = @id", new SugarParameter("@id", result.Id));
            Assert.Equal(1, storedCount);

            var storedStatus = Scalar<string>(db, "SELECT status FROM sys_file WHERE id = @id", new SugarParameter("@id", result.Id));
            Assert.Equal("active", storedStatus);
            Assert.True(Directory.Exists(storageDirectory));

            var payload = await service.GetDownloadPayloadAsync(result.Id, false, new FileRequestContext(actorUserId, "admin", "192.168.101.199", "integration", "it", fixedClock.UtcNow), CancellationToken.None);
            using var memory = new MemoryStream();
            await payload.Content.CopyToAsync(memory, CancellationToken.None);
            Assert.Equal(content.Length, memory.Length);

            await service.DeleteAsync(result.Id, new FileRequestContext(actorUserId, "admin", "192.168.101.199", "integration", "it", fixedClock.UtcNow), CancellationToken.None);
            var deletedStatus = Scalar<string>(db, "SELECT status FROM sys_file WHERE id = @id", new SugarParameter("@id", result.Id));
            Assert.Equal("deleted", deletedStatus);
            Assert.Equal(0L, Scalar<long>(db, "SELECT COUNT(1) FROM sys_file WHERE id = @id AND deleted_at IS NULL", new SugarParameter("@id", result.Id)));
        }
        finally
        {
            try
            {
                if (Directory.Exists(storageDirectory))
                {
                    Directory.Delete(storageDirectory, recursive: true);
                }
            }
            catch
            {
                // Ignore cleanup failures to keep environment stable.
            }
        }
    }
    [DbFact]
    public async Task CreateAsync_RejectsSha256MismatchWithoutPersistingMetadata()
    {
        var baseConnectionString = RequiredConnectionString();


        try
        {
            using var db = new SqlSugarClientFactory(baseConnectionString).Create();
            await PrepareDatabaseAsync(db);

            var actorUserId = Scalar<long>(db, "SELECT id FROM sys_user WHERE username = 'admin' LIMIT 1");
            if (actorUserId <= 0)
            {
                db.Ado.ExecuteCommand(
                    "INSERT INTO sys_user (username, display_name, password_hash, status, is_super_admin, must_change_password, security_stamp, permission_version, created_at, updated_at, deleted_at) VALUES ('admin', 'admin', 'x', 'enabled', true, false, 'seed', 0, NOW(6), NOW(6), NULL)");
                actorUserId = Scalar<long>(db, "SELECT id FROM sys_user WHERE username = 'admin' LIMIT 1");
            }

            var service = new FileService(
                new FileRepository(db, new SecurityEventClassifier()),
                new LocalFileStorage(),
                new DeterministicObjectKeyGenerator(),
                new FileUploadPolicyResolver([new AvatarUploadPolicy(), new ImageUploadPolicy(), new DocumentUploadPolicy()]), NullLogger<FileService>.Instance);

            var content = new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D, 0x31, 0x2E, 0x32, 0x0A };
            var formFile = CreateFormFile("invoice.pdf", content);

            var exception = await Assert.ThrowsAsync<DomainException>(() =>
                service.CreateAsync(
                    new CreateFileRequest("invoice.pdf", "application/pdf", content.Length, new string('a', 64)),
                    formFile,
                    new FileRequestContext(actorUserId, "admin", "192.168.101.199", "integration", "it", new DateTimeOffset(2026, 6, 16, 0, 0, 0, TimeSpan.Zero)),
                    CancellationToken.None));

            Assert.Equal(ApiCodes.ValidationError, exception.Code);
            Assert.Equal(0, Scalar<int>(db, "SELECT COUNT(1) FROM sys_file WHERE original_name = 'invoice.pdf'"));
            Assert.Equal(1, Scalar<int>(db, "SELECT COUNT(1) FROM sys_security_event WHERE event_type = 'file_upload_rejected' AND source = 'file'"));
        }
        finally
        {
        }
    }
    private static string RequiredConnectionString()
    {
        return IntegrationTestDatabase.GetConnectionString();
    }
    private static string RepoPath(params string[] segments)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "database"))
                && File.Exists(Path.Combine(directory.FullName, "backend", "WeCms.slnx")))
            {
                return Path.Combine([directory.FullName, .. segments]);
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate repository root.");
    }
    private static T Scalar<T>(ISqlSugarClient db, string sql, params SugarParameter[] parameters)
    {
        var scalar = db.Ado.GetScalar(sql, parameters);
        if (scalar == null || scalar == DBNull.Value)
        {
            return default!;
        }

        if (scalar is T value)
        {
            return value;
        }

        if (typeof(T) == typeof(string))
        {
            return (T)(object)scalar.ToString()!;
        }

        return (T)Convert.ChangeType(scalar, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
    }
    private static string ComputeSha256(byte[] bytes)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
    private static IFormFile CreateFormFile(string fileName, byte[] content)
    {
        return new FormFile(new MemoryStream(content), 0, content.Length, "file", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "application/pdf"
        };
    }

    private sealed class FixedClock : IAuthClock
    {
        public FixedClock(DateTimeOffset now)
        {
            UtcNow = now;
        }

        public DateTimeOffset UtcNow { get; }
    }

    private sealed class DeterministicObjectKeyGenerator : IFileObjectKeyGenerator
    {
        public string GenerateObjectKey(DateTimeOffset now, string fileExt)
        {
            return "2026/06/integration-test" + fileExt;
        }
    }
}
