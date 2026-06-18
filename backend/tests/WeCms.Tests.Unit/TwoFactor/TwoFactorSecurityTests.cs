using WeCms.Modules.System.TwoFactor;
using WeCms.Shared;

namespace WeCms.Tests.Unit.TwoFactor;

public sealed class TwoFactorSecurityTests
{
    [Fact]
    public void TotpVerify_AcceptsCurrentAndAdjacentWindow()
    {
        var service = new TotpService(new TwoFactorOptions("0123456789abcdef0123456789abcdef", "WeCMS", 30, 6, 1, 10));
        var secret = "JBSWY3DPEHPK3PXP";
        var now = DateTimeOffset.FromUnixTimeSeconds(1_800_000_000);
        var adjacentCode = service.GenerateCode(secret, now.AddSeconds(30));

        var result = service.Verify(secret, adjacentCode, now, lastTotpStep: null);

        Assert.True(result.IsValid);
        Assert.False(result.IsReplay);
        Assert.Equal(service.GetStep(now.AddSeconds(30)), result.UsedStep);
    }

    [Fact]
    public void TotpVerify_RejectsInvalidAndReplay()
    {
        var service = new TotpService(new TwoFactorOptions("0123456789abcdef0123456789abcdef", "WeCMS", 30, 6, 1, 10));
        var secret = "JBSWY3DPEHPK3PXP";
        var now = DateTimeOffset.FromUnixTimeSeconds(1_800_000_000);
        var code = service.GenerateCode(secret, now);
        var step = service.GetStep(now);

        Assert.False(service.Verify(secret, "000000", now, lastTotpStep: null).IsValid);

        var replay = service.Verify(secret, code, now, step);

        Assert.False(replay.IsValid);
        Assert.True(replay.IsReplay);
    }

    [Fact]
    public void SecretProtector_RoundTripsWithoutPlaintextCipher()
    {
        var protector = new SecretProtector(new TwoFactorOptions("0123456789abcdef0123456789abcdef", "WeCMS", 30, 6, 1, 10));
        var secret = "JBSWY3DPEHPK3PXP";

        var cipher = protector.Protect(secret);
        var plain = protector.Unprotect(cipher);

        Assert.NotEqual(secret, cipher);
        Assert.DoesNotContain(secret, cipher, StringComparison.Ordinal);
        Assert.Equal(secret, plain);
    }

    [Fact]
    public void RecoveryCodeService_StoresHashesAndConsumesOnce()
    {
        var service = new RecoveryCodeService(
            new TwoFactorOptions("0123456789abcdef0123456789abcdef", "WeCMS", 30, 6, 1, 10),
            new FixedEntropy());

        var bundle = service.GenerateCodes(count: 3);

        Assert.Equal(3, bundle.Codes.Count);
        Assert.Equal(3, bundle.Hashes.Count);
        Assert.DoesNotContain(bundle.Codes[0], bundle.Hashes[0], StringComparison.Ordinal);

        var firstUse = service.TryConsume(bundle.Codes[0], bundle.Hashes);
        var secondUse = service.TryConsume(bundle.Codes[0], firstUse.RemainingHashes);

        Assert.True(firstUse.Consumed);
        Assert.False(secondUse.Consumed);
        Assert.Equal(2, firstUse.RemainingHashes.Count);
    }

    [Fact]
    public void SecretProtector_RejectsShortKey()
    {
        var exception = Assert.Throws<ArgumentException>(() => new SecretProtector(new TwoFactorOptions("short", "WeCMS", 30, 6, 1, 10)));

        Assert.Contains("Secret protection key", exception.Message, StringComparison.Ordinal);
    }

    private sealed class FixedEntropy : IRecoveryCodeEntropy
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
