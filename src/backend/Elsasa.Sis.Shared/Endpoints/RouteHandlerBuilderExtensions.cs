namespace Elsasa.Sis.Shared.Endpoints;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

/// <summary>
/// Extension methods for attaching validation filters and metadata to route handlers (ADR-005).
/// </summary>
public static class RouteHandlerBuilderExtensions
{
    /// <summary>
    /// Attaches a <see cref="ValidationFilter{TRequest}"/> and OpenAPI 422 problem metadata to the route handler.
    /// </summary>
    /// <typeparam name="TRequest">The type of the request payload to validate.</typeparam>
    /// <param name="builder">The route handler builder.</param>
    /// <returns>The modified <paramref name="builder"/> instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="builder"/> is null.</exception>
    public static RouteHandlerBuilder WithValidation<TRequest>(this RouteHandlerBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder
            .AddEndpointFilter<ValidationFilter<TRequest>>()
            .ProducesValidationProblem(StatusCodes.Status422UnprocessableEntity);
    }
}
