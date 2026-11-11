namespace Elsasa.Sis.Teaching.Features.Workload.RequestWorkloadException;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler for submitting a workload exception recommendation (TD-04, P3-S10).
/// </summary>
public sealed class RequestWorkloadExceptionHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    static RequestWorkloadExceptionHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectTeacherSql = """
        SELECT
            id AS Id,
            standing AS Standing,
            email AS Email,
            employee_ref AS EmployeeRef
        FROM teaching.teacher_profiles
        WHERE id = @TeacherProfileId;
        """;

    private const string _selectAssignmentByIdSql = """
        SELECT
            id AS Id,
            teacher_profile_id AS TeacherProfileId,
            academic_period_id AS AcademicPeriodId,
            effective_start_date AS EffectiveStartDate,
            effective_end_date AS EffectiveEndDate
        FROM teaching.teaching_assignments
        WHERE id = @AssignmentId;
        """;

    private const string _selectTeacherAssignmentInPeriodSql = """
        SELECT TOP 1
            id AS Id,
            teacher_profile_id AS TeacherProfileId,
            academic_period_id AS AcademicPeriodId,
            effective_start_date AS EffectiveStartDate,
            effective_end_date AS EffectiveEndDate
        FROM teaching.teaching_assignments
        WHERE teacher_profile_id = @TeacherProfileId
          AND academic_period_id = @AcademicPeriodId
        ORDER BY created_at DESC;
        """;

    private const string _selectCommitmentPolicySql = """
        SELECT TOP 1 workload_policy_id
        FROM teaching.workload_commitments
        WHERE teacher_profile_id = @TeacherProfileId
          AND academic_period_id = @AcademicPeriodId;
        """;

    private const string _selectActiveWorkloadPolicySql = """
        SELECT TOP 1 id
        FROM teaching.workload_policies
        WHERE status = 'active'
        ORDER BY effective_start_date DESC, created_at DESC;
        """;

    private const string _countExceptionNumberPrefixSql = """
        SELECT COUNT(1)
        FROM teaching.workload_exceptions
        WHERE exception_number LIKE @Prefix;
        """;

    private const string _checkExceptionNumberSql = """
        SELECT COUNT(1)
        FROM teaching.workload_exceptions
        WHERE exception_number = @ExceptionNumber;
        """;

    private const string _insertExceptionSql = """
        INSERT INTO teaching.workload_exceptions (
            id,
            exception_number,
            teaching_assignment_id,
            teacher_profile_id,
            workload_policy_id,
            exceeded_hours,
            justification,
            recommended_by,
            recommended_date,
            approved_by,
            approved_date,
            effective_start_date,
            effective_end_date,
            status,
            decision_remarks,
            created_at,
            created_by
        )
        VALUES (
            @Id,
            @ExceptionNumber,
            @TeachingAssignmentId,
            @TeacherProfileId,
            @WorkloadPolicyId,
            @ExceededHours,
            @Justification,
            @RecommendedBy,
            @RecommendedDate,
            NULL,
            NULL,
            @EffectiveStartDate,
            @EffectiveEndDate,
            @Status,
            NULL,
            SYSUTCDATETIME(),
            @CreatedBy
        );
        """;

    public async Task<Result<RequestWorkloadExceptionResponse>> HandleAsync(
        RequestWorkloadExceptionRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Verify target teacher exists
        var teacher = await connection.QuerySingleOrDefaultAsync<TeacherProfileSummary>(new CommandDefinition(
            _selectTeacherSql,
            new { TeacherProfileId = request.TeacherProfileId },
            cancellationToken: cancellationToken));

        if (teacher is null)
        {
            return Result.Failure<RequestWorkloadExceptionResponse>(DomainError.NotFound(
                "Teaching.TeacherNotFound",
                $"Teacher profile with ID '{request.TeacherProfileId}' was not found."));
        }

        // 2. Identify and validate teaching assignment
        AssignmentSummary? assignment;
        if (request.TeachingAssignmentId.HasValue && request.TeachingAssignmentId.Value != Guid.Empty)
        {
            assignment = await connection.QuerySingleOrDefaultAsync<AssignmentSummary>(new CommandDefinition(
                _selectAssignmentByIdSql,
                new { AssignmentId = request.TeachingAssignmentId.Value },
                cancellationToken: cancellationToken));

            if (assignment is null)
            {
                return Result.Failure<RequestWorkloadExceptionResponse>(DomainError.NotFound(
                    "Teaching.AssignmentNotFound",
                    $"Teaching assignment with ID '{request.TeachingAssignmentId.Value}' was not found."));
            }

            if (assignment.TeacherProfileId != request.TeacherProfileId)
            {
                return Result.Failure<RequestWorkloadExceptionResponse>(DomainError.Conflict(
                    "Teaching.AssignmentTeacherMismatch",
                    "The specified teaching assignment belongs to another teacher profile."));
            }

            if (assignment.AcademicPeriodId != request.AcademicPeriodId)
            {
                return Result.Failure<RequestWorkloadExceptionResponse>(DomainError.Conflict(
                    "Teaching.AssignmentPeriodMismatch",
                    "The specified teaching assignment belongs to another academic period."));
            }
        }
        else
        {
            assignment = await connection.QuerySingleOrDefaultAsync<AssignmentSummary>(new CommandDefinition(
                _selectTeacherAssignmentInPeriodSql,
                new
                {
                    TeacherProfileId = request.TeacherProfileId,
                    AcademicPeriodId = request.AcademicPeriodId
                },
                cancellationToken: cancellationToken));

            if (assignment is null)
            {
                return Result.Failure<RequestWorkloadExceptionResponse>(DomainError.Validation(
                    "Teaching.AssignmentRequired",
                    "A teaching assignment in this academic period is required to request a workload exception."));
            }
        }

        // 3. Resolve governing workload policy
        var policyId = await connection.ExecuteScalarAsync<Guid?>(new CommandDefinition(
            _selectCommitmentPolicySql,
            new
            {
                TeacherProfileId = request.TeacherProfileId,
                AcademicPeriodId = request.AcademicPeriodId
            },
            cancellationToken: cancellationToken));

        if (!policyId.HasValue || policyId.Value == Guid.Empty)
        {
            policyId = await connection.ExecuteScalarAsync<Guid?>(new CommandDefinition(
                _selectActiveWorkloadPolicySql,
                cancellationToken: cancellationToken));
        }

        if (!policyId.HasValue || policyId.Value == Guid.Empty)
        {
            return Result.Failure<RequestWorkloadExceptionResponse>(DomainError.NotFound(
                "Teaching.PolicyNotFound",
                "No active workload policy found to govern capacity exceptions."));
        }

        // 4. Generate unique exception number: WEX-YYYY-XXXX
        var year = DateTime.UtcNow.Year;
        var prefix = $"WEX-{year}-";
        var count = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            _countExceptionNumberPrefixSql,
            new { Prefix = prefix + "%" },
            cancellationToken: cancellationToken));

        var seq = count + 1;
        string exceptionNumber;
        while (true)
        {
            var candidate = $"{prefix}{seq:D4}";
            var exists = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                _checkExceptionNumberSql,
                new { ExceptionNumber = candidate },
                cancellationToken: cancellationToken)) > 0;

            if (!exists)
            {
                exceptionNumber = candidate;
                break;
            }

            seq++;
        }

        // 5. Determine effective dates and status
        var exceptionId = Guid.NewGuid();
        var recommendedBy = currentUser.Username ?? "system";
        var recommendedDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var effectiveStartDate = request.EffectiveStartDate ?? assignment.EffectiveStartDate;
        var effectiveEndDate = request.EffectiveEndDate ?? assignment.EffectiveEndDate;
        var status = !string.IsNullOrWhiteSpace(request.Status)
            ? request.Status.Trim().ToLowerInvariant()
            : "requested";

        // 6. Insert workload exception record
        await connection.ExecuteAsync(new CommandDefinition(
            _insertExceptionSql,
            new
            {
                Id = exceptionId,
                ExceptionNumber = exceptionNumber,
                TeachingAssignmentId = assignment.Id,
                TeacherProfileId = request.TeacherProfileId,
                WorkloadPolicyId = policyId.Value,
                ExceededHours = request.RequestedOverloadHours,
                Justification = request.CoordinatorJustification.Trim(),
                RecommendedBy = recommendedBy,
                RecommendedDate = recommendedDate.ToDateTime(TimeOnly.MinValue),
                EffectiveStartDate = effectiveStartDate.ToDateTime(TimeOnly.MinValue),
                EffectiveEndDate = effectiveEndDate.ToDateTime(TimeOnly.MinValue),
                Status = status,
                CreatedBy = recommendedBy
            },
            cancellationToken: cancellationToken));

        return Result.Success(new RequestWorkloadExceptionResponse(
            exceptionId,
            exceptionNumber,
            assignment.Id,
            request.TeacherProfileId,
            request.AcademicPeriodId,
            policyId.Value,
            request.RequestedOverloadHours,
            request.CoordinatorJustification.Trim(),
            recommendedBy,
            recommendedDate,
            effectiveStartDate,
            effectiveEndDate,
            status,
            DateTimeOffset.UtcNow));
    }

    private sealed record TeacherProfileSummary(Guid Id, string Standing, string? Email, string? EmployeeRef);
    private sealed record AssignmentSummary(Guid Id, Guid TeacherProfileId, Guid AcademicPeriodId, DateOnly EffectiveStartDate, DateOnly EffectiveEndDate);
}
