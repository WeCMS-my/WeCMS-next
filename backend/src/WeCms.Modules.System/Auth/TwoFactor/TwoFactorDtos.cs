 namespace WeCms.Modules.System.Auth.TwoFactor;
 
 public sealed record TwoFactorSetupResponse(string Secret, string QrCodeUri, string[] BackupCodes);
 public sealed record TwoFactorEnableRequest(string Code);
 public sealed record TwoFactorDisableRequest(string Code);
 public sealed record TwoFactorVerifyRequest(string Username, string Code);
