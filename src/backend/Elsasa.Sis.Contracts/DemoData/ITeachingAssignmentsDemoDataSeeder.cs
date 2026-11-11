namespace Elsasa.Sis.Contracts.DemoData;

using Elsasa.Sis.Contracts.Academics;
using Elsasa.Sis.Contracts.Operations;
using Elsasa.Sis.Contracts.Students;

/// <summary>
/// Seeds the Teaching domain Stage 2 data (demands, assignments, homerooms, availability, workload commitments,
/// exceptions, substitutions, reassignments, completions, history) binding real learning units and student cohorts
/// to Hagios teacher personas.
/// </summary>
public interface ITeachingAssignmentsDemoDataSeeder
{
    /// <summary>
    /// Seeds Stage 2 teaching data using Operations, Academics, Students, and Stage 1 Teaching context.
    /// </summary>
    /// <param name="operationsContext">The operations seed context containing academic year and period identifiers.</param>
    /// <param name="academicsContext">The academics structure seed context containing created learning unit identifiers.</param>
    /// <param name="studentsContext">The students seed context containing created cohort identifiers.</param>
    /// <param name="teachingContext">The stage 1 teaching seed context containing teacher persona profiles.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result summary of seeded teaching assignments and workload entities.</returns>
    Task<TeachingAssignmentsSeedResult> SeedTeachingAssignmentsAsync(
        OperationsSeedResult operationsContext,
        AcademicsStructureSeedResult academicsContext,
        StudentsSeedResult studentsContext,
        TeachingSeedResult teachingContext,
        CancellationToken cancellationToken = default);
}
