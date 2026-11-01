namespace Elsasa.Sis.IntegrationTests.UnitTests.Common;

using Elsasa.Sis.Shared.Common;

public sealed class ResultTests
{
    [Fact]
    public void Success_Should_ReturnSuccessfulResult_When_NoValueIsProduced()
    {
        var result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Failure_Should_ReturnFailedResult_When_DomainErrorIsProvided()
    {
        var error = DomainError.Conflict("Admission.DuplicateActiveApplication", "An active application already exists.");

        var result = Result.Failure(error);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Same(error, result.Error);
    }

    [Fact]
    public void Failure_Should_ThrowArgumentNullException_When_ErrorIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => Result.Failure(null!));
        Assert.Throws<ArgumentNullException>(() => Result.Failure<int>(null!));
    }

    [Fact]
    public void SuccessOfT_Should_ExposeValue_When_OperationSucceeds()
    {
        var result = Result.Success(42);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Null(result.Error);
        Assert.Equal(42, result.Value);
    }

    [Fact]
    public void FailureOfT_Should_ExposeErrorAndThrowOnValueAccess_When_OperationFails()
    {
        var error = DomainError.NotFound("Operations.AcademicYearNotFound", "The academic year does not exist.");

        var result = Result.Failure<Guid>(error);

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Same(error, result.Error);

        var exception = Assert.Throws<InvalidOperationException>(() => result.Value);
        Assert.Contains("failure", exception.Message);
    }

    [Fact]
    public void ImplicitConversion_Should_CreateSuccessfulResult_When_ValueIsAssigned()
    {
        Result<string> result = "implemented";

        Assert.True(result.IsSuccess);
        Assert.Equal("implemented", result.Value);
        Assert.Null(result.Error);
    }

    [Fact]
    public void ImplicitConversion_Should_CreateFailedResult_When_DomainErrorIsAssigned()
    {
        var error = DomainError.Forbidden("Security.Forbidden", "The caller is not allowed to perform this operation.");

        Result nonGeneric = error;
        Result<string> generic = error;

        Assert.True(nonGeneric.IsFailure);
        Assert.Same(error, nonGeneric.Error);
        Assert.True(generic.IsFailure);
        Assert.Same(error, generic.Error);
    }
}
