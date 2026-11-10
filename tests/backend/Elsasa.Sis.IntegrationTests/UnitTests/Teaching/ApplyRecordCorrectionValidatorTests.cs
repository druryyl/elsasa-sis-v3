namespace Elsasa.Sis.IntegrationTests.UnitTests.Teaching;

using Elsasa.Sis.Teaching.Features.RecordCorrections.ApplyRecordCorrection;
using FluentValidation.TestHelper;
using Xunit;

[Trait("Category", "UnitTest")]
public sealed class ApplyRecordCorrectionValidatorTests
{
    private readonly ApplyRecordCorrectionValidator _validator = new();

    [Theory]
    [InlineData("teacher_profiles", "full_name")]
    [InlineData("teaching.teacher_profiles", "display_name")]
    [InlineData("teacher_profiles", "email")]
    [InlineData("teacher_profiles", "phone")]
    [InlineData("teacher_profiles", "employee_ref")]
    [InlineData("teacher_profiles", "national_id_or_external_ref")]
    [InlineData("teacher_profiles", "campus_affiliation")]
    [InlineData("teacher_profiles", "department")]
    [InlineData("teacher_profiles", "standing")]
    [InlineData("teacher_profiles", "effective_from")]
    [InlineData("teacher_profiles", "notes")]
    [InlineData("teaching_authorizations", "scope_description")]
    [InlineData("teaching.teaching_authorizations", "campus_scope")]
    [InlineData("teaching_authorizations", "status")]
    [InlineData("teaching_authorizations", "effective_start_date")]
    [InlineData("teaching_authorizations", "delivery_mode_scope")]
    public void Validate_Should_Pass_When_ValidTargetTableAndField(string table, string field)
    {
        // Arrange
        var request = new ApplyRecordCorrectionRequest(
            TargetTableName: table,
            TargetRecordId: Guid.NewGuid(),
            FieldName: field,
            CorrectedValue: "Corrected Content",
            ReasonForCorrection: "Correction of administrative transcription error.");

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("unknown_table")]
    [InlineData("students")]
    [InlineData("users")]
    [InlineData("teaching.unknown")]
    public void Validate_Should_Fail_When_TargetTableIsInvalid(string invalidTable)
    {
        // Arrange
        var request = new ApplyRecordCorrectionRequest(
            TargetTableName: invalidTable,
            TargetRecordId: Guid.NewGuid(),
            FieldName: "full_name",
            CorrectedValue: "New Name",
            ReasonForCorrection: "Correction reason");

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TargetTableName)
            .WithErrorMessage("Target table must be 'teacher_profiles' or 'teaching_authorizations'.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_Should_Fail_When_TargetTableIsEmptyOrNull(string? invalidTable)
    {
        // Arrange
        var request = new ApplyRecordCorrectionRequest(
            TargetTableName: invalidTable!,
            TargetRecordId: Guid.NewGuid(),
            FieldName: "full_name",
            CorrectedValue: "New Name",
            ReasonForCorrection: "Correction reason");

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TargetTableName);
    }

    [Fact]
    public void Validate_Should_Fail_When_TargetRecordIdIsEmpty()
    {
        // Arrange
        var request = new ApplyRecordCorrectionRequest(
            TargetTableName: "teacher_profiles",
            TargetRecordId: Guid.Empty,
            FieldName: "full_name",
            CorrectedValue: "New Name",
            ReasonForCorrection: "Correction reason");

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TargetRecordId)
            .WithErrorMessage("Target record ID is required.");
    }

    [Theory]
    [InlineData("id")]
    [InlineData("created_at")]
    [InlineData("created_by")]
    [InlineData("updated_at")]
    [InlineData("updated_by")]
    [InlineData("random_field")]
    [InlineData("password_hash")]
    public void Validate_Should_Fail_When_FieldIsNotEligible(string disallowedField)
    {
        // Arrange
        var request = new ApplyRecordCorrectionRequest(
            TargetTableName: "teacher_profiles",
            TargetRecordId: Guid.NewGuid(),
            FieldName: disallowedField,
            CorrectedValue: "New Val",
            ReasonForCorrection: "Correction reason");

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FieldName)
            .WithErrorMessage("Field name is not recognized or not eligible for record correction on the target table.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void Validate_Should_Fail_When_ReasonForCorrectionIsEmptyOrNull(string? reason)
    {
        // Arrange
        var request = new ApplyRecordCorrectionRequest(
            TargetTableName: "teacher_profiles",
            TargetRecordId: Guid.NewGuid(),
            FieldName: "full_name",
            CorrectedValue: "New Name",
            ReasonForCorrection: reason!);

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ReasonForCorrection)
            .WithErrorMessage("Reason for correction is mandatory.");
    }

    [Fact]
    public void Validate_Should_Fail_When_CorrectedValueIsNull()
    {
        // Arrange
        var request = new ApplyRecordCorrectionRequest(
            TargetTableName: "teacher_profiles",
            TargetRecordId: Guid.NewGuid(),
            FieldName: "full_name",
            CorrectedValue: null!,
            ReasonForCorrection: "Correction reason");

        // Act
        var result = _validator.TestValidate(request);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.CorrectedValue)
            .WithErrorMessage("Corrected value is required.");
    }
}
