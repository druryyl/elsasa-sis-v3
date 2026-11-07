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

    [Fact]
    public void Submodule5PersonaConstants_Should_BeConfiguredWithDeterministicGuids()
    {
        AcademicsActivitiesDemoDataSeeder.BudiSlpV1Id.Should().NotBeEmpty();
        AcademicsActivitiesDemoDataSeeder.BudiSlpV2Id.Should().NotBeEmpty();
        AcademicsActivitiesDemoDataSeeder.TimotiusSlpV1Id.Should().NotBeEmpty();

        AcademicsActivitiesDemoDataSeeder.BudiSlpV1Id.Should().NotBe(AcademicsActivitiesDemoDataSeeder.BudiSlpV2Id);
        AcademicsActivitiesDemoDataSeeder.BudiSlpV2Id.Should().NotBe(AcademicsActivitiesDemoDataSeeder.TimotiusSlpV1Id);

        AcademicsActivitiesDemoDataSeeder.LuMat3S1Id.Should().NotBeEmpty();
        AcademicsActivitiesDemoDataSeeder.LuBi4S1Id.Should().NotBeEmpty();
        AcademicsActivitiesDemoDataSeeder.LuPjok4S1Id.Should().NotBeEmpty();
    }

    [Fact]
    public void AcademicsDemoDataPurger_PurgeSql_Should_DeleteSubmodule5InStrictReverseDependencyOrder()
    {
        var purgerType = typeof(AcademicsDemoDataPurger);
        var field = purgerType.GetField("_purgeSql", BindingFlags.Static | BindingFlags.NonPublic);
        field.Should().NotBeNull("purger must have _purgeSql constant");

        var sql = (string)field!.GetValue(null)!;
        sql.Should().NotBeNullOrWhiteSpace();

        // Verify Submodule 5 tables are deleted in exact reverse-dependency order:
        // 1. academic_participations (clears SLP references)
        // 2. slp_approvals
        // 3. academic_plan_adjustments
        // 4. slp_learning_units
        // 5. student_learning_plans
        // 6. academic_requirements / learning_units / learning_structures
        var participationsIndex = sql.IndexOf("DELETE FROM academics.academic_participations;", StringComparison.OrdinalIgnoreCase);
        var approvalsIndex = sql.IndexOf("DELETE FROM academics.slp_approvals;", StringComparison.OrdinalIgnoreCase);
        var adjustmentsIndex = sql.IndexOf("DELETE FROM academics.academic_plan_adjustments;", StringComparison.OrdinalIgnoreCase);
        var unitsIndex = sql.IndexOf("DELETE FROM academics.slp_learning_units;", StringComparison.OrdinalIgnoreCase);
        var plansIndex = sql.IndexOf("DELETE FROM academics.student_learning_plans;", StringComparison.OrdinalIgnoreCase);
        var requirementsIndex = sql.IndexOf("DELETE FROM academics.academic_requirements;", StringComparison.OrdinalIgnoreCase);
        var learningUnitsIndex = sql.IndexOf("DELETE FROM academics.learning_units", StringComparison.OrdinalIgnoreCase);

        participationsIndex.Should().BeGreaterThan(-1);
        approvalsIndex.Should().BeGreaterThan(-1);
        adjustmentsIndex.Should().BeGreaterThan(-1);
        unitsIndex.Should().BeGreaterThan(-1);
        plansIndex.Should().BeGreaterThan(-1);
        requirementsIndex.Should().BeGreaterThan(-1);
        learningUnitsIndex.Should().BeGreaterThan(-1);

        // Strict ordering assertions
        participationsIndex.Should().BeLessThan(approvalsIndex, "academic_participations must be deleted before slp_approvals (clears FK references)");
        approvalsIndex.Should().BeLessThan(adjustmentsIndex, "slp_approvals must be deleted before academic_plan_adjustments");
        adjustmentsIndex.Should().BeLessThan(unitsIndex, "academic_plan_adjustments must be deleted before slp_learning_units");
        unitsIndex.Should().BeLessThan(plansIndex, "slp_learning_units must be deleted before student_learning_plans");
        plansIndex.Should().BeLessThan(requirementsIndex, "student_learning_plans must be deleted before academic_requirements");
        plansIndex.Should().BeLessThan(learningUnitsIndex, "student_learning_plans and constituent units must be deleted before learning_units");
    }
}
