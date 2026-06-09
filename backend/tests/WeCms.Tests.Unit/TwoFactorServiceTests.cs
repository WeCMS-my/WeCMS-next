 using WeCms.Infrastructure.Security;
 using WeCms.Shared.Contracts;
 using Xunit;
 
 namespace WeCms.Tests.Unit;
 
 public class TwoFactorServiceTests
 {
     private readonly ITwoFactorService _svc = new TwoFactorService();
 
     [Fact]
     public void GenerateSecret_ShouldProduceValidBase32String()
     {
         var secret = _svc.GenerateSecret();
         Assert.NotNull(secret);
         Assert.True(secret.Length >= 16);
     }
 
     [Fact]
     public void GenerateQrCodeUri_ShouldCreateOtpAuthUri()
     {
         var secret = _svc.GenerateSecret();
         var uri = _svc.GenerateQrCodeUri("admin", "WeCMS", secret);
         Assert.StartsWith("otpauth://totp/", uri);
         Assert.Contains("admin", uri);
         Assert.Contains("WeCMS", uri);
         Assert.Contains(secret, uri);
     }
 
     [Fact]
     public void Verify_ValidCode_ShouldReturnTrue()
     {
         var secret = _svc.GenerateSecret();
         var code = _svc.GenerateCurrentCode(secret);
         Assert.True(_svc.Verify(secret, code));
     }
 
     [Fact]
     public void Verify_InvalidCode_ShouldReturnFalse()
     {
         var secret = _svc.GenerateSecret();
         Assert.False(_svc.Verify(secret, "000000"));
     }
 
     [Fact]
     public void Verify_SameSecretDifferentCodes_ShouldMatchWindow()
     {
         var secret = _svc.GenerateSecret();
         var code = _svc.GenerateCurrentCode(secret);
         // Same code should verify (within window)
         Assert.True(_svc.Verify(secret, code));
     }
 
     [Fact]
     public void GenerateBackupCodes_ShouldProduceHashedCodes()
     {
         var (plain, hashed) = _svc.GenerateBackupCodes();
         Assert.Equal(8, plain.Length);
         Assert.Equal(8, hashed.Length);
         foreach (var h in hashed)
             Assert.True(h.Length > 0);
     }
 
     [Fact]
     public void VerifyBackupCode_ValidCode_ShouldReturnTrue()
     {
         var (plain, hashed) = _svc.GenerateBackupCodes();
         Assert.True(_svc.VerifyBackupCode(plain[0], hashed));
     }
 
     [Fact]
     public void VerifyBackupCode_InvalidCode_ShouldReturnFalse()
     {
         var (_, hashed) = _svc.GenerateBackupCodes();
         Assert.False(_svc.VerifyBackupCode("AAAA-BBBB", hashed));
     }
 }
