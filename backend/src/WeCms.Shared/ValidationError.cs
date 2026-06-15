namespace WeCms.Shared;

public sealed record ValidationError(
    string Field,
    IReadOnlyList<string> Messages);
