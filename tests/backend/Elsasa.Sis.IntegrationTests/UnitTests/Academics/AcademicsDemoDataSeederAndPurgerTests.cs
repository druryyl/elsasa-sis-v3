namespace Elsasa.Sis.IntegrationTests.UnitTests.Academics;

using System.Reflection;
using Elsasa.Sis.Academics.Features.DemoData;
using Elsasa.Sis.Contracts.DemoData;
using FluentAssertions;
using Xunit;

/// <summary>
/// Unit tests verifying Hagios personas demo data seeder extensions and reverse-dependency purger configuration (P5-S14, TD-08, GAP-008).
/// </summary>
public sealed class AcademicsDemoDataSeederAndPurgerTests
{
    [Fact]
    public void FlagshipPersonaConstants_Should_BeConfiguredWithDeterministicGuids()
    {
        AcademicsActivitiesDemoDataSeeder.RinaKusumaStudentId.Should().NotBeEmpty();
        AcademicsActivitiesDemoDataSeeder.BudiSantosoStudentId.Should().Be(DemoDataConstants.BudiStudentId);
        AcademicsActivitiesDemoDataSeeder.TimotiusStudentId.Should().Be(DemoDataConstants.TimotiusApplicantId);
    }

    [Fact]
    public void AcademicsDemoDataPurger_PurgeSql_Should_DeleteSubmodule4InStrictReverseDependencyOrder()
    {
        var purgerType = typeof(AcademicsDemoDataPurger);
        var field = purgerType.GetField("_purgeSql", BindingFlags.Static | BindingFlags.NonPublic);
        field.Should().NotBeNull("purger must have _purgeSql constant");

        var sql = (string)field!.GetValue(null)!;
        sql.Should().NotBeNullOrWhiteSpace();

        // 1. Verify Submodule 4 tables are deleted in exact reverse-dependency order
        var achievementsIndex = sql.IndexOf("DELETE FROM academics.academic_achievements;", StringComparison.OrdinalIgnoreCase);
        var recommendationsIndex = sql.IndexOf("DELETE FROM academics.promotion_recommendations;", StringComparison.OrdinalIgnoreCase);
        var standingsIndex = sql.IndexOf("DELETE FROM academics.academic_standings;", StringComparison.OrdinalIgnoreCase);
        var interventionsIndex = sql.IndexOf("DELETE FROM academics.academic_interventions;", StringComparison.OrdinalIgnoreCase);
        var progressIndex = sql.IndexOf("DELETE FROM academics.learning_progress;", StringComparison.OrdinalIgnoreCase);
        var transcriptsIndex = sql.IndexOf("DELETE FROM academics.transcripts;", StringComparison.OrdinalIgnoreCase);
        var learningUnitsIndex = sql.IndexOf("DELETE FROM academics.learning_units", StringComparison.OrdinalIgnoreCase);

        achievementsIndex.Should().BeGreaterThan(-1);
        recommendationsIndex.Should().BeGreaterThan(-1);
        standingsIndex.Should().BeGreaterThan(-1);
        interventionsIndex.Should().BeGreaterThan(-1);
        progressIndex.Should().BeGreaterThan(-1);
        transcriptsIndex.Should().BeGreaterThan(-1);
        learningUnitsIndex.Should().BeGreaterThan(-1);

        // Strict ordering assertions
        achievementsIndex.Should().BeLessThan(recommendationsIndex, "achievements must be deleted before promotion recommendations");
        recommendationsIndex.Should().BeLessThan(standingsIndex, "promotion recommendations must be deleted before standings (FK constraint)");
        standingsIndex.Should().BeLessThan(interventionsIndex, "standings must be deleted before interventions");
        interventionsIndex.Should().BeLessThan(progressIndex, "interventions must be deleted before learning progress");
        progressIndex.Should().BeLessThan(transcriptsIndex, "learning progress must be deleted before transcripts");
        progressIndex.Should().BeLessThan(learningUnitsIndex, "Submodule 4 tables must be deleted before learning units");
    }
}
