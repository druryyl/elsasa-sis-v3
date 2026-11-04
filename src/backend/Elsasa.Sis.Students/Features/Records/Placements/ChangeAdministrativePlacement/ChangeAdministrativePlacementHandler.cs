namespace Elsasa.Sis.Students.Features.Records.Placements.ChangeAdministrativePlacement;

using Dapper;
using Elsasa.Sis.Contracts.Students.Events;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using MediatR;

/// <summary>
/// Feature handler executing effective-dated administrative placement reassignments (ADR-004, UC-STD-06, STD-WF-05, TD-04).
/// Enforces student existence, active placement presence, forward/equal effective dating,
/// atomic record closure and replacement, immutable history logging, and post-commit event publishing.
/// </summary>
public sealed class ChangeAdministrativePlacementHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser,
    IPublisher publisher)
{
    private const string _selectStudentExistsSql = """
        SELECT TOP 1 1
        FROM students.students
        WHERE id = @Id;
        """;

    private const string _selectActivePlacementSql = """
        SELECT TOP 1
            id AS Id,
            effective_date AS EffectiveDate
        FROM students.administrative_placements
        WHERE student_id = @StudentId AND is_current = 1;
        """;

    private const string _closeActivePlacementSql = """
        UPDATE students.administrative_placements
        SET is_current = 0,
            end_date = @EffectiveDate,
            updated_at = SYSUTCDATETIME(),
            updated_by = @UpdatedBy
        WHERE student_id = @StudentId AND is_current = 1;
        """;

    private const string _insertPlacementSql = """
        INSERT INTO students.administrative_placements
        (
            id,
            student_id,
            campus,
            school_level,
            grade,
            academic_year,
            effective_date,
            program,
            stream,
            cohort,
            homeroom,
            end_date,
            is_current,
            reason,
            authorized_by,
            decision_reference,
            created_at,
            created_by
        )
        VALUES
        (
            @Id,
            @StudentId,
            @Campus,
            @SchoolLevel,
            @Grade,
            @AcademicYear,
            @EffectiveDate,
            @Program,
            @Stream,
            @Cohort,
            @Homeroom,
            NULL,
            1,
            @Reason,
            @AuthorizedBy,
            @DecisionReference,
            SYSUTCDATETIME(),
            @CreatedBy
        );
        """;

    private const string _insertHistorySql = """
        INSERT INTO students.administrative_history_entries
        (
            id,
            student_id,
            event_type,
            event_date,
            detail,
            authorized_by,
            decision_reference,
            is_correction,
            corrects_entry_id,
            created_at,
            created_by
        )
        VALUES
        (
            @Id,
            @StudentId,
            'placement_changed',
            @EventDate,
            @Detail,
            @AuthorizedBy,
            @DecisionReference,
            0,
            NULL,
            SYSUTCDATETIME(),
            @CreatedBy
        );
        """;

    /// <summary>
    /// Executes the administrative placement reassignment command.
    /// </summary>
    /// <param name="studentId">The identifier of the student being reassigned.</param>
    /// <param name="request">The reassignment parameters.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A result containing the new placement response or domain error.</returns>
    public async Task<Result<ChangeAdministrativePlacementResponse>> HandleAsync(
        Guid studentId,
        ChangeAdministrativePlacementRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Verify student exists in students.students
        var studentExists = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            _selectStudentExistsSql,
            new { Id = studentId },
            cancellationToken: cancellationToken));

        if (!studentExists)
        {
            return Result.Failure<ChangeAdministrativePlacementResponse>(DomainError.NotFound(
                "Students.StudentNotFound",
                $"Student with ID '{studentId}' was not found."));
        }

        // 2. Query current active placement
        var currentPlacement = await connection.QuerySingleOrDefaultAsync<ActivePlacementRow>(new CommandDefinition(
            _selectActivePlacementSql,
            new { StudentId = studentId },
            cancellationToken: cancellationToken));

        if (currentPlacement is null)
        {
            return Result.Failure<ChangeAdministrativePlacementResponse>(DomainError.NotFound(
                "Students.Placements.ActivePlacementNotFound",
                "No active placement found for this student."));
        }

        // 3. Validate EffectiveDate >= currentPlacement.EffectiveDate
        if (request.EffectiveDate < currentPlacement.EffectiveDate)
        {
            return Result.Failure<ChangeAdministrativePlacementResponse>(DomainError.Validation(
                "Students.Placements.EffectiveDatePriorToCurrent",
                "Effective date cannot precede current placement effective date."));
        }

        var auditUser = currentUser.Username ?? "system";
        var newPlacementId = Guid.NewGuid();
        var historyId = Guid.NewGuid();

        var normalizedCampus = request.Campus.Trim();
        var normalizedSchoolLevel = request.SchoolLevel.Trim();
        var normalizedGrade = request.Grade.Trim();
        var normalizedAcademicYear = request.AcademicYear.Trim();
        var normalizedProgram = string.IsNullOrWhiteSpace(request.Program) ? null : request.Program.Trim();
        var normalizedStream = string.IsNullOrWhiteSpace(request.Stream) ? null : request.Stream.Trim();
        var normalizedCohort = string.IsNullOrWhiteSpace(request.Cohort) ? null : request.Cohort.Trim();
        var normalizedHomeroom = string.IsNullOrWhiteSpace(request.Homeroom) ? null : request.Homeroom.Trim();
        var normalizedReason = string.IsNullOrWhiteSpace(request.Reason) ? null : request.Reason.Trim();
        var authorizedBy = string.IsNullOrWhiteSpace(request.AuthorizedBy) ? auditUser : request.AuthorizedBy.Trim();
        var decisionReference = string.IsNullOrWhiteSpace(request.DecisionReference) ? null : request.DecisionReference.Trim();

        var cohortDisplay = normalizedCohort ?? "-";
        var homeroomDisplay = normalizedHomeroom ?? "-";
        var historyDetail = $"Reassigned to {normalizedCampus}, {normalizedSchoolLevel}, {normalizedGrade}, Cohort: {cohortDisplay}, Homeroom: {homeroomDisplay}";

        // 4. Open atomic SqlTransaction (close old active row before inserting new active row to maintain ux partial unique index)
        await using var tx = await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            await connection.ExecuteAsync(new CommandDefinition(
                _closeActivePlacementSql,
                new
                {
                    StudentId = studentId,
                    EffectiveDate = request.EffectiveDate,
                    UpdatedBy = auditUser,
                },
                transaction: tx,
                cancellationToken: cancellationToken));

            await connection.ExecuteAsync(new CommandDefinition(
                _insertPlacementSql,
                new
                {
                    Id = newPlacementId,
                    StudentId = studentId,
                    Campus = normalizedCampus,
                    SchoolLevel = normalizedSchoolLevel,
                    Grade = normalizedGrade,
                    AcademicYear = normalizedAcademicYear,
                    EffectiveDate = request.EffectiveDate,
                    Program = normalizedProgram,
                    Stream = normalizedStream,
                    Cohort = normalizedCohort,
                    Homeroom = normalizedHomeroom,
                    Reason = normalizedReason,
                    AuthorizedBy = authorizedBy,
                    DecisionReference = decisionReference,
                    CreatedBy = auditUser,
                },
                transaction: tx,
                cancellationToken: cancellationToken));

            await connection.ExecuteAsync(new CommandDefinition(
                _insertHistorySql,
                new
                {
                    Id = historyId,
                    StudentId = studentId,
                    EventDate = request.EffectiveDate,
                    Detail = historyDetail,
                    AuthorizedBy = authorizedBy,
                    DecisionReference = decisionReference,
                    CreatedBy = auditUser,
                },
                transaction: tx,
                cancellationToken: cancellationToken));

            await tx.CommitAsync(cancellationToken);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }

        // 5. Post-commit event publishing
        await publisher.Publish(
            new PlacementChangedEvent(
                studentId,
                newPlacementId,
                normalizedCampus,
                normalizedSchoolLevel,
                normalizedGrade,
                normalizedAcademicYear,
                request.EffectiveDate,
                normalizedProgram,
                normalizedStream,
                normalizedCohort,
                normalizedHomeroom),
            cancellationToken);

        // 6. Return successful response
        return Result.Success(new ChangeAdministrativePlacementResponse(
            newPlacementId,
            studentId,
            IsCurrent: true));
    }

    private sealed record ActivePlacementRow(Guid Id, DateOnly EffectiveDate);
}
