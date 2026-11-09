namespace Elsasa.Sis.IntegrationTests.UnitTests.Academics;

using Elsasa.Sis.Academics;
using Elsasa.Sis.Academics.Features.AcademicAchievements.ConferAcademicAchievement;
using Elsasa.Sis.Academics.Features.AcademicAchievements.GetPeriodAchievements;
using Elsasa.Sis.Academics.Features.AcademicAchievements.GetStudentAchievements;
using FluentAssertions;
using FluentValidation.TestHelper;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

/// <summary>
/// Unit test suite for Academic Achievements validator and DI registration (UC-ACD-21, P4-S11).
/// </summary>
[Trait("Category", "UnitTest")]
public sealed class AcademicAchievementsValidatorTests
{
    private readonly ConferAcademicAchievementValidator _validator = new();

    [Theory]
    [InlineData("subject_distinction")]
    [InlineData("academic_honors")]
    [InlineData("mastery_recognition")]
    [InlineData("academic_certification")]
    [InlineData("academic_competition")]
    public void ConferValidator_ValidAchievementTypes_ShouldPass(string achievementType)
    {
        var command = new ConferAcademicAchievementCommand(
            StudentId: Guid.NewGuid(),
            LearningUnitId: Guid.NewGuid(),
            AcademicPeriodId: Guid.NewGuid(),
            AchievementType: achievementType,
            Title: "Nilai Tertinggi Matematika",
            Description: "Achieved highest score in grade 4 math",
            CriteriaMet: "Score >= 95 and top rank in cohort",
            SupportingEvidence: "Exam result RS-001 with score 98",
            RecognizedAt: new DateOnly(2026, 6, 15));

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ConferValidator_OptionalFieldsNull_ShouldPass()
    {
        var command = new ConferAcademicAchievementCommand(
            StudentId: Guid.NewGuid(),
            LearningUnitId: null,
            AcademicPeriodId: Guid.NewGuid(),
            AchievementType: "academic_honors",
            Title: "Juara Umum Grade 4",
            Description: null,
            CriteriaMet: "Highest cumulative GPA in grade 4 cohort",
            SupportingEvidence: null,
            RecognizedAt: new DateOnly(2026, 6, 15));

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("invalid_type")]
    [InlineData("sports_trophy")]
    public void ConferValidator_InvalidAchievementType_ShouldHaveValidationError(string invalidType)
    {
        var command = new ConferAcademicAchievementCommand(
            StudentId: Guid.NewGuid(),
            LearningUnitId: null,
            AcademicPeriodId: Guid.NewGuid(),
            AchievementType: invalidType,
            Title: "Best Athlete",
            Description: null,
            CriteriaMet: "Won 100m sprint",
            SupportingEvidence: null,
            RecognizedAt: new DateOnly(2026, 6, 15));

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.AchievementType);
    }

    [Fact]
    public void ConferValidator_EmptyStudentId_ShouldHaveValidationError()
    {
        var command = new ConferAcademicAchievementCommand(
            StudentId: Guid.Empty,
            LearningUnitId: null,
            AcademicPeriodId: Guid.NewGuid(),
            AchievementType: "subject_distinction",
            Title: "Math Distinction",
            Description: null,
            CriteriaMet: "Scored 95+ in all unit tests",
            SupportingEvidence: null,
            RecognizedAt: new DateOnly(2026, 6, 15));

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.StudentId);
    }

    [Fact]
    public void ConferValidator_EmptyAcademicPeriodId_ShouldHaveValidationError()
    {
        var command = new ConferAcademicAchievementCommand(
            StudentId: Guid.NewGuid(),
            LearningUnitId: null,
            AcademicPeriodId: Guid.Empty,
            AchievementType: "subject_distinction",
            Title: "Math Distinction",
            Description: null,
            CriteriaMet: "Scored 95+ in all unit tests",
            SupportingEvidence: null,
            RecognizedAt: new DateOnly(2026, 6, 15));

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.AcademicPeriodId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    [InlineData("AB")]
    public void ConferValidator_TitleLessThan3Chars_ShouldHaveValidationError(string shortTitle)
    {
        var command = new ConferAcademicAchievementCommand(
            StudentId: Guid.NewGuid(),
            LearningUnitId: null,
            AcademicPeriodId: Guid.NewGuid(),
            AchievementType: "subject_distinction",
            Title: shortTitle,
            Description: null,
            CriteriaMet: "Criteria description here",
            SupportingEvidence: null,
            RecognizedAt: new DateOnly(2026, 6, 15));

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Fact]
    public void ConferValidator_TitleExceeds200Chars_ShouldHaveValidationError()
    {
        var command = new ConferAcademicAchievementCommand(
            StudentId: Guid.NewGuid(),
            LearningUnitId: null,
            AcademicPeriodId: Guid.NewGuid(),
            AchievementType: "subject_distinction",
            Title: new string('A', 201),
            Description: null,
            CriteriaMet: "Criteria description here",
            SupportingEvidence: null,
            RecognizedAt: new DateOnly(2026, 6, 15));

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Four")]
    public void ConferValidator_CriteriaMetLessThan5Chars_ShouldHaveValidationError(string shortCriteria)
    {
        var command = new ConferAcademicAchievementCommand(
            StudentId: Guid.NewGuid(),
            LearningUnitId: null,
            AcademicPeriodId: Guid.NewGuid(),
            AchievementType: "subject_distinction",
            Title: "Valid Title",
            Description: null,
            CriteriaMet: shortCriteria,
            SupportingEvidence: null,
            RecognizedAt: new DateOnly(2026, 6, 15));

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.CriteriaMet);
    }

    [Fact]
    public void ConferValidator_CriteriaMetExceeds1000Chars_ShouldHaveValidationError()
    {
        var command = new ConferAcademicAchievementCommand(
            StudentId: Guid.NewGuid(),
            LearningUnitId: null,
            AcademicPeriodId: Guid.NewGuid(),
            AchievementType: "subject_distinction",
            Title: "Valid Title",
            Description: null,
            CriteriaMet: new string('X', 1001),
            SupportingEvidence: null,
            RecognizedAt: new DateOnly(2026, 6, 15));

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.CriteriaMet);
    }

    [Fact]
    public void ConferValidator_DescriptionExceeds1000Chars_ShouldHaveValidationError()
    {
        var command = new ConferAcademicAchievementCommand(
            StudentId: Guid.NewGuid(),
            LearningUnitId: null,
            AcademicPeriodId: Guid.NewGuid(),
            AchievementType: "subject_distinction",
            Title: "Valid Title",
            Description: new string('D', 1001),
            CriteriaMet: "Valid criteria met description",
            SupportingEvidence: null,
            RecognizedAt: new DateOnly(2026, 6, 15));

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Fact]
    public void ConferValidator_SupportingEvidenceExceeds2000Chars_ShouldHaveValidationError()
    {
        var command = new ConferAcademicAchievementCommand(
            StudentId: Guid.NewGuid(),
            LearningUnitId: null,
            AcademicPeriodId: Guid.NewGuid(),
            AchievementType: "subject_distinction",
            Title: "Valid Title",
            Description: null,
            CriteriaMet: "Valid criteria met description",
            SupportingEvidence: new string('E', 2001),
            RecognizedAt: new DateOnly(2026, 6, 15));

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.SupportingEvidence);
    }

    [Fact]
    public void ConferValidator_DefaultRecognizedAt_ShouldHaveValidationError()
    {
        var command = new ConferAcademicAchievementCommand(
            StudentId: Guid.NewGuid(),
            LearningUnitId: null,
            AcademicPeriodId: Guid.NewGuid(),
            AchievementType: "subject_distinction",
            Title: "Valid Title",
            Description: null,
            CriteriaMet: "Valid criteria met description",
            SupportingEvidence: null,
            RecognizedAt: default);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(x => x.RecognizedAt);
    }

    [Fact]
    public void AddAcademicsDomain_Should_RegisterAchievementsHandlers()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAcademicsDomain();

        // Handlers are registered as scoped services in the service collection
        services.Should().Contain(sd => sd.ServiceType == typeof(ConferAcademicAchievementHandler) && sd.Lifetime == ServiceLifetime.Scoped);
        services.Should().Contain(sd => sd.ServiceType == typeof(GetStudentAchievementsHandler) && sd.Lifetime == ServiceLifetime.Scoped);
        services.Should().Contain(sd => sd.ServiceType == typeof(GetPeriodAchievementsHandler) && sd.Lifetime == ServiceLifetime.Scoped);
    }
}
