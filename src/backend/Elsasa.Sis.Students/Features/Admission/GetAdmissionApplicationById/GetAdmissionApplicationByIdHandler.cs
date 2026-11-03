namespace Elsasa.Sis.Students.Features.Admission.GetAdmissionApplicationById;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler querying the complete admission intake dossier by application ID (ADR-004, ADR-005).
/// </summary>
public sealed class GetAdmissionApplicationByIdHandler(ISqlConnectionFactory connectionFactory)
{
    private const string _selectDossierMultiSql = """
        SELECT
            app.id AS Id,
            app.application_number AS ApplicationNumber,
            app.applicant_id AS ApplicantId,
            app.admission_period_id AS AdmissionPeriodId,
            app.academic_year_id AS AcademicYearId,
            app.application_type AS ApplicationType,
            app.requested_entry_level AS RequestedEntryLevel,
            app.requested_campus AS RequestedCampus,
            app.requested_program AS RequestedProgram,
            app.registration_date AS RegistrationDate,
            app.status AS Status,
            app.submitted_at AS SubmittedAt,
            app.notes AS Notes,
            app.created_at AS CreatedAt,
            app.created_by AS CreatedBy,
            app.updated_at AS UpdatedAt,
            app.updated_by AS UpdatedBy
        FROM students.admission_applications app
        WHERE app.id = @Id;

        SELECT
            a.id AS Id,
            a.full_name AS FullName,
            a.preferred_name AS PreferredName,
            a.date_of_birth AS DateOfBirth,
            a.place_of_birth AS PlaceOfBirth,
            a.gender AS Gender,
            a.nationality AS Nationality,
            a.government_id_type AS GovernmentIdType,
            a.government_id_number AS GovernmentIdNumber,
            a.email AS Email,
            a.phone AS Phone,
            a.address AS Address,
            a.previous_school_name AS PreviousSchoolName,
            a.previous_school_address AS PreviousSchoolAddress,
            a.notes AS Notes
        FROM students.applicants a
        INNER JOIN students.admission_applications app ON app.applicant_id = a.id
        WHERE app.id = @Id;

        SELECT
            c.id AS Id,
            c.application_id AS ApplicationId,
            c.full_name AS FullName,
            c.relationship AS Relationship,
            c.phone AS Phone,
            c.email AS Email,
            c.address AS Address,
            c.is_primary AS IsPrimary,
            c.created_at AS CreatedAt,
            c.created_by AS CreatedBy
        FROM students.admission_application_contacts c
        WHERE c.application_id = @Id
        ORDER BY c.is_primary DESC, c.created_at ASC;

        SELECT
            d.id AS Id,
            d.application_id AS ApplicationId,
            d.document_type AS DocumentType,
            d.submission_status AS SubmissionStatus,
            d.submitted_date AS SubmittedDate,
            d.verified_date AS VerifiedDate,
            d.verified_by AS VerifiedBy,
            d.file_reference AS FileReference,
            d.file_name AS FileName,
            d.file_size_bytes AS FileSizeBytes,
            d.content_type AS ContentType,
            d.notes AS Notes,
            d.created_at AS CreatedAt,
            d.created_by AS CreatedBy,
            d.updated_at AS UpdatedAt,
            d.updated_by AS UpdatedBy
        FROM students.applicant_documents d
        WHERE d.application_id = @Id
        ORDER BY d.created_at ASC;

        SELECT
            e.id AS Id,
            e.application_id AS ApplicationId,
            e.assessment_type AS AssessmentType,
            e.score AS Score,
            e.source AS Source,
            e.assessment_date AS AssessmentDate,
            e.notes AS Notes,
            e.created_at AS CreatedAt,
            e.created_by AS CreatedBy,
            e.updated_at AS UpdatedAt,
            e.updated_by AS UpdatedBy
        FROM students.entrance_assessment_evidence e
        WHERE e.application_id = @Id
        ORDER BY e.created_at ASC;

        SELECT
            dec.id AS Id,
            dec.application_id AS ApplicationId,
            dec.decision_type AS DecisionType,
            dec.decision_date AS DecisionDate,
            dec.effective_date AS EffectiveDate,
            dec.decided_by AS DecidedBy,
            dec.reason AS Reason,
            dec.supporting_reference AS SupportingReference,
            dec.deferred_admission_period_id AS DeferredAdmissionPeriodId,
            dec.created_at AS CreatedAt,
            dec.created_by AS CreatedBy
        FROM students.admission_decisions dec
        WHERE dec.application_id = @Id;
        """;

    private sealed record ApplicationHeaderRow(
        Guid Id,
        string ApplicationNumber,
        Guid ApplicantId,
        Guid AdmissionPeriodId,
        Guid AcademicYearId,
        string ApplicationType,
        string RequestedEntryLevel,
        string? RequestedCampus,
        string? RequestedProgram,
        DateOnly RegistrationDate,
        string Status,
        DateTimeOffset? SubmittedAt,
        string? Notes,
        DateTimeOffset CreatedAt,
        string? CreatedBy,
        DateTimeOffset? UpdatedAt,
        string? UpdatedBy);

    /// <summary>
    /// Retrieves the entire aggregate dossier for an admission application by its ID.
    /// </summary>
    public async Task<Result<AdmissionApplicationDetailResponse>> HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        using var multi = await connection.QueryMultipleAsync(new CommandDefinition(
            _selectDossierMultiSql,
            new { Id = id },
            cancellationToken: cancellationToken));

        var appHeader = await multi.ReadSingleOrDefaultAsync<ApplicationHeaderRow>();
        if (appHeader is null)
        {
            return Result.Failure<AdmissionApplicationDetailResponse>(
                DomainError.NotFound(
                    "Admission.ApplicationNotFound",
                    $"Admission application with ID '{id}' was not found."));
        }

        var applicant = await multi.ReadSingleOrDefaultAsync<ApplicantDetailBio>();
        if (applicant is null)
        {
            return Result.Failure<AdmissionApplicationDetailResponse>(
                DomainError.NotFound(
                    "Admission.ApplicantNotFound",
                    $"Applicant profile associated with application '{id}' was not found."));
        }

        var contacts = (await multi.ReadAsync<AdmissionContactDetail>()).AsList();
        var documents = (await multi.ReadAsync<ApplicantDocumentDetail>()).AsList();
        var evidence = (await multi.ReadAsync<AssessmentEvidenceDetail>()).AsList();
        var decision = await multi.ReadSingleOrDefaultAsync<AdmissionDecisionDetail>();

        var response = new AdmissionApplicationDetailResponse(
            appHeader.Id,
            appHeader.ApplicationNumber,
            appHeader.ApplicantId,
            appHeader.AdmissionPeriodId,
            appHeader.AcademicYearId,
            appHeader.ApplicationType,
            appHeader.RequestedEntryLevel,
            appHeader.RequestedCampus,
            appHeader.RequestedProgram,
            appHeader.RegistrationDate,
            appHeader.Status,
            appHeader.SubmittedAt,
            appHeader.Notes,
            appHeader.CreatedAt,
            appHeader.CreatedBy,
            appHeader.UpdatedAt,
            appHeader.UpdatedBy,
            applicant,
            contacts,
            documents,
            evidence,
            decision);

        return Result.Success(response);
    }
}
