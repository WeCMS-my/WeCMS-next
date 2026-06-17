using System.Security.Cryptography;

namespace WeCms.Modules.System.Files;

public sealed class FileObjectKeyGenerator : IFileObjectKeyGenerator
{
    private const int SeedByteCount = 16;

    public string GenerateObjectKey(DateTimeOffset now, string fileExt)
    {
        var fileKey = Convert.ToHexString(RandomNumberGenerator.GetBytes(SeedByteCount)).ToLowerInvariant();
        return $"{now.UtcDateTime:yyyy}/{now.UtcDateTime:MM}/{fileKey}{fileExt.ToLowerInvariant()}";
    }
}
