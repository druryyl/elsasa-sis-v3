namespace Elsasa.Sis.IntegrationTests.UnitTests.Academics;

using Elsasa.Sis.Academics.Features.Assignments.CreateAssignment;
using FluentAssertions;
using Xunit;

/// <summary>
/// Unit tests for Coursework Assignment validators (P5-S10, TD-05).
/// </summary>
[Trait("Category", "UnitTest")]
public sealed class AssignmentValidatorTests
{
    private readonly CreateAssignmentValidator _createValidator = new();

    [Fact]
    public void CreateValidator_Should_Succeed_When_RequestIsValid()
    {
        // Arrange
        var request = new CreateAssignmentRequest(
            Title: "Algebra Problem Set 1",
            Description: "Solve problems 1-20 in chapter 4.",
            ReleaseDate: new DateOnly(2026, 11, 10),
            DueDate: new DateOnly(2026, 11, 17),
            Notes: "Calculators permitted.");

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void CreateValidator_Should_Succeed_When_DueDateIsNull()
    {
        // Arrange
        var request = new CreateAssignmentRequest(
            Title: "Open-Ended Project Draft",
            Description: null,
            ReleaseDate: new DateOnly(2026, 11, 10),
            DueDate: null,
            Notes: null);

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateValidator_Should_Succeed_When_DueDateEqualsReleaseDate()
    {
        // Arrange
        var request = new CreateAssignmentRequest(
            Title: "In-Class Quiz",
            Description: "Complete by end of session",
            ReleaseDate: new DateOnly(2026, 11, 10),
            DueDate: new DateOnly(2026, 11, 10),
            Notes: null);

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void CreateValidator_Should_Fail_When_TitleIsMissing(string? title)
    {
        // Arrange
        var request = new CreateAssignmentRequest(
            Title: title!,
            Description: "Some desc",
            ReleaseDate: new DateOnly(2026, 11, 10),
            DueDate: new DateOnly(2026, 11, 15),
            Notes: null);

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.Title));
    }

    [Fact]
    public void CreateValidator_Should_Fail_When_TitleExceeds200Characters()
    {
        // Arrange
        var request = new CreateAssignmentRequest(
            Title: new string('A', 201),
            Description: null,
            ReleaseDate: new DateOnly(2026, 11, 10),
            DueDate: new DateOnly(2026, 11, 15),
            Notes: null);

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.Title));
    }

    [Fact]
    public void CreateValidator_Should_Fail_When_DueDateIsBeforeReleaseDate()
    {
        // Arrange
        var request = new CreateAssignmentRequest(
            Title: "Invalid Due Date Assignment",
            Description: null,
            ReleaseDate: new DateOnly(2026, 11, 15),
            DueDate: new DateOnly(2026, 11, 10),
            Notes: null);

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.DueDate) && e.ErrorMessage.Contains("on or after"));
    }

    [Fact]
    public void CreateValidator_Should_Fail_When_ReleaseDateIsDefault()
    {
        // Arrange
        var request = new CreateAssignmentRequest(
            Title: "Invalid Release Date Assignment",
            Description: null,
            ReleaseDate: default,
            DueDate: new DateOnly(2026, 11, 10),
            Notes: null);

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.ReleaseDate));
    }

    [Fact]
    public void CreateValidator_Should_Fail_When_DescriptionExceeds4000Characters()
    {
        // Arrange
        var request = new CreateAssignmentRequest(
            Title: "Valid Title",
            Description: new string('A', 4001),
            ReleaseDate: new DateOnly(2026, 11, 10),
            DueDate: new DateOnly(2026, 11, 15),
            Notes: null);

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.Description));
    }

    [Fact]
    public void CreateValidator_Should_Fail_When_NotesExceed4000Characters()
    {
        // Arrange
        var request = new CreateAssignmentRequest(
            Title: "Valid Title",
            Description: null,
            ReleaseDate: new DateOnly(2026, 11, 10),
            DueDate: new DateOnly(2026, 11, 15),
            Notes: new string('N', 4001));

        // Act
        var result = _createValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(request.Notes));
    }
}
