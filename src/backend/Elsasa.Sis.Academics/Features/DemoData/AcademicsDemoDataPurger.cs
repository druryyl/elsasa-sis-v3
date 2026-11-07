namespace Elsasa.Sis.Academics.Features.DemoData;

using Dapper;
using Elsasa.Sis.Contracts.DemoData;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Autonomous purger for the Academics domain schema (ADR-002, ADR-004).
/// Deletes all demo data from academics.* tables in strict reverse dependency order.
/// Handles child learning units before parent learning units to respect FK constraints.
/// </summary>
public sealed class AcademicsDemoDataPurger(
    ISqlConnectionFactory connectionFactory) : IAcademicsDemoDataPurger
{
    private const string _purgeSql = """
        DELETE FROM academics.academic_achievements;
        DELETE FROM academics.promotion_recommendations;
        DELETE FROM academics.academic_standings;
        DELETE FROM academics.academic_interventions;
        DELETE FROM academics.learning_progress;
        DELETE FROM academics.transcripts;
        DELETE FROM academics.report_card_entries;
        DELETE FROM academics.report_cards;
        DELETE FROM academics.academic_record_corrections;
        DELETE FROM academics.academic_results;
        DELETE FROM academics.assessment_evidence;
        DELETE FROM academics.assessments;
        DELETE FROM academics.submissions;
        DELETE FROM academics.assignments;
        DELETE FROM academics.attendance_corrections;
        DELETE FROM academics.attendance_records;
        DELETE FROM academics.learning_activities;
        DELETE FROM academics.academic_participations;
        DELETE FROM academics.academic_requirements;
        DELETE FROM academics.learning_units WHERE parent_unit_id IS NOT NULL;
        DELETE FROM academics.learning_units WHERE parent_unit_id IS NULL;
        DELETE FROM academics.learning_structures;
        """;

    public async Task PurgeDomainDataAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        await connection.ExecuteAsync(new CommandDefinition(_purgeSql, cancellationToken: cancellationToken));
    }
}