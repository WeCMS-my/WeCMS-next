namespace WeCms.Shared;

public sealed record ValidationError(
    string Field,
    string Message);
