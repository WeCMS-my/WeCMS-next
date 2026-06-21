namespace WeCms.Shared.Endpoints;

public interface IRequestValidator<in TRequest>
{
    ValueTask<ValidationResult> ValidateAsync(TRequest request, CancellationToken cancellationToken);
}
