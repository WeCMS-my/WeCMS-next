using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace WeCms.Modules.Identity.Services;

public sealed class TotpService : ITotpService
{
    private const int SecretBytes = 20;
    private readonly TwoFactorOptions _options;
    private readonly ITotpSecretEntropy? _entropy;

    public TotpService(TwoFactorOptions options)
        : this(options, null)
    {
    }

    public TotpService(TwoFactorOptions options, ITotpSecretEntropy? entropy)
    {
        ValidateOptions(options);
        _options = options;
        _entropy = entropy;
    }

    public string GenerateSecret()
    {
        var bytes = _entropy?.GetBytes(SecretBytes) ?? RandomNumberGenerator.GetBytes(SecretBytes);
        return Base32Encode(bytes);
    }

    public string GenerateCode(string secret, DateTimeOffset now)
    {
        return GenerateCode(secret, GetStep(now));
    }

    public TotpVerificationResult Verify(string secret, string code, DateTimeOffset now, long? lastTotpStep)
    {
        var normalizedCode = NormalizeCode(code);
        if (normalizedCode is null)
        {
            return new TotpVerificationResult(false, false, null);
        }

        var currentStep = GetStep(now);
        for (var offset = -_options.AllowedWindowSteps; offset <= _options.AllowedWindowSteps; offset++)
        {
            var step = currentStep + offset;
            if (step < 0)
            {
                continue;
            }

            var expected = GenerateCode(secret, step);
            if (!FixedTimeEquals(expected, normalizedCode))
            {
                continue;
            }

            var replay = lastTotpStep.HasValue && lastTotpStep.Value >= step;
            return new TotpVerificationResult(!replay, replay, step);
        }

        return new TotpVerificationResult(false, false, null);
    }

    public long GetStep(DateTimeOffset now)
    {
        return now.ToUnixTimeSeconds() / _options.PeriodSeconds;
    }

    public string BuildOtpAuthUri(string secret, string accountName)
    {
        var issuer = Uri.EscapeDataString(_options.Issuer);
        var account = Uri.EscapeDataString(accountName);
        return $"otpauth://totp/{issuer}:{account}?secret={secret}&issuer={issuer}&algorithm=SHA1&digits={_options.CodeDigits}&period={_options.PeriodSeconds}";
    }

    private string GenerateCode(string secret, long step)
    {
        var key = Base32Decode(secret);
        Span<byte> counter = stackalloc byte[8];
        global::System.Buffers.Binary.BinaryPrimitives.WriteInt64BigEndian(counter, step);

        using var hmac = new HMACSHA1(key);
        var hash = hmac.ComputeHash(counter.ToArray());
        var offset = hash[^1] & 0x0f;
        var binary = ((hash[offset] & 0x7f) << 24)
            | ((hash[offset + 1] & 0xff) << 16)
            | ((hash[offset + 2] & 0xff) << 8)
            | (hash[offset + 3] & 0xff);
        var divisor = (int)Math.Pow(10, _options.CodeDigits);
        var value = binary % divisor;
        return value.ToString(new string('0', _options.CodeDigits), CultureInfo.InvariantCulture);
    }

    private string? NormalizeCode(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        var normalized = code.Trim();
        if (normalized.Length != _options.CodeDigits || normalized.Any(static ch => ch < '0' || ch > '9'))
        {
            return null;
        }

        return normalized;
    }

    private static bool FixedTimeEquals(string left, string right)
    {
        return CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(left), Encoding.ASCII.GetBytes(right));
    }

    private static string Base32Encode(byte[] bytes)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var output = new StringBuilder((bytes.Length + 4) / 5 * 8);
        var buffer = 0;
        var bitsLeft = 0;

        foreach (var value in bytes)
        {
            buffer = (buffer << 8) | value;
            bitsLeft += 8;
            while (bitsLeft >= 5)
            {
                output.Append(alphabet[(buffer >> (bitsLeft - 5)) & 31]);
                bitsLeft -= 5;
            }
        }

        if (bitsLeft > 0)
        {
            output.Append(alphabet[(buffer << (5 - bitsLeft)) & 31]);
        }

        return output.ToString();
    }

    private static byte[] Base32Decode(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("TOTP secret is required.", nameof(value));
        }

        var normalized = value.Trim().TrimEnd('=').ToUpperInvariant();
        var buffer = 0;
        var bitsLeft = 0;
        var bytes = new List<byte>();

        foreach (var ch in normalized)
        {
            var index = ch switch
            {
                >= 'A' and <= 'Z' => ch - 'A',
                >= '2' and <= '7' => ch - '2' + 26,
                _ => -1
            };
            if (index < 0)
            {
                throw new ArgumentException("TOTP secret must be Base32 encoded.", nameof(value));
            }

            buffer = (buffer << 5) | index;
            bitsLeft += 5;
            if (bitsLeft >= 8)
            {
                bytes.Add((byte)((buffer >> (bitsLeft - 8)) & 0xff));
                bitsLeft -= 8;
            }
        }

        return bytes.ToArray();
    }

    private static void ValidateOptions(TwoFactorOptions options)
    {
        if (options.PeriodSeconds <= 0)
        {
            throw new ArgumentException("TOTP period must be positive.", nameof(options));
        }

        if (options.CodeDigits is < 6 or > 8)
        {
            throw new ArgumentException("TOTP code digits must be between 6 and 8.", nameof(options));
        }

        if (options.AllowedWindowSteps < 0)
        {
            throw new ArgumentException("TOTP allowed window must not be negative.", nameof(options));
        }
    }
}

public sealed class SecretProtector : ISecretProtector
{
    private const string Prefix = "wecms.2fa-secret.v1";
    private const int NonceBytes = 12;
    private const int TagBytes = 16;
    private readonly byte[] _key;

    public SecretProtector(TwoFactorOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.SecretProtectionKey) || options.SecretProtectionKey.Length < 32)
        {
            throw new ArgumentException("Secret protection key must be at least 32 characters.", nameof(options));
        }

        _key = SHA256.HashData(Encoding.UTF8.GetBytes(options.SecretProtectionKey));
    }

    public string Protect(string secret)
    {
        if (string.IsNullOrWhiteSpace(secret))
        {
            throw new ArgumentException("Secret is required.", nameof(secret));
        }

        var nonce = RandomNumberGenerator.GetBytes(NonceBytes);
        var plain = Encoding.UTF8.GetBytes(secret);
        var cipher = new byte[plain.Length];
        var tag = new byte[TagBytes];

        using var aes = new AesGcm(_key, TagBytes);
        aes.Encrypt(nonce, plain, cipher, tag);

        return string.Join('.', Prefix, Base64UrlEncode(nonce), Base64UrlEncode(tag), Base64UrlEncode(cipher));
    }

    public string Unprotect(string cipher)
    {
        var parts = cipher.Split('.');
        if (parts.Length != 6 || string.Join('.', parts[0], parts[1], parts[2]) != Prefix)
        {
            throw new ArgumentException("Invalid protected secret format.", nameof(cipher));
        }

        var nonce = Base64UrlDecode(parts[3]);
        var tag = Base64UrlDecode(parts[4]);
        var encrypted = Base64UrlDecode(parts[5]);
        var plain = new byte[encrypted.Length];

        using var aes = new AesGcm(_key, TagBytes);
        aes.Decrypt(nonce, encrypted, tag, plain);

        return Encoding.UTF8.GetString(plain);
    }

    private static string Base64UrlEncode(byte[] value)
    {
        return Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static byte[] Base64UrlDecode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded = padded.PadRight(padded.Length + (4 - padded.Length % 4) % 4, '=');
        return Convert.FromBase64String(padded);
    }
}

public sealed class RecoveryCodeService : IRecoveryCodeService
{
    private const int RecoveryCodeBytes = 10;
    private const string Prefix = "wecms.2fa-recovery.v1";
    private readonly byte[] _key;
    private readonly IRecoveryCodeEntropy? _entropy;

    public RecoveryCodeService(TwoFactorOptions options, IRecoveryCodeEntropy? entropy = null)
    {
        if (string.IsNullOrWhiteSpace(options.SecretProtectionKey) || options.SecretProtectionKey.Length < 32)
        {
            throw new ArgumentException("Secret protection key must be at least 32 characters.", nameof(options));
        }

        _key = SHA256.HashData(Encoding.UTF8.GetBytes(options.SecretProtectionKey));
        _entropy = entropy;
    }

    public RecoveryCodeBundle GenerateCodes(int count)
    {
        if (count <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Recovery code count must be positive.");
        }

        var codes = new List<string>(count);
        var hashes = new List<string>(count);
        for (var index = 0; index < count; index++)
        {
            var code = FormatCode((_entropy?.GetBytes(RecoveryCodeBytes) ?? RandomNumberGenerator.GetBytes(RecoveryCodeBytes)));
            codes.Add(code);
            hashes.Add(Hash(code));
        }

        return new RecoveryCodeBundle(codes, hashes);
    }

    public RecoveryCodeConsumptionResult TryConsume(string code, IReadOnlyList<string> hashes)
    {
        if (string.IsNullOrWhiteSpace(code) || hashes.Count == 0)
        {
            return new RecoveryCodeConsumptionResult(false, hashes);
        }

        var normalized = NormalizeCode(code);
        var remaining = new List<string>(hashes.Count);
        var consumed = false;

        foreach (var hash in hashes)
        {
            if (!consumed && Verify(normalized, hash))
            {
                consumed = true;
                continue;
            }

            remaining.Add(hash);
        }

        return new RecoveryCodeConsumptionResult(consumed, remaining);
    }

    private string Hash(string code)
    {
        using var hmac = new HMACSHA256(_key);
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(NormalizeCode(code)));
        return $"{Prefix}.{Convert.ToHexString(hash).ToLowerInvariant()}";
    }

    private bool Verify(string code, string hash)
    {
        if (!hash.StartsWith($"{Prefix}.", StringComparison.Ordinal))
        {
            return false;
        }

        var actual = Hash(code);
        return CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(actual), Encoding.ASCII.GetBytes(hash));
    }

    private static string FormatCode(byte[] bytes)
    {
        var text = Convert.ToHexString(bytes).ToUpperInvariant();
        return string.Join('-', Enumerable.Range(0, 4).Select(index => text.Substring(index * 5, 5)));
    }

    private static string NormalizeCode(string code)
    {
        return code.Trim().Replace("-", string.Empty, StringComparison.Ordinal).ToUpperInvariant();
    }
}

public sealed class TwoFactorEntropy : ITotpSecretEntropy, IRecoveryCodeEntropy
{
    public byte[] GetBytes(int count)
    {
        return RandomNumberGenerator.GetBytes(count);
    }
}
