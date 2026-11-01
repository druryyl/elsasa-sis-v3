namespace Elsasa.Sis.IntegrationTests.UnitTests.Endpoints;

using Elsasa.Sis.Shared.Endpoints;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.DependencyInjection;

public sealed class ValidationFilterTests
{
    [Fact]
    public async Task InvokeAsync_Should_ThrowArgumentNullException_When_ContextIsNull()
    {
        var filter = new ValidationFilter<SampleValidationRequest>(new SampleValidationRequestValidator());

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            filter.InvokeAsync(null!, _ => ValueTask.FromResult<object?>("ok")).AsTask());
    }

    [Fact]
    public async Task InvokeAsync_Should_ThrowArgumentNullException_When_NextIsNull()
    {
        var filter = new ValidationFilter<SampleValidationRequest>(new SampleValidationRequestValidator());
        var context = EndpointFilterInvocationContext.Create(new DefaultHttpContext(), new SampleValidationRequest("Valid", 25));

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            filter.InvokeAsync(context, null!).AsTask());
    }

    [Fact]
    public async Task InvokeAsync_Should_CallNext_When_ValidatorIsNull()
    {
        var filter = new ValidationFilter<SampleValidationRequest>(validator: null);
        var context = EndpointFilterInvocationContext.Create(new DefaultHttpContext(), new SampleValidationRequest("Invalid", -1));
        var nextExecuted = false;

        var result = await filter.InvokeAsync(context, ctx =>
        {
            nextExecuted = true;
            return ValueTask.FromResult<object?>("pass-through");
        });

        Assert.True(nextExecuted);
        Assert.Equal("pass-through", result);
    }

    [Fact]
    public async Task InvokeAsync_Should_CallNext_When_ValidatorIsResolvedFromServiceProviderAndValid()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IValidator<SampleValidationRequest>, SampleValidationRequestValidator>();
        var serviceProvider = services.BuildServiceProvider();

        var httpContext = new DefaultHttpContext { RequestServices = serviceProvider };
        var filter = new ValidationFilter<SampleValidationRequest>(); // validator is null, resolved from RequestServices
        var validPayload = new SampleValidationRequest("Alice", 25);
        var context = EndpointFilterInvocationContext.Create(httpContext, validPayload);

        var nextExecuted = false;
        var result = await filter.InvokeAsync(context, ctx =>
        {
            nextExecuted = true;
            return ValueTask.FromResult<object?>("success");
        });

        Assert.True(nextExecuted);
        Assert.Equal("success", result);
    }

    [Fact]
    public async Task InvokeAsync_Should_ReturnHttp422ProblemDetails_When_ValidationFails()
    {
        var validator = new SampleValidationRequestValidator();
        var filter = new ValidationFilter<SampleValidationRequest>(validator);
        var invalidPayload = new SampleValidationRequest(string.Empty, -5);
        var context = EndpointFilterInvocationContext.Create(new DefaultHttpContext(), invalidPayload);

        var nextExecuted = false;
        var result = await filter.InvokeAsync(context, ctx =>
        {
            nextExecuted = true;
            return ValueTask.FromResult<object?>("should-not-execute");
        });

        Assert.False(nextExecuted);
        var problemResult = Assert.IsType<ProblemHttpResult>(result);
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, problemResult.StatusCode);

        var validationDetails = Assert.IsType<HttpValidationProblemDetails>(problemResult.ProblemDetails);
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, validationDetails.Status);
        Assert.Equal(ValidationFilter<SampleValidationRequest>.DefaultValidationErrorCode, validationDetails.Extensions["errorCode"]?.ToString());
        Assert.Equal("One or more validation errors occurred.", validationDetails.Title);
        Assert.True(validationDetails.Errors.ContainsKey("Name"));
        Assert.True(validationDetails.Errors.ContainsKey("Age"));
        Assert.Contains(validationDetails.Errors["Name"], msg => msg.Contains("Name is required", StringComparison.Ordinal));
        Assert.Contains(validationDetails.Errors["Age"], msg => msg.Contains("greater than 0", StringComparison.Ordinal));
    }

    [Fact]
    public async Task InvokeAsync_Should_ReturnHttp422_When_PayloadIsMissing()
    {
        var validator = new SampleValidationRequestValidator();
        var filter = new ValidationFilter<SampleValidationRequest>(validator);
        var context = EndpointFilterInvocationContext.Create(new DefaultHttpContext(), (object?)null);

        var nextExecuted = false;
        var result = await filter.InvokeAsync(context, ctx =>
        {
            nextExecuted = true;
            return ValueTask.FromResult<object?>("should-not-execute");
        });

        Assert.False(nextExecuted);
        var problemResult = Assert.IsType<ProblemHttpResult>(result);
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, problemResult.StatusCode);
        Assert.Equal(ValidationFilter<SampleValidationRequest>.MissingPayloadErrorCode, problemResult.ProblemDetails.Extensions["errorCode"]?.ToString());
    }

    [Fact]
    public void WithValidation_Should_AttachFilter_When_CalledOnRouteHandlerBuilder()
    {
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        var route = app.MapPost("/test-route", (SampleValidationRequest req) => Results.Ok());
        var returnedRoute = route.WithValidation<SampleValidationRequest>();

        Assert.Same(route, returnedRoute);
    }
}

public sealed record SampleValidationRequest(string Name, int Age);

public sealed class SampleValidationRequestValidator : AbstractValidator<SampleValidationRequest>
{
    public SampleValidationRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.");
        RuleFor(x => x.Age).GreaterThan(0).WithMessage("Age must be greater than 0.");
    }
}
