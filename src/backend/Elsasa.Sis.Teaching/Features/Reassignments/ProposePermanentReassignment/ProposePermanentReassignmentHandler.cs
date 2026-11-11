namespace Elsasa.Sis.Teaching.Features.Reassignments.ProposePermanentReassignment;

using Dapper;
using Elsasa.Sis.Contracts.Teaching;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler proposing a permanent teaching responsibility reassignment (TD-01, TD-04, P4-S12).
/// </summary>
public sealed class ProposePermanentReassignmentHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    static ProposePermanentReassignmentHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectAssignmentSql = """
        SELECT
            id AS Id,
            assignment_number AS AssignmentNumber,
            teaching_demand_id AS TeachingDemandId,
            teacher_profile_id AS TeacherProfileId,
            status AS Status,
            academic_period_id AS AcademicPeriodId
        FROM teaching.teaching_assignments
        WHERE id = @Id;
        """;

    private const string _selectTeacherStandingSql = """
        SELECT standing AS Standing
        FROM teaching.teacher_profiles
        WHERE id = @Id;
        """;

    private const string _countReassignmentsPrefixSql = """
        SELECT COUNT(1)
        FROM teaching.teaching_reassignments
        WHERE reassignment_number LIKE @Prefix;
        """;

    private const string _checkReassignmentNumberSql = """
        SELECT 1
        FROM teaching.teaching_reassignments
        WHERE reassignment_number = @ReassignmentNumber;
        """;

    private const string _insertReassignmentSql = """
        INSERT INTO teaching.teaching_reassignments (
            id,
            reassignment_number,
            original_assignment_id,
            predecessor_teacher_id,
            new_teacher_id,
            replacement_assignment_id,
            effective_date,
            reassignment_type,
            reason,
            requires_handover,
            handover_status,
            handover_completed_date,
            handover_notes,
            status,
            approved_by,
            approved_date,
            created_at,
            created_by
        )
        VALUES (
            @Id,
            @ReassignmentNumber,
            @OriginalAssignmentId,
            @PredecessorTeacherId,
            @NewTeacherId,
            NULL,
            @EffectiveDate,
            @ReassignmentType,
            @Reason,
            @RequiresHandover,
            'pending',
            NULL,
            NULL,
            'proposed',
            'PENDING',
            @EffectiveDate,
            SYSUTCDATETIME(),
            @CreatedBy
        );
        """;

    public async Task<Result<ProposePermanentReassignmentResponse>> HandleAsync(
        ProposePermanentReassignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Verify original assignment exists and is approved
        var origAssignment = await connection.QuerySingleOrDefaultAsync<AssignmentRow>(new CommandDefinition(
            _selectAssignmentSql,
            new { Id = request.OriginalAssignmentId },
            cancellationToken: cancellationToken));

        if (origAssignment is null)
        {
            return Result.Failure<ProposePermanentReassignmentResponse>(DomainError.NotFound(
                "Teaching.AssignmentNotFound",
                $"Original teaching assignment with ID '{request.OriginalAssignmentId}' was not found."));
        }

        if (!string.Equals(origAssignment.Status, "approved", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<ProposePermanentReassignmentResponse>(DomainError.Conflict(
                "Teaching.InvalidAssignmentStatus",
                $"Original assignment '{origAssignment.AssignmentNumber}' is in '{origAssignment.Status}' status. Only 'approved' assignments can be permanently reassigned."));
        }

        // 2. Verify new teacher exists and is active
        var teacherStanding = await connection.QuerySingleOrDefaultAsync<string>(new CommandDefinition(
            _selectTeacherStandingSql,
            new { Id = request.NewTeacherId },
            cancellationToken: cancellationToken));

        if (teacherStanding is null)
        {
            return Result.Failure<ProposePermanentReassignmentResponse>(DomainError.NotFound(
                "Teaching.TeacherNotFound",
                $"Replacement teacher profile with ID '{request.NewTeacherId}' was not found."));
        }

        if (!string.Equals(teacherStanding, "active", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<ProposePermanentReassignmentResponse>(DomainError.Conflict(
                "Teaching.TeacherNotActive",
                $"Replacement teacher profile has standing '{teacherStanding}'. Only active faculty members can receive reassigned responsibilities."));
        }

        if (origAssignment.TeacherProfileId == request.NewTeacherId)
        {
            return Result.Failure<ProposePermanentReassignmentResponse>(DomainError.Conflict(
                "Teaching.InvalidReassignmentTeacher",
                "Replacement teacher cannot be the same as the current predecessor teacher."));
        }

        // 3. Generate sequential unique reassignment number (REA-YYYY-XXXX)
        var year = DateTime.UtcNow.Year;
        var prefix = $"REA-{year}-";
        var count = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            _countReassignmentsPrefixSql,
            new { Prefix = prefix + "%" },
            cancellationToken: cancellationToken));

        var seq = count + 1;
        string reassignmentNumber;
        while (true)
        {
            var candidate = $"{prefix}{seq:D4}";
            var exists = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
                _checkReassignmentNumberSql,
                new { ReassignmentNumber = candidate },
                cancellationToken: cancellationToken));

            if (!exists)
            {
                reassignmentNumber = candidate;
                break;
            }

            seq++;
        }

        var reassignmentId = Guid.NewGuid();
        var auditUser = !string.IsNullOrWhiteSpace(currentUser.Username) ? currentUser.Username : "Teaching Coordinator";
        var reassignmentType = !string.IsNullOrWhiteSpace(request.ReassignmentType)
            ? request.ReassignmentType.Trim()
            : "permanent_transfer";

        await connection.ExecuteAsync(new CommandDefinition(
            _insertReassignmentSql,
            new
            {
                Id = reassignmentId,
                ReassignmentNumber = reassignmentNumber,
                OriginalAssignmentId = request.OriginalAssignmentId,
                PredecessorTeacherId = origAssignment.TeacherProfileId,
                NewTeacherId = request.NewTeacherId,
                EffectiveDate = request.EffectiveDate,
                ReassignmentType = reassignmentType,
                Reason = request.Reason.Trim(),
                RequiresHandover = request.RequiresHandover,
                CreatedBy = auditUser
            },
            cancellationToken: cancellationToken));

        var dto = new TeachingReassignmentDto(
            Id: reassignmentId,
            ReassignmentNumber: reassignmentNumber,
            OriginalAssignmentId: request.OriginalAssignmentId,
            PredecessorTeacherId: origAssignment.TeacherProfileId,
            NewTeacherId: request.NewTeacherId,
            ReplacementAssignmentId: null,
            EffectiveDate: request.EffectiveDate,
            ReassignmentType: reassignmentType,
            Reason: request.Reason.Trim(),
            RequiresHandover: request.RequiresHandover,
            HandoverStatus: "pending",
            HandoverCompletedDate: null,
            HandoverNotes: null,
            Status: "proposed",
            ApprovedBy: "PENDING",
            ApprovedDate: request.EffectiveDate,
            CreatedAt: DateTimeOffset.UtcNow);

        return Result.Success(new ProposePermanentReassignmentResponse(dto));
    }

    private sealed record AssignmentRow(
        Guid Id,
        string AssignmentNumber,
        Guid TeachingDemandId,
        Guid TeacherProfileId,
        string Status,
        Guid AcademicPeriodId);
}
