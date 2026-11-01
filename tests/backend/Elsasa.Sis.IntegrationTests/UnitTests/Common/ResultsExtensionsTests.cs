namespace Elsasa.Sis.IntegrationTests.UnitTests.Common;

using Elsasa.Sis.Shared.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

public sealed class ResultsExtensionsTests
{
    [Fact]
    public void ToProblemDetails_Should_ThrowArgumentNullException_When_ErrorIsNull()
    {
        DomainError nullError = null!;

        Assert.Throws<ArgumentNullException>(() => nullError.ToProblemDetails());
    }

    [Theory]
    [InlineData(ErrorType.NotFound, StatusCodes.Status404NotFound, "The requested resource was not found.", "https://tools.ietf.org/html/rfc9110#section-15.5.5")]
    [InlineData(ErrorType.Conflict, StatusCodes.Status409Conflict, "The request conflicts with the current state of the resource.", "https://tools.ietf.org/html/rfc9110#section-15.5.10")]
    [InlineData(ErrorType.Validation, StatusCodes.Status422UnprocessableEntity, "One or more validation errors occurred.", "https://tools.ietf.org/html/rfc4918#section-11.2")]
    [InlineData(ErrorType.Unauthorized, StatusCodes.Status401Unauthorized, "Authentication is required to access this resource.", "https://tools.ietf.org/html/rfc9110#section-15.5.2")]
    [InlineData(ErrorType.Forbidden, StatusCodes.Status403Forbidden, "You are not allowed to access this resource.", "https://tools.ietf.org/html/rfc9110#section-15.5.4")]
    [InlineData(ErrorType.Failure, StatusCodes.Status400BadRequest, "The request could not be processed.", "https://tools.ietf.org/html/rfc9110#section-15.5.1")]
    public void ToProblemDetails_Should_ReturnExpectedProblemDetails_ForStandardErrorTypes(
        ErrorType errorType,
        int expectedStatusCode,
        string expectedTitle,
        string expectedTypeUri)
    {
        var error = errorType switch
        {
            ErrorType.NotFound => DomainError.NotFound("Resource.NotFound", "The requested entity could not be found."),
            ErrorType.Conflict => DomainError.Conflict("Resource.Conflict", "A conflict occurred with existing state."),
            ErrorType.Validation => DomainError.Validation("Resource.Validation", "Validation failed for entity."),
            ErrorType.Unauthorized => DomainError.Unauthorized("Resource.Unauthorized", "Caller is unauthorized."),
            ErrorType.Forbidden => DomainError.Forbidden("Resource.Forbidden", "Caller lacks permission."),
            ErrorType.Failure => DomainError.Failure("Resource.Failure", "Generic failure occurred."),
            _ => throw new ArgumentOutOfRangeException(nameof(errorType), errorType, "Unsupported test error type.")
        };

        var result = error.ToProblemDetails();

        var problemResult = Assert.IsType<ProblemHttpResult>(result);
        Assert.Equal(expectedStatusCode, problemResult.StatusCode);
        Assert.Equal(expectedStatusCode, problemResult.ProblemDetails.Status);
        Assert.Equal(expectedTitle, problemResult.ProblemDetails.Title);
        Assert.Equal(expectedTypeUri, problemResult.ProblemDetails.Type);
        Assert.Equal(error.Message, problemResult.ProblemDetails.Detail);
        Assert.True(problemResult.ProblemDetails.Extensions.ContainsKey("errorCode"));
        Assert.Equal(error.Code, problemResult.ProblemDetails.Extensions["errorCode"]?.ToString());
    }

    [Fact]
    public void ToProblemDetails_Should_ReturnValidationProblem_When_ValidationErrorsArePresent()
    {
        var validationErrors = new Dictionary<string, string[]>
        {
            ["firstName"] = ["First name is required."],
            ["email"] = ["Email format is invalid.", "Email is already taken."]
        };

        var error = DomainError.Validation(
            "Students.ValidationFailed",
            "One or more validation errors occurred.",
            validationErrors);

        var result = error.ToProblemDetails();

        var problemResult = Assert.IsType<ProblemHttpResult>(result);
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, problemResult.StatusCode);

        var validationDetails = Assert.IsType<HttpValidationProblemDetails>(problemResult.ProblemDetails);
        Assert.Equal(StatusCodes.Status422UnprocessableEntity, validationDetails.Status);
        Assert.Equal("One or more validation errors occurred.", validationDetails.Title);
        Assert.Equal("https://tools.ietf.org/html/rfc4918#section-11.2", validationDetails.Type);
        Assert.Equal("One or more validation errors occurred.", validationDetails.Detail);
        Assert.True(validationDetails.Extensions.ContainsKey("errorCode"));
        Assert.Equal("Students.ValidationFailed", validationDetails.Extensions["errorCode"]?.ToString());

        Assert.Equal(2, validationDetails.Errors.Count);
        Assert.True(validationDetails.Errors.ContainsKey("firstName"));
        Assert.Equal(validationErrors["firstName"], validationDetails.Errors["firstName"]);
        Assert.True(validationDetails.Errors.ContainsKey("email"));
        Assert.Equal(validationErrors["email"], validationDetails.Errors["email"]);
    }

    [Fact]
    public void ToProblemDetails_Should_ThrowArgumentOutOfRangeException_When_ErrorTypeIsInvalid()
    {
        var invalidError = new DomainError("Test.Invalid", "Invalid type", (ErrorType)999);

        Assert.Throws<ArgumentOutOfRangeException>(() => invalidError.ToProblemDetails());
    }
}
