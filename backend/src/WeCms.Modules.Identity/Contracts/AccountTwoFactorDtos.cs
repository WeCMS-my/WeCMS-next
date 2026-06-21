namespace WeCms.Modules.Identity.Contracts;

public sealed record AccountTwoFactorStatusResponse(
    bool Enabled,
    DateTimeOffset? ConfirmedAt,
    int RecoveryCodesRemaining,
    bool ResetRequired);

public sealed record AccountTwoFactorSetupResponse(
    string Secret,
    string OtpAuthUri,
    IReadOnlyList<string> RecoveryCodes);

public sealed record AccountTwoFactorConfirmRequest(string Code);

public sealed record AccountTwoFactorDisableRequest(string? CurrentPassword, string? Code);

public sealed record AccountTwoFactorRegenerateRecoveryCodesRequest(string? CurrentPassword, string? Code);

public sealed record AccountTwoFactorRecoveryCodesResponse(IReadOnlyList<string> RecoveryCodes);
