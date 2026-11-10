namespace Elsasa.Sis.Teaching.Features.TeacherProfiles.GetTeacherProfileById;

using Dapper;
using Elsasa.Sis.Contracts.Teaching;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler retrieving complete faculty dossier for a teacher profile (TD-02, TD-07, P2-S07).
/// </summary>
public sealed class GetTeacherProfileByIdHandler(ISqlConnectionFactory connectionFactory)
{
    static GetTeacherProfileByIdHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _querySql = """
        -- 1. Teacher Profile Core
        SELECT
            tp.id AS Id,
            tp.teacher_number AS TeacherNumber,
            tp.full_name AS FullName,
            tp.display_name AS DisplayName,
            tp.employee_ref AS EmployeeRef,
            tp.national_id_or_external_ref AS NationalIdOrExternalRef,
            tp.email AS Email,
            tp.phone AS Phone,
            tp.teaching_role_id AS TeachingRoleId,
            COALESCE(tr.name, N'') AS TeachingRoleName,
            tp.campus_affiliation AS CampusAffiliation,
            tp.department AS Department,
            tp.standing AS Standing,
            tp.effective_from AS EffectiveFrom,
            tp.effective_to AS EffectiveTo,
            tp.standing_reason AS StandingReason,
            tp.approved_by AS ApprovedBy,
            tp.approved_date AS ApprovedDate,
            tp.notes AS Notes
        FROM teaching.teacher_profiles tp
        LEFT JOIN teaching.teaching_roles tr ON tr.id = tp.teaching_role_id
        WHERE tp.id = @TeacherId;

        -- 2. Teaching Capabilities
        SELECT
            tc.id AS Id,
            tc.teacher_profile_id AS TeacherProfileId,
            tc.capability_type_id AS CapabilityTypeId,
            tc.capability_code AS CapabilityCode,
            tc.capability_name AS CapabilityName,
            tc.applicable_level AS ApplicableLevel,
            tc.delivery_mode AS DeliveryMode,
            tc.language_of_instruction AS LanguageOfInstruction,
            tc.status AS Status,
            tc.notes AS Notes
        FROM teaching.teaching_capabilities tc
        WHERE tc.teacher_profile_id = @TeacherId
        ORDER BY tc.capability_code ASC, tc.created_at ASC;

        -- 3. Qualification Evidence
        SELECT
            qe.id AS Id,
            qe.teacher_profile_id AS TeacherProfileId,
            qe.qualification_type_id AS QualificationTypeId,
            qe.title AS Title,
            qe.issuing_authority AS IssuingAuthority,
            qe.credential_identifier AS CredentialIdentifier,
            qe.issue_date AS IssueDate,
            qe.expiry_date AS ExpiryDate,
            qe.document_reference AS DocumentReference,
            qe.verification_state AS VerificationState,
            qe.verified_by AS VerifiedBy,
            qe.verified_date AS VerifiedDate,
            qe.verification_notes AS VerificationNotes,
            qe.rejection_reason AS RejectionReason,
            qe.restrictions_or_conditions AS RestrictionsOrConditions
        FROM teaching.qualification_evidence qe
        WHERE qe.teacher_profile_id = @TeacherId
        ORDER BY qe.issue_date DESC, qe.created_at DESC;

        -- 4. Teaching Authorizations
        SELECT
            ta.id AS Id,
            ta.authorization_number AS AuthorizationNumber,
            ta.teacher_profile_id AS TeacherProfileId,
            ta.authorized_capability_id AS AuthorizedCapabilityId,
            ta.scope_description AS ScopeDescription,
            ta.campus_scope AS CampusScope,
            ta.program_scope AS ProgramScope,
            ta.level_scope AS LevelScope,
            ta.delivery_mode_scope AS DeliveryModeScope,
            ta.status AS Status,
            ta.unverified_credential_warning AS UnverifiedCredentialWarning,
            ta.effective_start_date AS EffectiveStartDate,
            ta.effective_end_date AS EffectiveEndDate,
            ta.conditions_or_restrictions AS ConditionsOrRestrictions,
            ta.decision_authority AS DecisionAuthority,
            ta.approved_date AS ApprovedDate,
            ta.suspension_or_revocation_reason AS SuspensionOrRevocationReason,
            ta.notes AS Notes
        FROM teaching.teaching_authorizations ta
        WHERE ta.teacher_profile_id = @TeacherId
        ORDER BY ta.effective_start_date DESC, ta.created_at DESC;

        -- 5. Authorization Linked Evidence
        SELECT
            ae.authorization_id AS AuthorizationId,
            ae.qualification_evidence_id AS QualificationEvidenceId
        FROM teaching.authorization_evidence ae
        INNER JOIN teaching.teaching_authorizations ta ON ta.id = ae.authorization_id
        WHERE ta.teacher_profile_id = @TeacherId;

        -- 6. Supervision Requirements
        SELECT
            sr.id AS Id,
            sr.authorization_id AS AuthorizationId,
            sr.supervising_teacher_id AS SupervisingTeacherId,
            COALESCE(mentor.display_name, mentor.full_name) AS SupervisingTeacherName,
            sr.supervision_type AS SupervisionType,
            sr.description AS Description,
            sr.start_date AS StartDate,
            sr.end_date AS EndDate,
            sr.status AS Status,
            sr.resolution_notes AS ResolutionNotes
        FROM teaching.supervision_requirements sr
        INNER JOIN teaching.teaching_authorizations ta ON ta.id = sr.authorization_id
        LEFT JOIN teaching.teacher_profiles mentor ON mentor.id = sr.supervising_teacher_id
        WHERE ta.teacher_profile_id = @TeacherId
        ORDER BY sr.start_date DESC, sr.created_at DESC;

        -- 7. Recent Record Corrections
        SELECT
            trc.id AS Id,
            trc.correction_number AS CorrectionNumber,
            trc.target_table_name AS TargetTableName,
            trc.target_record_id AS TargetRecordId,
            trc.teacher_profile_id AS TeacherProfileId,
            trc.field_name AS FieldName,
            trc.original_value AS OriginalValue,
            trc.corrected_value AS CorrectedValue,
            trc.reason_for_correction AS ReasonForCorrection,
            trc.supporting_evidence AS SupportingEvidence,
            trc.downstream_impact_reviewed AS DownstreamImpactReviewed,
            trc.effective_date_implications AS EffectiveDateImplications,
            trc.requested_by AS RequestedBy,
            trc.requested_date AS RequestedDate,
            trc.approved_by AS ApprovedBy,
            trc.approved_date AS ApprovedDate,
            trc.status AS Status
        FROM teaching.teaching_record_corrections trc
        WHERE trc.teacher_profile_id = @TeacherId
        ORDER BY trc.requested_date DESC, trc.created_at DESC;
        """;

    /// <summary>
    /// Retrieves the complete dossier for the requested teacher profile ID.
    /// </summary>
    public async Task<Result<TeacherProfileDto>> HandleAsync(
        Guid teacherId,
        CancellationToken cancellationToken = default)
    {
        if (teacherId == Guid.Empty)
        {
            return Result.Failure<TeacherProfileDto>(
                DomainError.Validation("Teaching.EmptyTeacherId", "Teacher ID cannot be empty."));
        }

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        using var multi = await connection.QueryMultipleAsync(
            new CommandDefinition(_querySql, new { TeacherId = teacherId }, cancellationToken: cancellationToken));

        var rawProfile = await multi.ReadSingleOrDefaultAsync<TeacherProfileRaw>();
        if (rawProfile is null)
        {
            return Result.Failure<TeacherProfileDto>(
                DomainError.NotFound("Teaching.TeacherNotFound", $"Teacher profile with ID '{teacherId}' was not found."));
        }

        var capabilities = (await multi.ReadAsync<TeachingCapabilityDto>()).AsList();
        var qualifications = (await multi.ReadAsync<QualificationEvidenceDto>()).AsList();
        var rawAuthorizations = (await multi.ReadAsync<TeachingAuthorizationRaw>()).AsList();
        var linkedEvidenceRows = (await multi.ReadAsync<LinkedEvidenceRow>()).AsList();
        var supervisionRows = (await multi.ReadAsync<SupervisionRequirementDto>()).AsList();
        var corrections = (await multi.ReadAsync<TeachingRecordCorrectionDto>()).AsList();

        var evidenceLookup = linkedEvidenceRows.ToLookup(r => r.AuthorizationId, r => r.QualificationEvidenceId);
        var supervisionLookup = supervisionRows.ToLookup(r => r.AuthorizationId);

        var finalAuthorizations = rawAuthorizations.Select(a => new TeachingAuthorizationDto(
            a.Id,
            a.AuthorizationNumber,
            a.TeacherProfileId,
            a.AuthorizedCapabilityId,
            a.ScopeDescription,
            a.CampusScope,
            a.ProgramScope,
            a.LevelScope,
            a.DeliveryModeScope,
            a.Status,
            a.UnverifiedCredentialWarning,
            a.EffectiveStartDate,
            a.EffectiveEndDate,
            a.ConditionsOrRestrictions,
            a.DecisionAuthority,
            a.ApprovedDate,
            a.SuspensionOrRevocationReason,
            a.Notes,
            evidenceLookup[a.Id].ToList(),
            supervisionLookup[a.Id].ToList()
        )).ToList();

        var profileDto = new TeacherProfileDto(
            rawProfile.Id,
            rawProfile.TeacherNumber,
            rawProfile.FullName,
            rawProfile.DisplayName,
            rawProfile.EmployeeRef,
            rawProfile.NationalIdOrExternalRef,
            rawProfile.Email,
            rawProfile.Phone,
            rawProfile.TeachingRoleId,
            rawProfile.TeachingRoleName,
            rawProfile.CampusAffiliation,
            rawProfile.Department,
            rawProfile.Standing,
            rawProfile.EffectiveFrom,
            rawProfile.EffectiveTo,
            rawProfile.StandingReason,
            rawProfile.ApprovedBy,
            rawProfile.ApprovedDate,
            rawProfile.Notes,
            capabilities,
            qualifications,
            finalAuthorizations,
            corrections);

        return Result.Success(profileDto);
    }

    private sealed record TeacherProfileRaw(
        Guid Id,
        string TeacherNumber,
        string FullName,
        string? DisplayName,
        string? EmployeeRef,
        string? NationalIdOrExternalRef,
        string? Email,
        string? Phone,
        Guid TeachingRoleId,
        string TeachingRoleName,
        string CampusAffiliation,
        string? Department,
        string Standing,
        DateOnly EffectiveFrom,
        DateOnly? EffectiveTo,
        string? StandingReason,
        string? ApprovedBy,
        DateOnly? ApprovedDate,
        string? Notes);

    private sealed record TeachingAuthorizationRaw(
        Guid Id,
        string AuthorizationNumber,
        Guid TeacherProfileId,
        Guid? AuthorizedCapabilityId,
        string ScopeDescription,
        string CampusScope,
        string? ProgramScope,
        string? LevelScope,
        string? DeliveryModeScope,
        string Status,
        bool UnverifiedCredentialWarning,
        DateOnly EffectiveStartDate,
        DateOnly? EffectiveEndDate,
        string? ConditionsOrRestrictions,
        string DecisionAuthority,
        DateOnly ApprovedDate,
        string? SuspensionOrRevocationReason,
        string? Notes);

    private sealed record LinkedEvidenceRow(
        Guid AuthorizationId,
        Guid QualificationEvidenceId);
}
