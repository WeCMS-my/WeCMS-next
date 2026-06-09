 namespace WeCms.Shared.Contracts;
 
 public interface ITwoFactorService
 {
     string GenerateSecret();
     string GenerateQrCodeUri(string username, string issuer, string secret);
     string GenerateCurrentCode(string secret);
     bool Verify(string secret, string code);
     (string[] plain, string[] hashed) GenerateBackupCodes();
     bool VerifyBackupCode(string code, string[] hashedCodes);
 }
