namespace Elsasa.Sis.IntegrationTests.UnitTests.Academics;

using Elsasa.Sis.Academics.Features.AcademicResults.ApproveSummativeResults;
using Elsasa.Sis.Academics.Features.AcademicResults.PublishAcademicResults;
using Elsasa.Sis.Contracts.Academics.Events;
using FluentAssertions;
using Xunit;

/// <summary>
/// Unit tests for <see cref="ApproveSummativeResultsValidator"/> and <see cref="PublishAcademicResultsValidator"/>
/// and related event payloads (UC-ACD-16, P4-S09, TD-04).
/// </summary>
[Trait("Category", "UnitTest")]
public sealed class ApproveAndPublishResultsTests
{
    private readonly ApproveSummativeResultsValidator _approveValidator = new();
    private readonly PublishAcademicResultsValidator _publishValidator = new();

    [Fact]
    public void ApproveValidator_Should_Succeed_When_ResultIds_IsNull()
    {
        // Arrange
        var request = new ApproveSummativeResultsRequest(ResultIds: null);

        // Act
        var result = _approveValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ApproveValidator_Should_Succeed_When_ResultIds_IsEmpty()
    {
        // Arrange
        var request = new ApproveSummativeResultsRequest(ResultIds: []);

        // Act
        var result = _approveValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ApproveValidator_Should_Succeed_When_ResultIds_Contains_ValidGuids()
    {
        // Arrange
        var request = new ApproveSummativeResultsRequest(ResultIds: [Guid.NewGuid(), Guid.NewGuid()]);

        // Act
        var result = _approveValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ApproveValidator_Should_Fail_When_ResultIds_Contains_EmptyGuid()
    {
        // Arrange
        var request = new ApproveSummativeResultsRequest(ResultIds: [Guid.NewGuid(), Guid.Empty]);

        // Act
        var result = _approveValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("ResultIds") && e.ErrorMessage.Contains("cannot be empty"));
    }

    [Fact]
    public void PublishValidator_Should_Succeed_When_ResultIds_IsNull()
    {
        // Arrange
        var request = new PublishAcademicResultsRequest(ResultIds: null);

        // Act
        var result = _publishValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void PublishValidator_Should_Succeed_When_ResultIds_IsEmpty()
    {
        // Arrange
        var request = new PublishAcademicResultsRequest(ResultIds: []);

        // Act
        var result = _publishValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void PublishValidator_Should_Succeed_When_ResultIds_Contains_ValidGuids()
    {
        // Arrange
        var request = new PublishAcademicResultsRequest(ResultIds: [Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid()]);

        // Act
        var result = _publishValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void PublishValidator_Should_Fail_When_ResultIds_Contains_EmptyGuid()
    {
        // Arrange
        var request = new PublishAcademicResultsRequest(ResultIds: [Guid.Empty]);

        // Act
        var result = _publishValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName.Contains("ResultIds") && e.ErrorMessage.Contains("cannot be empty"));
    }

    [Fact]
    public void AcademicResultApprovedEvent_Should_Correctly_Initialize_Properties()
    {
        // Arrange
        var resultId = Guid.NewGuid();
        var assessmentId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        decimal score = 92.5m;
        var gradeLetter = "A";
        var approvedBy = "coordinator.jane";

        // Act
        var ev = new AcademicResultApprovedEvent(
            resultId,
            assessmentId,
            studentId,
            null,
            score,
            gradeLetter,
            approvedBy);

        // Assert
        ev.ResultId.Should().Be(resultId);
        ev.AssessmentId.Should().Be(assessmentId);
        ev.StudentId.Should().Be(studentId);
        ev.ApplicantId.Should().BeNull();
        ev.ScoreNumeric.Should().Be(score);
        ev.GradeLetter.Should().Be(gradeLetter);
        ev.ApprovedBy.Should().Be(approvedBy);
        ev.OccurredOn.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void AcademicResultApprovedEvent_Should_Support_Applicant_Candidates()
    {
        // Arrange
        var resultId = Guid.NewGuid();
        var assessmentId = Guid.NewGuid();
        var applicantId = Guid.NewGuid();
        decimal score = 84.0m;
        var gradeLetter = "B+";
        var approvedBy = "authority.drury";

        // Act
        var ev = new AcademicResultApprovedEvent(
            resultId,
            assessmentId,
            null,
            applicantId,
            score,
            gradeLetter,
            approvedBy);

        // Assert
        ev.StudentId.Should().BeNull();
        ev.ApplicantId.Should().Be(applicantId);
        ev.ScoreNumeric.Should().Be(score);
        ev.GradeLetter.Should().Be(gradeLetter);
        ev.ApprovedBy.Should().Be(approvedBy);
    }

    [Fact]
    public void AcademicResultPublishedEvent_Should_Correctly_Initialize_Properties()
    {
        // Arrange
        var resultId = Guid.NewGuid();
        var assessmentId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        decimal score = 95.0m;
        var gradeLetter = "A";

        // Act
        var ev = new AcademicResultPublishedEvent(
            resultId,
            assessmentId,
            studentId,
            score,
            gradeLetter);

        // Assert
        ev.ResultId.Should().Be(resultId);
        ev.AssessmentId.Should().Be(assessmentId);
        ev.StudentId.Should().Be(studentId);
        ev.ScoreNumeric.Should().Be(score);
        ev.GradeLetter.Should().Be(gradeLetter);
        ev.OccurredOn.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void ApproveSummativeResultsResponse_Should_Hold_ApprovedCount()
    {
        // Arrange & Act
        var response = new ApproveSummativeResultsResponse(ApprovedCount: 42);

        // Assert
        response.ApprovedCount.Should().Be(42);
    }

    [Fact]
    public void PublishAcademicResultsResponse_Should_Hold_PublishedCount()
    {
        // Arrange & Act
        var response = new PublishAcademicResultsResponse(PublishedCount: 15);

        // Assert
        response.PublishedCount.Should().Be(15);
    }
}
