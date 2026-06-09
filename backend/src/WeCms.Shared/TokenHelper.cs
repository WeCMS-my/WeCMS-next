using System.Security.Cryptography;
using System.Text;

namespace WeCms.Shared;

public static class TokenHelper
{
    public static string HashToken(string token) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
