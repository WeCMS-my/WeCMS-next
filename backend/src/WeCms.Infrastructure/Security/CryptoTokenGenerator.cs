using System.Security.Cryptography;
using WeCms.Shared.Security;

namespace WeCms.Infrastructure.Security;

public sealed class CryptoTokenGenerator : ITokenGenerator
{
    public string GenerateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes);
    }
}
