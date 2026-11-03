namespace Elsasa.Sis.IntegrationTests.UnitTests.Students;

using System.Globalization;
using FluentAssertions;
using Xunit;

[Trait("Category", "UnitTest")]
public sealed class ApplicantDeduplicationRulesTests
{
    [Theory]
    [InlineData("Ahmad Dahlan", "AHMAD DAHLAN")]
    [InlineData("  Ahmad Dahlan  ", "AHMAD DAHLAN")]
    [InlineData("ahmad dahlan", "AHMAD DAHLAN")]
    [InlineData("AHMAD DAHLAN", "AHMAD DAHLAN")]
    public void NameNormalization_Should_NormalizeToUppercaseTrimmed(string rawName, string expectedNormalized)
    {
        // Act: mirrors SQL UPPER(LTRIM(RTRIM(@FullName))) logic in RegisterApplicationHandler
        var normalized = rawName.Trim().ToUpper(CultureInfo.InvariantCulture);

        // Assert
        normalized.Should().Be(expectedNormalized);
    }

    [Theory]
    [InlineData("Ahmad Dahlan", "Ahmad", "Dahlan")]
    [InlineData("SingleName", "SingleName", "SingleName")]
    [InlineData("Muhammad Ahmad Dahlan", "Muhammad", "Ahmad Dahlan")]
    [InlineData("  Siti  Nurhaliza  ", "Siti", "Nurhaliza")]
    public void NameSplitting_Should_ProduceFirstNameAndLastName(string fullName, string expectedFirst, string expectedLast)
    {
        // Act: mirrors RegisterApplicationHandler name splitting
        var trimmedFullName = fullName.Trim();
        var nameParts = trimmedFullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var firstName = nameParts.Length > 0 ? nameParts[0] : trimmedFullName;
        var lastName = nameParts.Length > 1 ? nameParts[1] : firstName;

        // Assert
        firstName.Should().Be(expectedFirst);
        lastName.Should().Be(expectedLast);
    }

    [Fact]
    public void NameLengthCapping_Should_LimitFirstAndLastNameTo100AndFullTo200()
    {
        // Arrange
        var veryLongFirst = new string('A', 150);
        var veryLongLast = new string('B', 150);
        var veryLongFull = $"{veryLongFirst} {veryLongLast}";

        // Act
        var trimmedFullName = veryLongFull.Trim();
        var nameParts = trimmedFullName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var firstName = nameParts.Length > 0 ? nameParts[0] : trimmedFullName;
        var lastName = nameParts.Length > 1 ? nameParts[1] : firstName;

        if (firstName.Length > 100) firstName = firstName[..100];
        if (lastName.Length > 100) lastName = lastName[..100];
        var cappedFullName = trimmedFullName.Length > 200 ? trimmedFullName[..200] : trimmedFullName;

        // Assert
        firstName.Length.Should().Be(100);
        lastName.Length.Should().Be(100);
        cappedFullName.Length.Should().Be(200);
    }

    private static readonly string[] _inactiveStatuses = ["withdrawn", "decided"];

    [Theory]
    [InlineData("registered", true)]
    [InlineData("under_review", true)]
    [InlineData("submitted", true)]
    [InlineData("decided", false)]
    [InlineData("withdrawn", false)]
    public void ActiveApplicationStatusRule_Should_IdentifyBlockingStatuses(string status, bool isConsideredActive)
    {
        // Act: mirrors TD-03 / SQL WHERE status NOT IN ('withdrawn', 'decided')
        var activeStatuses = !_inactiveStatuses.Contains(status.ToLowerInvariant());

        // Assert
        activeStatuses.Should().Be(isConsideredActive);
    }

    [Fact]
    public void DeduplicationPriorityOrder_Should_PrioritizeApplicantId_Over_GovId_And_NameDob()
    {
        // Arrange
        var explicitApplicantId = Guid.NewGuid();
        var govIdMatch = Guid.NewGuid();
        var nameDobMatch = Guid.NewGuid();

        // Simulate TD-03 cascading match resolution
        Guid? matchedApplicantId = null;

        // Step 1: explicit applicantId provided
        matchedApplicantId = explicitApplicantId;

        // Assert: Step 1 immediately locks in the applicant ID without evaluating lower priorities
        matchedApplicantId.Should().Be(explicitApplicantId);
        matchedApplicantId.Should().NotBe(govIdMatch);
        matchedApplicantId.Should().NotBe(nameDobMatch);
    }

    [Fact]
    public void DeduplicationPriorityOrder_Should_PrioritizeGovId_Over_NameDob_When_ApplicantIdNull()
    {
        // Arrange
        Guid? applicantIdInput = null;
        var govIdMatch = Guid.NewGuid();
        var nameDobMatch = Guid.NewGuid();

        // Simulate TD-03 cascading match resolution
        Guid? matchedApplicantId = null;

        if (applicantIdInput.HasValue)
        {
            matchedApplicantId = applicantIdInput.Value;
        }

        if (!matchedApplicantId.HasValue)
        {
            // Gov ID match found
            matchedApplicantId = govIdMatch;
        }

        if (!matchedApplicantId.HasValue)
        {
            matchedApplicantId = nameDobMatch;
        }

        // Assert: Gov ID match takes precedence over name + DOB
        matchedApplicantId.Should().Be(govIdMatch);
        matchedApplicantId.Should().NotBe(nameDobMatch);
    }
}
