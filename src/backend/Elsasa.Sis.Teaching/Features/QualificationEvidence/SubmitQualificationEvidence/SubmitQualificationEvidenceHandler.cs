namespace Elsasa.Sis.Teaching.Features.QualificationEvidence.SubmitQualificationEvidence;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler for submitting qualification documentary evidence for a teacher profile (TD-05, P3-S09).
/// </summary>
public sealed class SubmitQualificationEvidenceHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    static SubmitQualificationEvidenceHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _checkTeacherSql = """
        SELECT TOP 1 1
        FROM teaching.teacher_profiles
        WHERE id = @TeacherId;
        """;

    private const string _checkQualificationTypeSql = """
        SELECT TOP 1 1
        FROM teaching.qualification_types
        WHERE id = @QualificationTypeId AND is_active = 1;
        """;

    private const string _insertEvidenceSql = """
        INSERT INTO teaching.qualification_evidence (
            id,
            teacher_profile_id,
            qualification_type_id,
            title,
            issuing_authority,
            credential_identifier,
            issue_date,
            expiry_date,
            document_reference,
            verification_state,
            verified_by,
            verified_date,
            verification_notes,
            rejection_reason,
            restrictions_or_conditions,
            created_at,
            created_by
        )
        VALUES (
            @Id,
            @TeacherProfileId,
            @QualificationTypeId,
            @Title,
            @IssuingAuthority,
            @CredentialIdentifier,
            @IssueDate,
            @ExpiryDate,
            @DocumentReference,
            'submitted',
            NULL,
            NULL,
            NULL,
            NULL,
            @RestrictionsOrConditions,
            SYSUTCDATETIME(),
            @CreatedBy
        );
        """;

    /// <summary>
    /// Executes qualification evidence submission for a teacher profile.
    /// </summary>
    /// <param name="teacherId">The identifier of the teacher profile.</param>
    /// <param name="request">The submission request payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A Result containing the created qualification evidence response or domain error.</returns>
    public async Task<Result<SubmitQualificationEvidenceResponse>> HandleAsync(
        Guid teacherId,
        SubmitQualificationEvidenceRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (teacherId == Guid.Empty)
        {
            return Result.Failure<SubmitQualificationEvidenceResponse>(
                DomainError.Validation("Teaching.EmptyTeacherId", "Teacher profile ID cannot be empty."));
        }

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Verify teacher profile exists
        var teacherExists = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            _checkTeacherSql,
            new { TeacherId = teacherId },
            cancellationToken: cancellationToken));

        if (!teacherExists)
        {
            return Result.Failure<SubmitQualificationEvidenceResponse>(DomainError.NotFound(
                "Teaching.TeacherProfileNotFound",
                $"Teacher profile with ID '{teacherId}' was not found."));
        }

        // 2. Verify qualification type exists and is active
        var typeExists = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            _checkQualificationTypeSql,
            new { QualificationTypeId = request.QualificationTypeId },
            cancellationToken: cancellationToken));

        if (!typeExists)
        {
            return Result.Failure<SubmitQualificationEvidenceResponse>(DomainError.NotFound(
                "Teaching.QualificationTypeNotFound",
                $"The qualification type with ID '{request.QualificationTypeId}' does not exist or is inactive."));
        }

        var evidenceId = Guid.NewGuid();
        var normalizedTitle = request.Title.Trim();
        var normalizedAuthority = request.IssuingAuthority.Trim();
        var normalizedCredentialId = string.IsNullOrWhiteSpace(request.CredentialIdentifier) ? null : request.CredentialIdentifier.Trim();
        var normalizedDocRef = string.IsNullOrWhiteSpace(request.DocumentReference) ? null : request.DocumentReference.Trim();
        var normalizedRestrictions = string.IsNullOrWhiteSpace(request.RestrictionsOrConditions) ? null : request.RestrictionsOrConditions.Trim();
        var auditUser = currentUser.Username ?? "system";

        await connection.ExecuteAsync(new CommandDefinition(
            _insertEvidenceSql,
            new
            {
                Id = evidenceId,
                TeacherProfileId = teacherId,
                QualificationTypeId = request.QualificationTypeId,
                Title = normalizedTitle,
                IssuingAuthority = normalizedAuthority,
                CredentialIdentifier = normalizedCredentialId,
                IssueDate = request.IssueDate.ToDateTime(TimeOnly.MinValue),
                ExpiryDate = request.ExpiryDate?.ToDateTime(TimeOnly.MinValue),
                DocumentReference = normalizedDocRef,
                RestrictionsOrConditions = normalizedRestrictions,
                CreatedBy = auditUser
            },
            cancellationToken: cancellationToken));

        return Result.Success(new SubmitQualificationEvidenceResponse(
            evidenceId,
            teacherId,
            request.QualificationTypeId,
            normalizedTitle,
            "submitted"));
    }
}
