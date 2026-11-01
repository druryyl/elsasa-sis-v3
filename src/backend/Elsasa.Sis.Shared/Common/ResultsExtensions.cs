namespace Elsasa.Sis.Shared.Common;

using Microsoft.AspNetCore.Http;

/// <summary>
/// Translates <see cref="DomainError"/> values into RFC 7807 <c>ProblemDetails</c> responses (ADR-007).
/// </summary>
public static class ResultsExtensions
{
    private const string _validationProblemType = "https://tools.ietf.org/html/rfc4918#section-11.2";

    /// <summary>
    /// Converts a <see cref="DomainError"/> into an HTTP result carrying an RFC 7807
    /// <c>ProblemDetails</c> response. The response always exposes the domain error code
    /// through the <c>errorCode</c> extension member.
    /// </summary>
    /// <param name="error">The domain error to translate.</param>
    /// <returns>
    /// A <see cref="Microsoft.AspNetCore.Http.HttpResults.ProblemHttpResult"/> with status 400, 401, 403,
    /// 404, 409, or 422. When <see cref="DomainError.ValidationErrors"/> contains entries the payload is
    /// an <see cref="HttpValidationProblemDetails"/> with status 422.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="error"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <see cref="DomainError.Type"/> is not a defined <see cref="ErrorType"/>.</exception>
    public static IResult ToProblemDetails(this DomainError error)
    {
        ArgumentNullException.ThrowIfNull(error);

        if (error.ValidationErrors is { Count: > 0 })
        {
            return Results.ValidationProblem(
                error.ValidationErrors,
                detail: error.Message,
                statusCode: StatusCodes.Status422UnprocessableEntity,
                title: TitleFor(ErrorType.Validation),
                type: _validationProblemType,
                extensions: ErrorCodeExtensions(error));
        }

        return Results.Problem(
            detail: error.Message,
            statusCode: StatusCodeFor(error.Type),
            title: TitleFor(error.Type),
            type: ProblemTypeFor(error.Type),
            extensions: ErrorCodeExtensions(error));
    }

    private static int StatusCodeFor(ErrorType type) => type switch
    {
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Validation => StatusCodes.Status422UnprocessableEntity,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.Failure => StatusCodes.Status400BadRequest,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported domain error type.")
    };

    private static string TitleFor(ErrorType type) => type switch
    {
        ErrorType.NotFound => "The requested resource was not found.",
        ErrorType.Conflict => "The request conflicts with the current state of the resource.",
        ErrorType.Validation => "One or more validation errors occurred.",
        ErrorType.Unauthorized => "Authentication is required to access this resource.",
        ErrorType.Forbidden => "You are not allowed to access this resource.",
        ErrorType.Failure => "The request could not be processed.",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported domain error type.")
    };

    private static string ProblemTypeFor(ErrorType type) => type switch
    {
        ErrorType.NotFound => "https://tools.ietf.org/html/rfc9110#section-15.5.5",
        ErrorType.Conflict => "https://tools.ietf.org/html/rfc9110#section-15.5.10",
        ErrorType.Validation => _validationProblemType,
        ErrorType.Unauthorized => "https://tools.ietf.org/html/rfc9110#section-15.5.2",
        ErrorType.Forbidden => "https://tools.ietf.org/html/rfc9110#section-15.5.4",
        ErrorType.Failure => "https://tools.ietf.org/html/rfc9110#section-15.5.1",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported domain error type.")
    };

    private static Dictionary<string, object?> ErrorCodeExtensions(DomainError error) =>
        new(StringComparer.Ordinal) { ["errorCode"] = error.Code };
}
