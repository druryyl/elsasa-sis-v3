namespace Elsasa.Sis.Students.Features.Records.GetStudentById;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler querying the complete 360-degree student dossier by student ID (ADR-004, ADR-005, P4-S06).
/// </summary>
public sealed class GetStudentByIdHandler(ISqlConnectionFactory connectionFactory)
{
    private const string _selectDossierMultiSql = """
        SELECT
            s.id AS Id,
            s.student_number AS StudentNumber,
            s.applicant_id AS ApplicantId,
            s.full_name AS FullName,
            s.preferred_name AS PreferredName,
            s.date_of_birth AS DateOfBirth,
            s.place_of_birth AS PlaceOfBirth,
            s.gender AS Gender,
            s.nationality AS Nationality,
            s.government_id_type AS GovernmentIdType,
            s.government_id_number AS GovernmentIdNumber,
            s.address AS Address,
            s.phone AS Phone,
            s.email AS Email,
            s.is_independent AS IsIndependent,
            s.current_status AS CurrentStatus,
            s.enrollment_date AS EnrollmentDate,
            s.notes AS Notes,
            s.created_at AS CreatedAt,
            s.created_by AS CreatedBy,
            s.updated_at AS UpdatedAt,
            s.updated_by AS UpdatedBy
        FROM students.students s
        WHERE s.id = @Id;

        SELECT
            e.enrollment_date AS EnrollmentDate,
            e.initial_campus AS Campus,
            e.initial_level AS Level,
            e.initial_academic_year AS AcademicYear,
            e.initial_program AS Program,
            e.initial_cohort AS Cohort,
            e.approved_by AS ApprovedBy,
            e.enrollment_payment_confirmed AS PaymentConfirmed,
            e.payment_reference AS PaymentReference,
            e.payment_waived AS PaymentWaived,
            e.waiver_reference AS WaiverReference
        FROM students.enrollments e
        WHERE e.student_id = @Id
        ORDER BY e.enrollment_date DESC, e.created_at DESC;

        SELECT
            rel.id AS RelationshipId,
            g.id AS GuardianId,
            g.full_name AS FullName,
            rel.relationship_type AS RelationshipType,
            g.government_id_type AS GovernmentIdType,
            g.government_id_number AS GovernmentIdNumber,
            g.phone AS Phone,
            g.email AS Email,
            g.address AS Address,
            g.occupation AS Occupation,
            rel.is_primary_contact AS IsPrimaryContact,
            rel.is_emergency_contact AS IsEmergencyContact,
            rel.is_financial_contact AS IsFinancialContact,
            rel.is_authorized_pickup AS IsAuthorizedPickup,
            rel.is_active AS IsActive
        FROM students.student_guardian_relationships rel
        INNER JOIN students.guardians g ON g.id = rel.guardian_id
        WHERE rel.student_id = @Id
        ORDER BY rel.is_active DESC, rel.is_primary_contact DESC, rel.created_at ASC;

        SELECT
            doc.id AS Id,
            doc.document_type AS DocumentType,
            doc.submission_status AS SubmissionStatus,
            doc.submitted_date AS SubmittedDate,
            doc.verified_date AS VerifiedDate,
            doc.verified_by AS VerifiedBy,
            doc.file_name AS FileName,
            doc.file_size_bytes AS FileSizeBytes,
            doc.content_type AS ContentType,
            doc.file_reference AS FileReference
        FROM students.student_documents doc
        WHERE doc.student_id = @Id
        ORDER BY doc.created_at ASC;
        """;

    private sealed record StudentHeaderRow(
        Guid Id,
        string StudentNumber,
        Guid ApplicantId,
        string FullName,
        string? PreferredName,
        DateOnly? DateOfBirth,
        string? PlaceOfBirth,
        string? Gender,
        string? Nationality,
        string? GovernmentIdType,
        string? GovernmentIdNumber,
        string? Address,
        string? Phone,
        string? Email,
        bool IsIndependent,
        string CurrentStatus,
        DateOnly EnrollmentDate,
        string? Notes,
        DateTimeOffset CreatedAt,
        string? CreatedBy,
        DateTimeOffset? UpdatedAt,
        string? UpdatedBy);

    /// <summary>
    /// Retrieves the entire 360-degree student dossier aggregate by student ID.
    /// </summary>
    public async Task<Result<StudentDetailResponse>> HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        using var multi = await connection.QueryMultipleAsync(new CommandDefinition(
            _selectDossierMultiSql,
            new { Id = id },
            cancellationToken: cancellationToken));

        var student = await multi.ReadSingleOrDefaultAsync<StudentHeaderRow>();
        if (student is null)
        {
            return Result.Failure<StudentDetailResponse>(
                DomainError.NotFound(
                    "Students.Records.StudentNotFound",
                    $"Student with ID '{id}' was not found."));
        }

        var enrollments = (await multi.ReadAsync<EnrollmentHistoryDto>()).AsList();
        var guardians = (await multi.ReadAsync<StudentGuardianRelationshipDto>()).AsList();
        var documents = (await multi.ReadAsync<StudentDocumentDto>()).AsList();

        var response = new StudentDetailResponse(
            student.Id,
            student.StudentNumber,
            student.ApplicantId,
            student.FullName,
            student.PreferredName,
            student.DateOfBirth,
            student.PlaceOfBirth,
            student.Gender,
            student.Nationality,
            student.GovernmentIdType,
            student.GovernmentIdNumber,
            student.Address,
            student.Phone,
            student.Email,
            student.IsIndependent,
            student.CurrentStatus,
            student.EnrollmentDate,
            student.Notes,
            student.CreatedAt,
            student.CreatedBy,
            student.UpdatedAt,
            student.UpdatedBy,
            enrollments,
            guardians,
            documents);

        return Result.Success(response);
    }
}
