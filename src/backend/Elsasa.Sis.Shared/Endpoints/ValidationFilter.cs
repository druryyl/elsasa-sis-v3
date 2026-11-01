namespace Elsasa.Sis.Shared.Endpoints;

using Elsasa.Sis.Shared.Common;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Minimal API endpoint filter executing FluentValidation rules against incoming request payloads (ADR-005).
/// </summary>
/// <typeparam name="TRequest">The type of the request payload to validate.</typeparam>
public sealed class ValidationFilter<TRequest>(IValidator<TRequest>? validator = null) : IEndpointFilter
{
    /// <summary>The default domain error code used when validation fails.</summary>
    public const string DefaultValidationErrorCode = "General.Validation";

    /// <summary>The default error message used when validation fails.</summary>
    public const string DefaultValidationErrorMessage = "One or more validation errors occurred.";

    /// <summary>The error code used when a required request payload is missing.</summary>
    public const string MissingPayloadErrorCode = "Validation.PayloadRequired";

    /// <inheritdoc/>
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(next);

        var activeValidator = validator ?? context.HttpContext?.RequestServices?.GetService<IValidator<TRequest>>();
        if (activeValidator is null)
        {
            return await next(context);
        }

        var request = context.Arguments.OfType<TRequest>().FirstOrDefault();
        if (request is null)
        {
            var missingPayloadError = DomainError.Validation(
                MissingPayloadErrorCode,
                $"The request payload of type '{typeof(TRequest).Name}' is required.");

            return missingPayloadError.ToProblemDetails();
        }

        var cancellationToken = context.HttpContext?.RequestAborted ?? CancellationToken.None;
        var validationResult = await activeValidator.ValidateAsync(request, cancellationToken);

        if (!validationResult.IsValid)
        {
            var error = DomainError.Validation(
                DefaultValidationErrorCode,
                DefaultValidationErrorMessage,
                validationResult.ToDictionary());

            return error.ToProblemDetails();
        }

        return await next(context);
    }
}
