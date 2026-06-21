using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using WeCms.Shared;
using WeCms.Shared.Endpoints;

namespace WeCms.Api.Endpoints;

public sealed class ValidationEndpointFilter<TRequest> : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var validators = context.HttpContext.RequestServices.GetServices<IRequestValidator<TRequest>>().ToArray();
        if (validators.Length == 0)
        {
            return await next(context);
        }

        var request = context.Arguments.OfType<TRequest>().FirstOrDefault();
        if (request is null)
        {
            throw new InvalidOperationException(
                $"Request argument of type {typeof(TRequest).Name} was not found for validation.");
        }

        var errors = new List<ValidationError>();
        foreach (var validator in validators)
        {
            var result = await validator.ValidateAsync(request, context.HttpContext.RequestAborted);
            errors.AddRange(result.Errors);
        }

        if (errors.Count == 0)
        {
            return await next(context);
        }

        return Results.Json(
            ApiResult<object>.Error(
                ApiCodes.ValidationError,
                "Validation failed.",
                context.HttpContext.TraceIdentifier,
                ToFieldErrors(errors)),
            statusCode: StatusCodes.Status400BadRequest);
    }

    private static IReadOnlyDictionary<string, string[]> ToFieldErrors(IReadOnlyList<ValidationError> errors)
    {
        return errors
            .GroupBy(static error => error.Field, StringComparer.Ordinal)
            .ToDictionary(
                static group => group.Key,
                static group => group.Select(static error => error.Message).ToArray(),
                StringComparer.Ordinal);
    }
}
