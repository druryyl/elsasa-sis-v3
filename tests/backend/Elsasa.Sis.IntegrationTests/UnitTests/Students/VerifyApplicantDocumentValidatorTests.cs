namespace Elsasa.Sis.IntegrationTests.UnitTests.Students;

using Elsasa.Sis.Students.Features.Admission.Documents.VerifyApplicantDocument;
using FluentValidation.TestHelper;

[Trait("Category", "UnitTest")]
public sealed class VerifyApplicantDocumentValidatorTests
{
    private readonly VerifyApplicantDocumentValidator _validator = new();

    [Theory]
    [InlineData("verified")]
    [InlineData("rejected")]
    [InlineData("Verified")]
    [InlineData("REJECTED")]
    [InlineData("  verified  ")]
    public void Validator_Should_Pass_For_ValidStatuses(string status)
    {
        var request = new VerifyApplicantDocumentRequest(status, "Valid notes");
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveValidationErrorFor(x => x.SubmissionStatus);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("submitted")]
    [InlineData("not_submitted")]
    [InlineData("approved")]
    [InlineData("pending")]
    public void Validator_Should_Fail_For_InvalidStatuses(string? status)
    {
        var request = new VerifyApplicantDocumentRequest(status!, "Notes");
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.SubmissionStatus);
    }

    [Fact]
    public void Validator_Should_Fail_When_NotesExceedMaxLimit()
    {
        var longNotes = new string('A', 2001);
        var request = new VerifyApplicantDocumentRequest("verified", longNotes);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Notes);
    }

    [Fact]
    public void Validator_Should_Pass_When_NotesAreNullOrReasonable()
    {
        var request = new VerifyApplicantDocumentRequest("verified", null);
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
