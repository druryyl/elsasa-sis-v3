namespace Elsasa.Sis.IntegrationTests.UnitTests.Students;

using Elsasa.Sis.Students.Features.Enrollment.EnrollAcceptedApplicant;
using FluentValidation.TestHelper;

public sealed class EnrollAcceptedApplicantValidatorTests
{
    private readonly EnrollAcceptedApplicantValidator _validator = new();

    [Fact]
    public void Validate_Should_Succeed_When_AllRequiredFieldsProvidedWithPaymentReference()
    {
        // Arrange
        var request = new EnrollAcceptedApplicantRequest(
            ApplicantId: Guid.NewGuid(),
            AcademicYearId: Guid.NewGuid(),
            InitialCampus: "Main Campus",
            InitialLevel: "Grade 1",
            InitialAcademicYear: "2026/2027",
            PaymentWaived: false,
            PaymentReference: "INV-2026-001");

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Succeed_When_PaymentWaivedWithWaiverReference()
    {
        // Arrange
        var request = new EnrollAcceptedApplicantRequest(
            ApplicantId: Guid.NewGuid(),
            AcademicYearId: Guid.NewGuid(),
            InitialCampus: "Main Campus",
            InitialLevel: "Grade 1",
            InitialAcademicYear: "2026/2027",
            PaymentWaived: true,
            WaiverReference: "SCHOLARSHIP-DECREE-2026-99");

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Succeed_When_CheckFinanceModuleIsTrueWithoutPaymentReference()
    {
        // Arrange
        var request = new EnrollAcceptedApplicantRequest(
            ApplicantId: Guid.NewGuid(),
            AcademicYearId: Guid.NewGuid(),
            InitialCampus: "Main Campus",
            InitialLevel: "Grade 1",
            InitialAcademicYear: "2026/2027",
            PaymentWaived: false,
            CheckFinanceModule: true);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_Should_Fail_When_ApplicantIdIsEmpty()
    {
        // Arrange
        var request = new EnrollAcceptedApplicantRequest(
            ApplicantId: Guid.Empty,
            AcademicYearId: Guid.NewGuid(),
            InitialCampus: "Main Campus",
            InitialLevel: "Grade 1",
            InitialAcademicYear: "2026/2027",
            PaymentWaived: true,
            WaiverReference: "REF-001");

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ApplicantId)
            .WithErrorMessage("Applicant ID is required.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_Should_Fail_When_InitialCampusIsEmpty(string? campus)
    {
        // Arrange
        var request = new EnrollAcceptedApplicantRequest(
            ApplicantId: Guid.NewGuid(),
            AcademicYearId: Guid.NewGuid(),
            InitialCampus: campus!,
            InitialLevel: "Grade 1",
            InitialAcademicYear: "2026/2027",
            PaymentWaived: true,
            WaiverReference: "REF-001");

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.InitialCampus)
            .WithErrorMessage("Initial campus is required.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_Should_Fail_When_InitialLevelIsEmpty(string? level)
    {
        // Arrange
        var request = new EnrollAcceptedApplicantRequest(
            ApplicantId: Guid.NewGuid(),
            AcademicYearId: Guid.NewGuid(),
            InitialCampus: "Main Campus",
            InitialLevel: level!,
            InitialAcademicYear: "2026/2027",
            PaymentWaived: true,
            WaiverReference: "REF-001");

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.InitialLevel)
            .WithErrorMessage("Initial level is required.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_Should_Fail_When_InitialAcademicYearIsEmpty(string? academicYear)
    {
        // Arrange
        var request = new EnrollAcceptedApplicantRequest(
            ApplicantId: Guid.NewGuid(),
            AcademicYearId: Guid.NewGuid(),
            InitialCampus: "Main Campus",
            InitialLevel: "Grade 1",
            InitialAcademicYear: academicYear!,
            PaymentWaived: true,
            WaiverReference: "REF-001");

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.InitialAcademicYear)
            .WithErrorMessage("Initial academic year is required.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_Should_Fail_When_PaymentWaivedTrue_And_WaiverReferenceEmpty(string? waiverRef)
    {
        // Arrange
        var request = new EnrollAcceptedApplicantRequest(
            ApplicantId: Guid.NewGuid(),
            AcademicYearId: Guid.NewGuid(),
            InitialCampus: "Main Campus",
            InitialLevel: "Grade 1",
            InitialAcademicYear: "2026/2027",
            PaymentWaived: true,
            WaiverReference: waiverRef);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.WaiverReference)
            .WithErrorMessage("Waiver reference is required when payment is waived.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_Should_Fail_When_PaymentNotWaived_And_NoPaymentRef_And_CheckFinanceNotTrue(string? paymentRef)
    {
        // Arrange
        var request = new EnrollAcceptedApplicantRequest(
            ApplicantId: Guid.NewGuid(),
            AcademicYearId: Guid.NewGuid(),
            InitialCampus: "Main Campus",
            InitialLevel: "Grade 1",
            InitialAcademicYear: "2026/2027",
            PaymentWaived: false,
            PaymentReference: paymentRef,
            CheckFinanceModule: false);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PaymentReference)
            .WithErrorMessage("Payment reference is required when payment is not waived and automated finance check is not requested.");
    }
}
