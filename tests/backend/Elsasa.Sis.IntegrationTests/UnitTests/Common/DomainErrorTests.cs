namespace Elsasa.Sis.IntegrationTests.UnitTests.Common;

using Elsasa.Sis.Shared.Common;

public sealed class DomainErrorTests
{
    [Theory]
    [InlineData(ErrorType.NotFound)]
    [InlineData(ErrorType.Conflict)]
    [InlineData(ErrorType.Validation)]
    [InlineData(ErrorType.Unauthorized)]
    [InlineData(ErrorType.Forbidden)]
    [InlineData(ErrorType.Failure)]
    public void Factory_Should_SetRequestedErrorType_When_Called(ErrorType expectedType)
    {
        var error = expectedType switch
        {
            ErrorType.NotFound => DomainError.NotFound("Test.Code", "Test message."),
            ErrorType.Conflict => DomainError.Conflict("Test.Code", "Test message."),
            ErrorType.Validation => DomainError.Validation("Test.Code", "Test message."),
            ErrorType.Unauthorized => DomainError.Unauthorized("Test.Code", "Test message."),
            ErrorType.Forbidden => DomainError.Forbidden("Test.Code", "Test message."),
            ErrorType.Failure => DomainError.Failure("Test.Code", "Test message."),
            _ => throw new ArgumentOutOfRangeException(nameof(expectedType), expectedType, "Unsupported test input.")
        };

        Assert.Equal(expectedType, error.Type);
        Assert.Equal("Test.Code", error.Code);
        Assert.Equal("Test message.", error.Message);
        Assert.Null(error.ValidationErrors);
    }

    [Fact]
    public void Validation_Should_ExposeValidationErrors_When_ValidationErrorsAreProvided()
    {
        var validationErrors = new Dictionary<string, string[]>
        {
            ["firstName"] = ["First name is required."],
            ["lastName"] = ["Last name is required.", "Last name is too long."]
        };

        var error = DomainError.Validation("Students.ValidationFailed", "The request is invalid.", validationErrors);

        Assert.Equal(ErrorType.Validation, error.Type);
        var actualErrors = Assert.IsAssignableFrom<IDictionary<string, string[]>>(error.ValidationErrors);
        Assert.Equal(2, actualErrors.Count);
        Assert.Equal(validationErrors["firstName"], actualErrors["firstName"]);
        Assert.Equal(validationErrors["lastName"], actualErrors["lastName"]);
    }

    [Fact]
    public void Validation_Should_SnapshotValidationErrors_When_SourceDictionaryIsMutatedAfterConstruction()
    {
        var messages = new[] { "First name is required." };
        var validationErrors = new Dictionary<string, string[]>
        {
            ["firstName"] = messages
        };

        var error = DomainError.Validation("Students.ValidationFailed", "The request is invalid.", validationErrors);

        messages[0] = "Mutated after construction.";
        var addedLater = new[] { "Added after construction." };
        validationErrors["middleName"] = addedLater;

        var actualErrors = Assert.IsAssignableFrom<IDictionary<string, string[]>>(error.ValidationErrors);
        Assert.Equal("First name is required.", actualErrors["firstName"][0]);
        Assert.False(actualErrors.ContainsKey("middleName"));
    }

    [Theory]
    [InlineData(" ", "Test message.")]
    [InlineData("Test.Code", " ")]
    public void Constructor_Should_ThrowArgumentException_When_CodeOrMessageIsBlank(string code, string message)
    {
        Assert.Throws<ArgumentException>(() => new DomainError(code, message, ErrorType.Failure));
    }
}
