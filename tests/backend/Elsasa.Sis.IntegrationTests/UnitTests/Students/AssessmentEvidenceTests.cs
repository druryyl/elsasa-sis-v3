namespace Elsasa.Sis.IntegrationTests.UnitTests.Students;

using Elsasa.Sis.Students;
using Elsasa.Sis.Students.Features.Admission.AssessmentEvidence.AddAssessmentEvidence;
using Elsasa.Sis.Students.Features.Admission.AssessmentEvidence.DeleteAssessmentEvidence;
using Elsasa.Sis.Students.Features.Admission.AssessmentEvidence.UpdateAssessmentEvidence;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

[Trait("Category", "UnitTest")]
public sealed class AssessmentEvidenceTests
{
    private readonly AddAssessmentEvidenceValidator _addValidator = new();
    private readonly UpdateAssessmentEvidenceValidator _updateValidator = new();

    [Fact]
    public void AddValidator_Should_Pass_When_PayloadIsValid()
    {
        // Arrange
        var request = new AddAssessmentEvidenceRequest(
            AssessmentType: "reading_readiness",
            Score: "85/100",
            Source: "External Assessment Center",
            AssessmentDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-5)),
            Notes: "Student performed well on reading comprehension.");

        // Act
        var result = _addValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void AddValidator_Should_Fail_When_AssessmentTypeIsNullOrEmpty(string? assessmentType)
    {
        // Arrange
        var request = new AddAssessmentEvidenceRequest(
            AssessmentType: assessmentType!,
            Score: "80",
            Source: "Center",
            AssessmentDate: null,
            Notes: null);

        // Act
        var result = _addValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AddAssessmentEvidenceRequest.AssessmentType));
    }

    [Fact]
    public void AddValidator_Should_Fail_When_AssessmentTypeExceeds100Chars()
    {
        // Arrange
        var longType = new string('a', 101);
        var request = new AddAssessmentEvidenceRequest(
            AssessmentType: longType,
            Score: "80",
            Source: "Center",
            AssessmentDate: null,
            Notes: null);

        // Act
        var result = _addValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AddAssessmentEvidenceRequest.AssessmentType));
    }

    [Fact]
    public void AddValidator_Should_Fail_When_ScoreExceeds50Chars()
    {
        // Arrange
        var longScore = new string('x', 51);
        var request = new AddAssessmentEvidenceRequest(
            AssessmentType: "math_readiness",
            Score: longScore,
            Source: "Center",
            AssessmentDate: null,
            Notes: null);

        // Act
        var result = _addValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AddAssessmentEvidenceRequest.Score));
    }

    [Fact]
    public void AddValidator_Should_Fail_When_AssessmentDateIsInFuture()
    {
        // Arrange
        var futureDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(10));
        var request = new AddAssessmentEvidenceRequest(
            AssessmentType: "math_readiness",
            Score: "Passed",
            Source: "Center",
            AssessmentDate: futureDate,
            Notes: null);

        // Act
        var result = _addValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(AddAssessmentEvidenceRequest.AssessmentDate));
    }

    [Fact]
    public void UpdateValidator_Should_Pass_When_PayloadIsValid()
    {
        // Arrange
        var request = new UpdateAssessmentEvidenceRequest(
            AssessmentType: "psychological_evaluation",
            Score: "Band 4",
            Source: "Child Psychology Clinic",
            AssessmentDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-2)),
            Notes: "Verified diagnostic observation.");

        // Act
        var result = _updateValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void UpdateValidator_Should_Fail_When_AssessmentTypeIsNullOrEmpty(string? assessmentType)
    {
        // Arrange
        var request = new UpdateAssessmentEvidenceRequest(
            AssessmentType: assessmentType!,
            Score: "80",
            Source: "Center",
            AssessmentDate: null,
            Notes: null);

        // Act
        var result = _updateValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateAssessmentEvidenceRequest.AssessmentType));
    }

    [Fact]
    public void AddStudentsDomain_Should_RegisterAssessmentEvidenceHandlers()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddStudentsDomain();

        // Assert
        services.Should().Contain(s => s.ServiceType == typeof(AddAssessmentEvidenceHandler));
        services.Should().Contain(s => s.ServiceType == typeof(UpdateAssessmentEvidenceHandler));
        services.Should().Contain(s => s.ServiceType == typeof(DeleteAssessmentEvidenceHandler));
    }
}
