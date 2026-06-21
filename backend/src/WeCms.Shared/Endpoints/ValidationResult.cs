namespace WeCms.Shared.Endpoints;

public sealed record ValidationError(string Field, string Message);

public sealed record ValidationResult(IReadOnlyList<ValidationError> Errors)
{
    public bool IsValid => Errors.Count == 0;

    public static ValidationResult Valid()
    {
        return new ValidationResult([]);
    }

    public static ValidationResult Invalid(params ValidationError[] errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        return new ValidationResult(errors);
    }
}
