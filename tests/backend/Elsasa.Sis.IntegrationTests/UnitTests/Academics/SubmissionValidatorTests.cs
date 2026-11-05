namespace Elsasa.Sis.IntegrationTests.UnitTests.Academics;

using Elsasa.Sis.Academics.Features.Assignments.RecordSubmissionException;
using Elsasa.Sis.Academics.Features.Assignments.SubmitCoursework;
using FluentAssertions;
using Xunit;

/// <summary>
/// Unit tests for Coursework Submission & Exception validators (P5-S11, TD-06, TD-07).
/// </summary>
[Trait("Category", "UnitTest")]
public sealed class SubmissionValidatorTests
{
    private readonly SubmitCourseworkValidator _submitValidator = new();
    private readonly RecordSubmissionExceptionValidator _exceptionValidator = new();

    [Theory]
    [InlineData("https://docs.google.com/document/d/12345/edit")]
    [InlineData("http://github.com/student/repo/blob/main/solution.py")]
    [InlineData("App_Data/Submissions/2026/student-456/submission_final.pdf")]
    [InlineData("/var/storage/submissions/2026/biology_report.docx")]
    [InlineData("Answer: 42. Derived via quadratic formula from the prompt equation.")]
    [InlineData("Summary notes written directly into coursework response container.")]
    public void SubmitValidator_Should_Succeed_For_VariousSubmissionReferenceFormats(string reference)
    {
        // Arrange
        var request = new SubmitCourseworkRequest(
            StudentId: Guid.NewGuid(),
            SubmissionReference: reference,
            Notes: "Completed on time.");

        // Act
        var result = _submitValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void SubmitValidator_Should_Fail_When_StudentIdIsEmpty()
    {
        // Arrange
        var request = new SubmitCourseworkRequest(
            StudentId: Guid.Empty,
            SubmissionReference: "https://github.com/student/assignment");

        // Act
        var result = _submitValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.StudentId));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void SubmitValidator_Should_Fail_When_SubmissionReferenceIsMissing(string? reference)
    {
        // Arrange
        var request = new SubmitCourseworkRequest(
            StudentId: Guid.NewGuid(),
            SubmissionReference: reference!);

        // Act
        var result = _submitValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.SubmissionReference));
    }

    [Fact]
    public void SubmitValidator_Should_Fail_When_SubmissionReferenceExceeds1000Characters()
    {
        // Arrange
        var request = new SubmitCourseworkRequest(
            StudentId: Guid.NewGuid(),
            SubmissionReference: new string('U', 1001));

        // Act
        var result = _submitValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.SubmissionReference));
    }

    [Fact]
    public void SubmitValidator_Should_Fail_When_NotesExceed2000Characters()
    {
        // Arrange
        var request = new SubmitCourseworkRequest(
            StudentId: Guid.NewGuid(),
            SubmissionReference: "https://example.com/work",
            Notes: new string('N', 2001));

        // Act
        var result = _submitValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.Notes));
    }

    [Theory]
    [InlineData("excused")]
    [InlineData("EXCUSED")]
    [InlineData("exempted")]
    [InlineData("Exempted")]
    public void ExceptionValidator_Should_Succeed_When_RequestIsValid(string exceptionType)
    {
        // Arrange
        var request = new RecordSubmissionExceptionRequest(
            StudentId: Guid.NewGuid(),
            ExceptionType: exceptionType,
            Reason: "Medical documentation submitted and verified.");

        // Act
        var result = _exceptionValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ExceptionValidator_Should_Fail_When_StudentIdIsEmpty()
    {
        // Arrange
        var request = new RecordSubmissionExceptionRequest(
            StudentId: Guid.Empty,
            ExceptionType: "excused",
            Reason: "Valid reason");

        // Act
        var result = _exceptionValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.StudentId));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("late")]
    [InlineData("submitted")]
    [InlineData("waived")]
    public void ExceptionValidator_Should_Fail_When_ExceptionTypeIsInvalid(string exceptionType)
    {
        // Arrange
        var request = new RecordSubmissionExceptionRequest(
            StudentId: Guid.NewGuid(),
            ExceptionType: exceptionType,
            Reason: "Valid reason for exception");

        // Act
        var result = _exceptionValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.ExceptionType));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    [InlineData("ab")]
    public void ExceptionValidator_Should_Fail_When_ReasonIsTooShortOrEmpty(string? reason)
    {
        // Arrange
        var request = new RecordSubmissionExceptionRequest(
            StudentId: Guid.NewGuid(),
            ExceptionType: "excused",
            Reason: reason!);

        // Act
        var result = _exceptionValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.Reason));
    }

    [Fact]
    public void ExceptionValidator_Should_Fail_When_ReasonExceeds2000Characters()
    {
        // Arrange
        var request = new RecordSubmissionExceptionRequest(
            StudentId: Guid.NewGuid(),
            ExceptionType: "exempted",
            Reason: new string('R', 2001));

        // Act
        var result = _exceptionValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.Reason));
    }
}
