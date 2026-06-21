using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using WeCms.Api.Endpoints;
using WeCms.Shared;
using WeCms.Shared.Endpoints;

namespace WeCms.Tests.Unit.Api;

public sealed class ValidationEndpointFilterTests
{
    [Fact]
    public async Task ValidationEndpointFilter_ReturnsValidationError_WhenInvalid()
    {
        var request = new SampleRequest("");
        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IRequestValidator<SampleRequest>>(new SampleRequestValidator(false))
            .BuildServiceProvider();
        var httpContext = CreateHttpContext(services);
        var context = new TestEndpointFilterInvocationContext(httpContext, request);
        var filter = new ValidationEndpointFilter<SampleRequest>();

        var result = await filter.InvokeAsync(context, _ => throw new InvalidOperationException("Next must not run."));

        var response = await ExecuteResultAsync(result, httpContext);
        Assert.Equal(StatusCodes.Status400BadRequest, response.StatusCode);
        Assert.Equal(ApiCodes.ValidationError, response.Code);
        Assert.Equal("Validation failed.", response.Message);
        Assert.Equal("name", response.Field);
        Assert.Equal("Name is required.", response.FieldMessage);
    }

    [Fact]
    public async Task ValidationEndpointFilter_CallsNext_WhenValid()
    {
        var request = new SampleRequest("valid");
        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IRequestValidator<SampleRequest>>(new SampleRequestValidator(true))
            .AddSingleton<IRequestValidator<SampleRequest>>(new SampleRequestValidator(true))
            .BuildServiceProvider();
        var httpContext = CreateHttpContext(services);
        var context = new TestEndpointFilterInvocationContext(httpContext, request);
        var filter = new ValidationEndpointFilter<SampleRequest>();
        var called = false;

        var result = await filter.InvokeAsync(context, _ =>
        {
            called = true;
            return ValueTask.FromResult<object?>("ok");
        });

        Assert.True(called);
        Assert.Equal("ok", result);
    }

    [Fact]
    public async Task ValidationEndpointFilter_DoesNotThrow_WhenNoValidator()
    {
        var request = new SampleRequest("valid");
        var httpContext = CreateHttpContext(new ServiceCollection().AddLogging().BuildServiceProvider());
        var context = new TestEndpointFilterInvocationContext(httpContext, request);
        var filter = new ValidationEndpointFilter<SampleRequest>();
        var called = false;

        var result = await filter.InvokeAsync(context, _ =>
        {
            called = true;
            return ValueTask.FromResult<object?>("ok");
        });

        Assert.True(called);
        Assert.Equal("ok", result);
    }

    [Fact]
    public async Task ValidationEndpointFilter_Throws_WhenValidatorExistsButRequestMissing()
    {
        var services = new ServiceCollection()
            .AddLogging()
            .AddSingleton<IRequestValidator<SampleRequest>>(new SampleRequestValidator(true))
            .BuildServiceProvider();
        var httpContext = CreateHttpContext(services);
        var context = new TestEndpointFilterInvocationContext(httpContext);
        var filter = new ValidationEndpointFilter<SampleRequest>();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await filter.InvokeAsync(context, _ => ValueTask.FromResult<object?>("ok")));

        Assert.Equal("Request argument of type SampleRequest was not found for validation.", exception.Message);
    }

    private static DefaultHttpContext CreateHttpContext(IServiceProvider services)
    {
        var httpContext = new DefaultHttpContext
        {
            RequestServices = services,
            TraceIdentifier = "trace-validation"
        };
        httpContext.Response.Body = new MemoryStream();

        return httpContext;
    }

    private static async Task<(int StatusCode, int Code, string? Message, string? Field, string? FieldMessage)> ExecuteResultAsync(
        object? result,
        DefaultHttpContext httpContext)
    {
        var typedResult = Assert.IsAssignableFrom<IResult>(result);
        await typedResult.ExecuteAsync(httpContext);
        httpContext.Response.Body.Position = 0;
        using var document = await JsonDocument.ParseAsync(httpContext.Response.Body, default, TestContext.Current.CancellationToken);
        var root = document.RootElement;
        var fieldError = root.GetProperty("fieldErrors").GetProperty("name");

        return (
            httpContext.Response.StatusCode,
            root.GetProperty("code").GetInt32(),
            root.GetProperty("msg").GetString(),
            "name",
            fieldError[0].GetString());
    }

    private sealed record SampleRequest(string Name);

    private sealed class SampleRequestValidator(bool isValid) : IRequestValidator<SampleRequest>
    {
        public ValueTask<ValidationResult> ValidateAsync(SampleRequest request, CancellationToken cancellationToken)
        {
            return ValueTask.FromResult(isValid
                ? ValidationResult.Valid()
                : ValidationResult.Invalid(new ValidationError("name", "Name is required.")));
        }
    }

    private sealed class TestEndpointFilterInvocationContext : EndpointFilterInvocationContext
    {
        public TestEndpointFilterInvocationContext(HttpContext httpContext, params object?[] arguments)
        {
            HttpContext = httpContext;
            Arguments = arguments;
        }

        public override HttpContext HttpContext { get; }

        public override IList<object?> Arguments { get; }

        public override T GetArgument<T>(int index)
        {
            return (T)Arguments[index]!;
        }
    }
}
