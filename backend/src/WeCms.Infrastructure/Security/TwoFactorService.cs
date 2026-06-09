 using System.Security.Cryptography;
 using System.Text;
 using WeCms.Shared.Contracts;
 
 namespace WeCms.Infrastructure.Security;
 
 public sealed class TwoFactorService : ITwoFactorService
{
    private readonly IClock _clock;
    private const string Base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
    private const int SecretLength = 20;
    private const int BackupCodeCount = 8;

    public TwoFactorService(IClock clock) { _clock = clock; }
 
     public string GenerateSecret()
     {
         var bytes = RandomNumberGenerator.GetBytes(SecretLength);
         return Base32Encode(bytes);
     }
 
     public string GenerateQrCodeUri(string username, string issuer, string secret)
     {
         var encodedIssuer = Uri.EscapeDataString(issuer);
         var encodedLabel = Uri.EscapeDataString($"{issuer}:{username}");
         return $"otpauth://totp/{encodedLabel}?secret={secret}&issuer={encodedIssuer}&algorithm=SHA1&digits=6&period=30";
     }
 
     public string GenerateCurrentCode(string secret) => ComputeTotp(secret, GetCurrentTimeSlice());
 
     public bool Verify(string secret, string code)
     {
         if (code.Length != 6 || !long.TryParse(code, out _)) return false;
         var slice = GetCurrentTimeSlice();
         return ComputeTotp(secret, slice) == code
             || ComputeTotp(secret, slice - 1) == code
             || ComputeTotp(secret, slice + 1) == code;
     }
 
     public (string[], string[]) GenerateBackupCodes()
     {
         var plain = new string[BackupCodeCount];
         var hashed = new string[BackupCodeCount];
         for (var i = 0; i < BackupCodeCount; i++)
         {
             var code = $"{RandomNumberGenerator.GetInt32(0, 10000):D4}-{RandomNumberGenerator.GetInt32(0, 10000):D4}";
             plain[i] = code;
             hashed[i] = HashBackupCode(code);
         }
         return (plain, hashed);
     }
 
     public bool VerifyBackupCode(string code, string[] hashedCodes)
     {
         var hash = HashBackupCode(code);
         return hashedCodes.Contains(hash);
     }
 
     private static string ComputeTotp(string secret, long timeSlice)
     {
         var key = Base32Decode(secret);
         var counter = BitConverter.GetBytes(timeSlice);
         if (BitConverter.IsLittleEndian) Array.Reverse(counter);
         var hmac = HMACSHA1.HashData(key, counter);
         var offset = hmac[^1] & 0x0F;
         var binary = ((hmac[offset] & 0x7F) << 24)
                    | ((hmac[offset + 1] & 0xFF) << 16)
                    | ((hmac[offset + 2] & 0xFF) << 8)
                    | (hmac[offset + 3] & 0xFF);
         return (binary % 1_000_000).ToString("D6");
     }
 
     private long GetCurrentTimeSlice()
        => _clock.UtcNow.ToUnixTimeSeconds() / 30;
 
     private static string Base32Encode(byte[] data)
     {
         var result = new StringBuilder();
         var bits = 0;
         var value = 0;
         foreach (var b in data)
         {
             value = (value << 8) | b;
             bits += 8;
             while (bits >= 5)
             {
                 result.Append(Base32Chars[(value >> (bits - 5)) & 0x1F]);
                 bits -= 5;
             }
         }
         if (bits > 0) result.Append(Base32Chars[(value << (5 - bits)) & 0x1F]);
         return result.ToString();
     }
 
     private static byte[] Base32Decode(string encoded)
     {
         encoded = encoded.TrimEnd('=').ToUpperInvariant();
         var bytes = new List<byte>();
         var bits = 0;
         var value = 0;
         foreach (var c in encoded)
         {
             var idx = Base32Chars.IndexOf(c);
             if (idx < 0) continue;
             value = (value << 5) | idx;
             bits += 5;
             if (bits >= 8)
             {
                 bytes.Add((byte)((value >> (bits - 8)) & 0xFF));
                 bits -= 8;
             }
         }
         return bytes.ToArray();
     }
 
     private static string HashBackupCode(string code)
     {
         var hash = SHA256.HashData(Encoding.UTF8.GetBytes(code));
         return Convert.ToHexString(hash);
     }
 }
