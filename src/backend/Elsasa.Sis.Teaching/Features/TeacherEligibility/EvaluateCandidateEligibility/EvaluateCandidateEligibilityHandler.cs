namespace Elsasa.Sis.Teaching.Features.TeacherEligibility.EvaluateCandidateEligibility;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler executing the automated 5-check Teacher Eligibility Evaluation Engine in memory (TD-03, P2-S05).
/// Generates human-readable natural language explanation and persists the result to teaching.teacher_eligibility_evaluations.
/// </summary>
public sealed class EvaluateCandidateEligibilityHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    static EvaluateCandidateEligibilityHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectDemandSql = """
        SELECT
            d.id AS Id,
            d.demand_code AS DemandCode,
            d.academic_period_id AS AcademicPeriodId,
            d.required_capability_code AS RequiredCapabilityCode,
            d.required_assignment_role_id AS RequiredAssignmentRoleId,
            d.expected_workload_hours AS ExpectedWorkloadHours,
            d.requested_start_date AS RequestedStartDate,
            d.requested_end_date AS RequestedEndDate,
            d.campus_scope AS CampusScope,
            d.status AS Status
        FROM teaching.teaching_demands d
        WHERE d.id = @DemandId;
        """;

    private const string _selectTeacherSql = """
        SELECT
            tp.id AS Id,
            tp.teacher_number AS TeacherNumber,
            tp.full_name AS FullName,
            tp.standing AS Standing,
            tp.campus_affiliation AS CampusAffiliation,
            tp.teaching_role_id AS TeachingRoleId,
            tr.requires_qualification_check AS RoleRequiresQualificationCheck
        FROM teaching.teacher_profiles tp
        INNER JOIN teaching.teaching_roles tr ON tr.id = tp.teaching_role_id
        WHERE tp.id = @TeacherProfileId;
        """;

    private const string _selectAuthorizationsSql = """
        SELECT
            a.id AS Id,
            a.authorization_number AS AuthorizationNumber,
            a.status AS Status,
            a.campus_scope AS CampusScope,
            a.effective_start_date AS EffectiveStartDate,
            a.effective_end_date AS EffectiveEndDate,
            a.unverified_credential_warning AS UnverifiedCredentialWarning,
            c.capability_code AS CapabilityCode
        FROM teaching.teaching_authorizations a
        LEFT JOIN teaching.teaching_capabilities c ON c.id = a.authorized_capability_id
        WHERE a.teacher_profile_id = @TeacherProfileId
          AND a.status IN ('active', 'authorized_under_supervision')
          AND (
              c.capability_code = @RequiredCapabilityCode
              OR a.scope_description LIKE '%' + @RequiredCapabilityCode + '%'
          )
          AND (
              a.campus_scope = 'All'
              OR a.campus_scope = @CampusScope
              OR @CampusScope = 'All'
          )
          AND a.effective_start_date <= @RequestedStartDate
          AND (a.effective_end_date IS NULL OR a.effective_end_date >= @RequestedEndDate);
        """;

    private const string _countVerifiedQualificationsSql = """
        SELECT COUNT(1)
        FROM teaching.qualification_evidence qe
        WHERE qe.teacher_profile_id = @TeacherProfileId
          AND qe.verification_state = 'verified'
          AND (qe.expiry_date IS NULL OR qe.expiry_date >= @RequestedEndDate);
        """;

    private const string _selectOverlappingLeavesSql = """
        SELECT
            uw.id AS Id,
            uw.unavailability_type AS UnavailabilityType,
            uw.reason AS Reason,
            uw.start_date AS StartDate,
            uw.end_date AS EndDate
        FROM teaching.teacher_unavailability_windows uw
        WHERE uw.teacher_profile_id = @TeacherProfileId
          AND uw.status = 'active'
          AND uw.start_date <= @RequestedEndDate
          AND uw.end_date >= @RequestedStartDate;
        """;

    private const string _selectAvailabilitySql = """
        SELECT
            ta.id AS Id,
            ta.max_available_hours AS MaxAvailableHours,
            ta.recurring_available_schedule AS RecurringAvailableSchedule
        FROM teaching.teacher_availabilities ta
        WHERE ta.teacher_profile_id = @TeacherProfileId
          AND ta.academic_period_id = @AcademicPeriodId
          AND ta.status = 'active';
        """;

    private const string _selectWorkloadCommitmentSql = """
        SELECT
            wc.committed_workload_hours AS CommittedWorkloadHours,
            wc.max_capacity_limit_hours AS MaxCapacityLimitHours,
            wc.remaining_capacity_hours AS RemainingCapacityHours,
            wc.approved_exception_count AS ApprovedExceptionCount
        FROM teaching.workload_commitments wc
        WHERE wc.teacher_profile_id = @TeacherProfileId
          AND wc.academic_period_id = @AcademicPeriodId;
        """;

    private const string _selectActivePolicyCapacitySql = """
        SELECT TOP 1 max_standard_capacity
        FROM teaching.workload_policies
        WHERE status = 'active'
        ORDER BY effective_start_date DESC;
        """;

    private const string _sumApprovedAssignmentsHoursSql = """
        SELECT COALESCE(SUM(workload_commitment_value), 0.00)
        FROM teaching.teaching_assignments
        WHERE teacher_profile_id = @TeacherProfileId
          AND academic_period_id = @AcademicPeriodId
          AND status = 'approved';
        """;

    private const string _countApprovedExceptionsSql = """
        SELECT COUNT(1)
        FROM teaching.workload_exceptions we
        WHERE we.teacher_profile_id = @TeacherProfileId
          AND we.status = 'approved'
          AND we.effective_start_date <= @RequestedEndDate
          AND we.effective_end_date >= @RequestedStartDate;
        """;

    private const string _insertEvaluationSql = """
        INSERT INTO teaching.teacher_eligibility_evaluations (
            id,
            teaching_demand_id,
            teacher_profile_id,
            evaluation_date,
            evaluated_by,
            is_eligible,
            eligibility_status,
            profile_active_check,
            authorization_check_passed,
            qualification_check_passed,
            availability_check_passed,
            capacity_check_passed,
            explanation,
            notes,
            created_at,
            created_by
        )
        VALUES (
            @Id,
            @TeachingDemandId,
            @TeacherProfileId,
            @EvaluationDate,
            @EvaluatedBy,
            @IsEligible,
            @EligibilityStatus,
            @ProfileActiveCheck,
            @AuthorizationCheckPassed,
            @QualificationCheckPassed,
            @AvailabilityCheckPassed,
            @CapacityCheckPassed,
            @Explanation,
            @Notes,
            SYSUTCDATETIME(),
            @CreatedBy
        );
        """;

    public async Task<Result<EvaluateCandidateEligibilityResponse>> HandleAsync(
        EvaluateCandidateEligibilityRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Fetch Demand Details
        var demand = await connection.QuerySingleOrDefaultAsync<DemandRow>(new CommandDefinition(
            _selectDemandSql,
            new { DemandId = request.TeachingDemandId },
            cancellationToken: cancellationToken));

        if (demand is null)
        {
            return Result.Failure<EvaluateCandidateEligibilityResponse>(DomainError.NotFound(
                "Teaching.DemandNotFound",
                $"Teaching demand with ID '{request.TeachingDemandId}' was not found."));
        }

        // 2. Fetch Teacher Profile Details
        var teacher = await connection.QuerySingleOrDefaultAsync<TeacherProfileRow>(new CommandDefinition(
            _selectTeacherSql,
            new { TeacherProfileId = request.TeacherProfileId },
            cancellationToken: cancellationToken));

        if (teacher is null)
        {
            return Result.Failure<EvaluateCandidateEligibilityResponse>(DomainError.NotFound(
                "Teaching.TeacherProfileNotFound",
                $"Teacher profile with ID '{request.TeacherProfileId}' was not found."));
        }

        // --- CHECK 1: Teacher Profile Standing ---
        bool profileActiveCheck = string.Equals(teacher.Standing, "active", StringComparison.OrdinalIgnoreCase);

        // --- CHECK 2: Teaching Authorization ---
        var matchingAuths = (await connection.QueryAsync<AuthorizationRow>(new CommandDefinition(
            _selectAuthorizationsSql,
            new
            {
                TeacherProfileId = request.TeacherProfileId,
                RequiredCapabilityCode = demand.RequiredCapabilityCode,
                CampusScope = demand.CampusScope,
                RequestedStartDate = demand.RequestedStartDate,
                RequestedEndDate = demand.RequestedEndDate
            },
            cancellationToken: cancellationToken))).ToList();

        bool authorizationCheckPassed = matchingAuths.Count > 0;
        bool isUnderSupervision = authorizationCheckPassed &&
                                  matchingAuths.All(a => string.Equals(a.Status, "authorized_under_supervision", StringComparison.OrdinalIgnoreCase));
        string primaryAuthNumber = matchingAuths.FirstOrDefault()?.AuthorizationNumber ?? "N/A";

        // --- CHECK 3: Qualification Evidence ---
        int verifiedQualificationsCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
            _countVerifiedQualificationsSql,
            new
            {
                TeacherProfileId = request.TeacherProfileId,
                RequestedEndDate = demand.RequestedEndDate
            },
            cancellationToken: cancellationToken));

        bool requiresQualificationCheck = teacher.RoleRequiresQualificationCheck ||
                                          matchingAuths.Any(a => a.UnverifiedCredentialWarning);

        bool qualificationCheckPassed = !requiresQualificationCheck || (verifiedQualificationsCount > 0);

        // --- CHECK 4: Availability & Leave Windows ---
        var overlappingLeaves = (await connection.QueryAsync<LeaveWindowRow>(new CommandDefinition(
            _selectOverlappingLeavesSql,
            new
            {
                TeacherProfileId = request.TeacherProfileId,
                RequestedStartDate = demand.RequestedStartDate,
                RequestedEndDate = demand.RequestedEndDate
            },
            cancellationToken: cancellationToken))).ToList();

        var availabilityRecord = await connection.QuerySingleOrDefaultAsync<AvailabilityRow>(new CommandDefinition(
            _selectAvailabilitySql,
            new
            {
                TeacherProfileId = request.TeacherProfileId,
                AcademicPeriodId = demand.AcademicPeriodId
            },
            cancellationToken: cancellationToken));

        bool availabilityCheckPassed = overlappingLeaves.Count == 0;

        // --- CHECK 5: Workload Capacity ---
        var commitment = await connection.QuerySingleOrDefaultAsync<WorkloadCommitmentRow>(new CommandDefinition(
            _selectWorkloadCommitmentSql,
            new
            {
                TeacherProfileId = request.TeacherProfileId,
                AcademicPeriodId = demand.AcademicPeriodId
            },
            cancellationToken: cancellationToken));

        decimal committedHours;
        decimal maxLimitHours;
        int approvedExceptionsCount;

        if (commitment is not null)
        {
            committedHours = commitment.CommittedWorkloadHours;
            maxLimitHours = commitment.MaxCapacityLimitHours;
            approvedExceptionsCount = commitment.ApprovedExceptionCount;
        }
        else
        {
            committedHours = await connection.ExecuteScalarAsync<decimal>(new CommandDefinition(
                _sumApprovedAssignmentsHoursSql,
                new
                {
                    TeacherProfileId = request.TeacherProfileId,
                    AcademicPeriodId = demand.AcademicPeriodId
                },
                cancellationToken: cancellationToken));

            var policyMax = await connection.QueryFirstOrDefaultAsync<decimal?>(new CommandDefinition(
                _selectActivePolicyCapacitySql,
                cancellationToken: cancellationToken));

            maxLimitHours = policyMax ?? 24.00m;

            approvedExceptionsCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition(
                _countApprovedExceptionsSql,
                new
                {
                    TeacherProfileId = request.TeacherProfileId,
                    RequestedStartDate = demand.RequestedStartDate,
                    RequestedEndDate = demand.RequestedEndDate
                },
                cancellationToken: cancellationToken));
        }

        decimal projectedHours = committedHours + demand.ExpectedWorkloadHours;
        bool capacityCheckPassed = (projectedHours <= maxLimitHours) || (approvedExceptionsCount > 0);

        // --- DETERMINATION OF OVERALL ELIGIBILITY & STATUS ---
        bool allPassed = profileActiveCheck &&
                         authorizationCheckPassed &&
                         qualificationCheckPassed &&
                         availabilityCheckPassed &&
                         capacityCheckPassed;

        bool isEligible;
        string eligibilityStatus;

        if (allPassed)
        {
            isEligible = true;
            eligibilityStatus = isUnderSupervision ? "eligible_under_supervision" : "eligible";
        }
        else
        {
            isEligible = false;
            if (!profileActiveCheck || !authorizationCheckPassed || !qualificationCheckPassed)
            {
                eligibilityStatus = "ineligible_unqualified";
            }
            else if (!availabilityCheckPassed)
            {
                eligibilityStatus = "ineligible_conflicted";
            }
            else
            {
                eligibilityStatus = "ineligible_capacity_exceeded";
            }
        }

        // --- NATURAL LANGUAGE EXPLANATION GENERATION ---
        var explanationParts = new List<string>();

        // 1. Standing explanation
        if (profileActiveCheck)
        {
            explanationParts.Add($"Profil pengajar '{teacher.FullName}' berstatus aktif ({teacher.Standing}).");
        }
        else
        {
            explanationParts.Add($"Profil pengajar '{teacher.FullName}' tidak berstatus aktif (status saat ini: '{teacher.Standing}').");
        }

        // 2. Authorization explanation
        if (authorizationCheckPassed)
        {
            if (isUnderSupervision)
            {
                explanationParts.Add($"Memiliki otorisasi mengajar aktif di bawah supervisi ({primaryAuthNumber}) untuk kapabilitas '{demand.RequiredCapabilityCode}' pada kampus '{demand.CampusScope}'.");
            }
            else
            {
                explanationParts.Add($"Memiliki otorisasi mengajar aktif ({primaryAuthNumber}) untuk kapabilitas '{demand.RequiredCapabilityCode}' pada kampus '{demand.CampusScope}'.");
            }
        }
        else
        {
            explanationParts.Add($"Tidak memiliki otorisasi mengajar aktif yang sesuai untuk kapabilitas '{demand.RequiredCapabilityCode}' pada kampus '{demand.CampusScope}' untuk periode {demand.RequestedStartDate:yyyy-MM-dd} s/d {demand.RequestedEndDate:yyyy-MM-dd}.");
        }

        // 3. Qualification explanation
        if (qualificationCheckPassed)
        {
            explanationParts.Add("Kualifikasi dan bukti kompetensi terverifikasi sah.");
        }
        else
        {
            explanationParts.Add("Dokumen bukti kualifikasi yang disyaratkan belum terverifikasi atau tidak valid.");
        }

        // 4. Availability explanation
        if (availabilityCheckPassed)
        {
            explanationParts.Add("Jadwal ketersediaan terverifikasi tanpa konflik jendela ketidakhadiran.");
        }
        else
        {
            var leaveDetails = string.Join("; ", overlappingLeaves.Select(l => $"{l.UnavailabilityType} ({l.StartDate:yyyy-MM-dd} s/d {l.EndDate:yyyy-MM-dd}: {l.Reason})"));
            explanationParts.Add($"Terdapat konflik ketersediaan waktu dengan jendela ketidakhadiran aktif: {leaveDetails}.");
        }

        // 5. Capacity explanation
        decimal remainingHours = maxLimitHours - committedHours;
        if (capacityCheckPassed)
        {
            if (projectedHours <= maxLimitHours)
            {
                explanationParts.Add($"Sisa kapasitas ({remainingHours:F1} jam) mencukupi untuk beban permintaan ({demand.ExpectedWorkloadHours:F1} jam) dalam batas kapasitas ({maxLimitHours:F1} jam).");
            }
            else
            {
                explanationParts.Add($"Beban mengajar ({projectedHours:F1} jam) melampaui batas standar ({maxLimitHours:F1} jam) namun tercakup oleh persetujuan dispensasi workload exception.");
            }
        }
        else
        {
            explanationParts.Add($"Beban mengajar melebihi kapasitas maksimum (beban saat ini {committedHours:F1} jam + permintaan {demand.ExpectedWorkloadHours:F1} jam = {projectedHours:F1} jam > batas {maxLimitHours:F1} jam) tanpa dispensasi workload exception yang disetujui.");
        }

        // Final conclusion
        if (isEligible)
        {
            explanationParts.Add(isUnderSupervision
                ? "Kesimpulan: Memenuhi kriteria staffing di bawah supervisi."
                : "Kesimpulan: Memenuhi seluruh kriteria staffing.");
        }
        else
        {
            explanationParts.Add($"Kesimpulan: Tidak memenuhi kriteria staffing ({eligibilityStatus}).");
        }

        string explanation = string.Join(" ", explanationParts);

        // --- PERSISTENCE INTO teaching.teacher_eligibility_evaluations ---
        var evaluationId = Guid.NewGuid();
        var evaluationDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var auditUser = currentUser.Username ?? "system";
        var notes = string.IsNullOrWhiteSpace(request.CoordinatorRemarks) ? null : request.CoordinatorRemarks.Trim();

        await connection.ExecuteAsync(new CommandDefinition(
            _insertEvaluationSql,
            new
            {
                Id = evaluationId,
                TeachingDemandId = demand.Id,
                TeacherProfileId = teacher.Id,
                EvaluationDate = evaluationDate,
                EvaluatedBy = auditUser,
                IsEligible = isEligible,
                EligibilityStatus = eligibilityStatus,
                ProfileActiveCheck = profileActiveCheck,
                AuthorizationCheckPassed = authorizationCheckPassed,
                QualificationCheckPassed = qualificationCheckPassed,
                AvailabilityCheckPassed = availabilityCheckPassed,
                CapacityCheckPassed = capacityCheckPassed,
                Explanation = explanation,
                Notes = notes,
                CreatedBy = auditUser
            },
            cancellationToken: cancellationToken));

        var response = new EvaluateCandidateEligibilityResponse(
            evaluationId,
            demand.Id,
            teacher.Id,
            demand.DemandCode,
            teacher.FullName,
            teacher.TeacherNumber,
            evaluationDate,
            auditUser,
            isEligible,
            eligibilityStatus,
            profileActiveCheck,
            authorizationCheckPassed,
            qualificationCheckPassed,
            availabilityCheckPassed,
            capacityCheckPassed,
            explanation,
            notes,
            DateTimeOffset.UtcNow);

        return Result.Success(response);
    }

    private sealed record DemandRow(
        Guid Id,
        string DemandCode,
        Guid AcademicPeriodId,
        string RequiredCapabilityCode,
        Guid RequiredAssignmentRoleId,
        decimal ExpectedWorkloadHours,
        DateOnly RequestedStartDate,
        DateOnly RequestedEndDate,
        string CampusScope,
        string Status);

    private sealed record TeacherProfileRow(
        Guid Id,
        string TeacherNumber,
        string FullName,
        string Standing,
        string CampusAffiliation,
        Guid TeachingRoleId,
        bool RoleRequiresQualificationCheck);

    private sealed record AuthorizationRow(
        Guid Id,
        string AuthorizationNumber,
        string Status,
        string CampusScope,
        DateOnly EffectiveStartDate,
        DateOnly? EffectiveEndDate,
        bool UnverifiedCredentialWarning,
        string? CapabilityCode);

    private sealed record LeaveWindowRow(
        Guid Id,
        string UnavailabilityType,
        string Reason,
        DateOnly StartDate,
        DateOnly EndDate);

    private sealed record AvailabilityRow(
        Guid Id,
        decimal MaxAvailableHours,
        string? RecurringAvailableSchedule);

    private sealed record WorkloadCommitmentRow(
        decimal CommittedWorkloadHours,
        decimal MaxCapacityLimitHours,
        decimal RemainingCapacityHours,
        int ApprovedExceptionCount);
}
