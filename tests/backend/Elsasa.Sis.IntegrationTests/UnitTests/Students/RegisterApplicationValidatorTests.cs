namespace Elsasa.Sis.IntegrationTests.UnitTests.Students;

using Elsasa.Sis.Students.Features.Admission.RegisterApplication;
using FluentAssertions;
using FluentValidation.TestHelper;
using Xunit;

[Trait("Category", "UnitTest")]
public sealed class RegisterApplicationValidatorTests
{
    private readonly RegisterApplicationValidator _validator = new();

    private static RegisterApplicationRequest CreateValidRequest(
        Action<RegisterApplicationRequestBuilder>? configure = null)
    {
        var builder = new RegisterApplicationRequestBuilder();
        configure?.Invoke(builder);
        return builder.Build();
    }

    private sealed class RegisterApplicationRequestBuilder
    {
        public string FullName { get; set; } = "Ahmad Dahlan";
        public DateOnly? DateOfBirth { get; set; } = new DateOnly(2018, 5, 12);
        public string? Gender { get; set; } = "Male";
        public string? GovernmentIdType { get; set; } = "NIK";
        public string? GovernmentIdNumber { get; set; } = "3171234567890001";
        public string? Email { get; set; } = "applicant@elsasa.sch.id";
        public string? Phone { get; set; } = "+628123456789";
        public string? Address { get; set; } = "Jl. Merdeka No. 10";
        public string? PreviousSchoolName { get; set; } = "TK Aisyiyah 1";
        public string? PreviousSchoolAddress { get; set; } = "Jakarta";
        public string? Notes { get; set; } = "Prospective student";
        public Guid? ApplicantId { get; set; }
        public Guid AdmissionPeriodId { get; set; } = Guid.NewGuid();
        public string RequestedEntryLevel { get; set; } = "Grade 1";
        public string? RequestedCampus { get; set; } = "Main Campus";
        public string? RequestedProgram { get; set; } = "Regular";
        public string? ApplicationType { get; set; } = "new";
        public List<RegisterApplicationContactRequest> Contacts { get; set; } =
        [
            new(
                FullName: "Siti Rahma",
                Relationship: "Mother",
                Phone: "+628129876543",
                Email: "siti.rahma@elsasa.sch.id",
                Address: "Jl. Merdeka No. 10",
                IsPrimary: true)
        ];
        public bool? BypassWindowCheck { get; set; } = false;

        public RegisterApplicationRequest Build() =>
            new(
                ApplicantId: ApplicantId,
                FullName: FullName,
                DateOfBirth: DateOfBirth,
                Gender: Gender,
                GovernmentIdType: GovernmentIdType,
                GovernmentIdNumber: GovernmentIdNumber,
                Email: Email,
                Phone: Phone,
                Address: Address,
                PreviousSchoolName: PreviousSchoolName,
                PreviousSchoolAddress: PreviousSchoolAddress,
                AdmissionPeriodId: AdmissionPeriodId,
                ApplicationType: ApplicationType,
                RequestedEntryLevel: RequestedEntryLevel,
                RequestedCampus: RequestedCampus,
                RequestedProgram: RequestedProgram,
                Contacts: Contacts,
                BypassWindowCheck: BypassWindowCheck,
                Notes: Notes);
    }

    [Fact]
    public void Validator_Should_Pass_When_RequestIsCompletelyValid()
    {
        var request = CreateValidRequest();

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void Validator_Should_Fail_When_FullNameIsNullOrEmpty(string? fullName)
    {
        var request = CreateValidRequest(b => b.FullName = fullName!);

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.FullName)
            .WithErrorMessage("Applicant full name is required.");
    }

    [Fact]
    public void Validator_Should_Fail_When_FullNameExceeds200Characters()
    {
        var request = CreateValidRequest(b => b.FullName = new string('A', 201));

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.FullName)
            .WithErrorMessage("Applicant full name cannot exceed 200 characters.");
    }

    [Fact]
    public void Validator_Should_Fail_When_DateOfBirthIsNull()
    {
        var request = CreateValidRequest(b => b.DateOfBirth = null);

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.DateOfBirth)
            .WithErrorMessage("Date of birth is required.");
    }

    [Fact]
    public void Validator_Should_Fail_When_DateOfBirthIsInFuture()
    {
        var futureDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1));
        var request = CreateValidRequest(b => b.DateOfBirth = futureDate);

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.DateOfBirth)
            .WithErrorMessage("Date of birth cannot be in the future.");
    }

    [Fact]
    public void Validator_Should_Pass_When_DateOfBirthIsToday()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var request = CreateValidRequest(b => b.DateOfBirth = today);

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.DateOfBirth);
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("@missing-user.com")]
    [InlineData("missing-domain@")]
    [InlineData("plainaddress")]
    public void Validator_Should_Fail_When_EmailIsInvalid(string invalidEmail)
    {
        var request = CreateValidRequest(b => b.Email = invalidEmail);

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Applicant email must be a valid email address.");
    }

    [Fact]
    public void Validator_Should_Pass_When_EmailIsNullOrWhitespace()
    {
        var requestNull = CreateValidRequest(b => b.Email = null);
        var requestEmpty = CreateValidRequest(b => b.Email = "   ");

        _validator.TestValidate(requestNull).ShouldNotHaveValidationErrorFor(x => x.Email);
        _validator.TestValidate(requestEmpty).ShouldNotHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData("abc-def")]
    [InlineData("081234abcd")]
    [InlineData("phone#123")]
    [InlineData("!@#$%^")]
    public void Validator_Should_Fail_When_PhoneFormatIsInvalid(string invalidPhone)
    {
        var request = CreateValidRequest(b => b.Phone = invalidPhone);

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Phone)
            .WithErrorMessage("Applicant phone number format is invalid.");
    }

    [Theory]
    [InlineData("+62 812 3456 7890")]
    [InlineData("081234567890")]
    [InlineData("+1-(555)-123-4567")]
    [InlineData("(021) 7894561")]
    public void Validator_Should_Pass_When_PhoneFormatIsValid(string validPhone)
    {
        var request = CreateValidRequest(b => b.Phone = validPhone);

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.Phone);
    }

    [Fact]
    public void Validator_Should_Fail_When_AdmissionPeriodIdIsEmpty()
    {
        var request = CreateValidRequest(b => b.AdmissionPeriodId = Guid.Empty);

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.AdmissionPeriodId)
            .WithErrorMessage("Admission period ID is required.");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public void Validator_Should_Fail_When_RequestedEntryLevelIsNullOrEmpty(string? entryLevel)
    {
        var request = CreateValidRequest(b => b.RequestedEntryLevel = entryLevel!);

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.RequestedEntryLevel)
            .WithErrorMessage("Requested entry level is required.");
    }

    [Fact]
    public void Validator_Should_Fail_When_ContactsIsNull()
    {
        var request = CreateValidRequest(b => b.Contacts = null!);

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Contacts)
            .WithErrorMessage("Contacts collection is required.");
    }

    [Fact]
    public void Validator_Should_Fail_When_ContactsIsEmpty()
    {
        var request = CreateValidRequest(b => b.Contacts = []);

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Contacts)
            .WithErrorMessage("At least one contact is required.");
    }

    [Fact]
    public void Validator_Should_Fail_When_NoPrimaryContactProvided()
    {
        var nonPrimaryContacts = new List<RegisterApplicationContactRequest>
        {
            new("Budi Santoso", "Uncle", "+6281111111", null, null, IsPrimary: false),
            new("Dewi Sartika", "Aunt", "+6282222222", null, null, IsPrimary: false)
        };

        var request = CreateValidRequest(b => b.Contacts = nonPrimaryContacts);

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Contacts)
            .WithErrorMessage("At least one primary guardian contact is required.");
    }

    [Fact]
    public void Validator_Should_Pass_When_AtLeastOnePrimaryContactExists()
    {
        var contacts = new List<RegisterApplicationContactRequest>
        {
            new("Budi Santoso", "Father", "+6281111111", "budi@elsasa.sch.id", null, IsPrimary: true),
            new("Dewi Sartika", "Mother", "+6282222222", null, null, IsPrimary: false)
        };

        var request = CreateValidRequest(b => b.Contacts = contacts);

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor(x => x.Contacts);
    }

    [Fact]
    public void Validator_Should_Fail_When_ContactMissingName()
    {
        var contacts = new List<RegisterApplicationContactRequest>
        {
            new("", "Father", "+6281111111", null, null, IsPrimary: true)
        };

        var request = CreateValidRequest(b => b.Contacts = contacts);

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor("Contacts[0].FullName")
            .WithErrorMessage("Contact full name is required.");
    }

    [Fact]
    public void Validator_Should_Fail_When_ContactMissingRelationship()
    {
        var contacts = new List<RegisterApplicationContactRequest>
        {
            new("Budi Santoso", "", "+6281111111", null, null, IsPrimary: true)
        };

        var request = CreateValidRequest(b => b.Contacts = contacts);

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor("Contacts[0].Relationship")
            .WithErrorMessage("Contact relationship is required.");
    }

    [Fact]
    public void Validator_Should_Fail_When_ContactHasNeitherPhoneNorEmail()
    {
        var contacts = new List<RegisterApplicationContactRequest>
        {
            new("Budi Santoso", "Father", Phone: null, Email: null, Address: "Jakarta", IsPrimary: true)
        };

        var request = CreateValidRequest(b => b.Contacts = contacts);

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor("Contacts[0]")
            .WithErrorMessage("A contact must have either a phone number or an email address.");
    }

    [Fact]
    public void Validator_Should_Pass_When_ContactHasPhoneOnly()
    {
        var contacts = new List<RegisterApplicationContactRequest>
        {
            new("Budi Santoso", "Father", Phone: "+62812345678", Email: null, Address: null, IsPrimary: true)
        };

        var request = CreateValidRequest(b => b.Contacts = contacts);

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor("Contacts[0]");
    }

    [Fact]
    public void Validator_Should_Pass_When_ContactHasEmailOnly()
    {
        var contacts = new List<RegisterApplicationContactRequest>
        {
            new("Budi Santoso", "Father", Phone: null, Email: "budi@elsasa.sch.id", Address: null, IsPrimary: true)
        };

        var request = CreateValidRequest(b => b.Contacts = contacts);

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveValidationErrorFor("Contacts[0]");
    }
}
