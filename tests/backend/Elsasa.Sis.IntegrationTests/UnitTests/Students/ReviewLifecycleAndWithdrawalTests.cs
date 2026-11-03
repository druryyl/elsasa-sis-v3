namespace Elsasa.Sis.IntegrationTests.UnitTests.Students;

using Elsasa.Sis.Students;
using Elsasa.Sis.Students.Features.Admission.StartApplicationReview;
using Elsasa.Sis.Students.Features.Admission.WithdrawAdmissionApplication;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

[Trait("Category", "UnitTest")]
public sealed class ReviewLifecycleAndWithdrawalTests
{
    private readonly WithdrawAdmissionApplicationValidator _withdrawValidator = new();

    [Fact]
    public void WithdrawValidator_Should_Pass_When_ReasonIsValid()
    {
        // Arrange
        var request = new WithdrawAdmissionApplicationRequest("Family relocation to another city.");

        // Act
        var result = _withdrawValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void WithdrawValidator_Should_Fail_When_ReasonIsNullOrEmpty(string? reason)
    {
        // Arrange
        var request = new WithdrawAdmissionApplicationRequest(reason!);

        // Act
        var result = _withdrawValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(WithdrawAdmissionApplicationRequest.WithdrawalReason));
    }

    [Fact]
    public void WithdrawValidator_Should_Fail_When_ReasonExceeds500Chars()
    {
        // Arrange
        var longReason = new string('x', 501);
        var request = new WithdrawAdmissionApplicationRequest(longReason);

        // Act
        var result = _withdrawValidator.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e =>
            e.PropertyName == nameof(WithdrawAdmissionApplicationRequest.WithdrawalReason)
            && e.ErrorMessage.Contains("500 characters", StringComparison.Ordinal));
    }

    [Fact]
    public void StartApplicationReviewResponse_Should_ConstructProperly()
    {
        // Arrange
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        // Act
        var response = new StartApplicationReviewResponse(id, "under_review", now);

        // Assert
        response.Id.Should().Be(id);
        response.Status.Should().Be("under_review");
        response.UpdatedAt.Should().Be(now);
    }

    [Fact]
    public void WithdrawAdmissionApplicationResponse_Should_ConstructProperly()
    {
        // Arrange
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        // Act
        var response = new WithdrawAdmissionApplicationResponse(id, "withdrawn", "Withdrawal Reason: Relocated", now);

        // Assert
        response.Id.Should().Be(id);
        response.Status.Should().Be("withdrawn");
        response.Notes.Should().Be("Withdrawal Reason: Relocated");
        response.UpdatedAt.Should().Be(now);
    }

    [Fact]
    public void AddStudentsDomain_Should_RegisterLifecycleHandlers()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();

        // Act
        services.AddStudentsDomain();

        // Assert
        services.Should().Contain(s => s.ServiceType == typeof(StartApplicationReviewHandler));
        services.Should().Contain(s => s.ServiceType == typeof(WithdrawAdmissionApplicationHandler));
    }
}
