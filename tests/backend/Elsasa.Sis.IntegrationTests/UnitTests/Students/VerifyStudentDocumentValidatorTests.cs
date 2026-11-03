namespace Elsasa.Sis.IntegrationTests.UnitTests.Students;

using Elsasa.Sis.Students.Features.Records.Documents.VerifyStudentDocument;
using FluentValidation.TestHelper;

[Trait("Category", "UnitTest")]
public sealed class VerifyStudentDocumentValidatorTests
{
    private readonly VerifyStudentDocumentValidator _validator = new();

    [Theory]
    [InlineData("verified")]
    [InlineData("rejected")]
    [InlineData("Verified")]
    [InlineData("REJECTED")]
    [InlineData("  verified  ")]
    public void Validator_Should_Pass_For_ValidStatuses(string status)
    {
        var request = new VerifyStudentDocumentRequest(status, "Valid notes");
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
        var request = new VerifyStudentDocumentRequest(status!, "Notes");
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.SubmissionStatus);
    }

    [Fact]
    public void Validator_Should_Fail_When_NotesExceedMaxLimit()
    {
        var longNotes = new string('A', 2001);
        var request = new VerifyStudentDocumentRequest("verified", longNotes);
        var result = _validator.TestValidate(request);
        result.ShouldHaveValidationErrorFor(x => x.Notes);
    }

    [Fact]
    public void Validator_Should_Pass_When_NotesAreNullOrReasonable()
    {
        var request = new VerifyStudentDocumentRequest("verified", null);
        var result = _validator.TestValidate(request);
        result.ShouldNotHaveAnyValidationErrors();
    }
}
