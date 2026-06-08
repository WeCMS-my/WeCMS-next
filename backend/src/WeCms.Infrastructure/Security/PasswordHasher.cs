using WeCms.Shared.Contracts;
using System.Security.Cryptography;

namespace WeCms.Infrastructure.Security;

public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 600_000;
    private const string Algorithm = "pbkdf2-sha256";
    private const int Version = 1;

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);
        return $"wecms.{Algorithm}.v{Version}.{Iterations}.{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
    }

    public bool Verify(string password, string hash)
    {
        var parts = hash.Split('.');
        if (parts.Length != 6 || parts[0] != "wecms") return false;
        if (parts[1] != Algorithm) return false;

        var iterations = int.Parse(parts[3]);
        var salt = Convert.FromBase64String(parts[4]);
        var expectedHash = Convert.FromBase64String(parts[5]);
        var actualHash = Rfc2898DeriveBytes.Pbkdf2(
            password, salt, iterations, HashAlgorithmName.SHA256, expectedHash.Length);
        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}