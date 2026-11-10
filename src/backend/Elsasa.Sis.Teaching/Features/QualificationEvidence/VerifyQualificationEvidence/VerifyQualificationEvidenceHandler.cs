namespace Elsasa.Sis.Teaching.Features.QualificationEvidence.VerifyQualificationEvidence;

using Dapper;
using Elsasa.Sis.Contracts.Teaching.Events;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;
using MediatR;

/// <summary>
/// Feature handler executing qualification evidence verification for Qualification Verifiers (TD-05, P3-S10).
/// Verifies authenticity of credentials, records verifier stamp, clears unverified warning flags
/// on linked authorizations if all supporting credentials become verified, and publishes <see cref="QualificationEvidenceVerifiedEvent"/>.
/// </summary>
public sealed class VerifyQualificationEvidenceHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser,
    IPublisher publisher)
{
    static VerifyQualificationEvidenceHandler()
    {
        DapperTypeHandlers.RegisterTypeHandlers();
    }

    private const string _selectEvidenceSql = """
        SELECT
            id AS Id,
            teacher_profile_id AS TeacherProfileId,
            title AS Title,
            verification_state AS VerificationState
        FROM teaching.qualification_evidence
        WHERE id = @Id;
        """;

    private const string _updateEvidenceSql = """
        UPDATE teaching.qualification_evidence
        SET
            verification_state = 'verified',
            verified_by = @VerifiedBy,
            verified_date = @VerifiedDate,
            verification_notes = @VerificationNotes,
            updated_at = SYSUTCDATETIME(),
            updated_by = @VerifiedBy
        WHERE id = @Id
          AND verification_state IN ('claimed', 'submitted');
        """;

    private const string _clearAuthorizationWarningSql = """
        UPDATE ta
        SET ta.unverified_credential_warning = 0,
            ta.updated_at = SYSUTCDATETIME(),
            ta.updated_by = @VerifiedBy
        FROM teaching.teaching_authorizations ta
        INNER JOIN teaching.authorization_evidence ae ON ae.authorization_id = ta.id
        WHERE ae.qualification_evidence_id = @Id
          AND ta.unverified_credential_warning = 1
          AND NOT EXISTS (
              SELECT 1
              FROM teaching.authorization_evidence ae2
              INNER JOIN teaching.qualification_evidence qe2 ON qe2.id = ae2.qualification_evidence_id
              WHERE ae2.authorization_id = ta.id
                AND qe2.verification_state <> 'verified'
          );
        """;

    /// <summary>
    /// Executes qualification evidence verification.
    /// </summary>
    /// <param name="id">The unique identifier of the qualification evidence.</param>
    /// <param name="request">Optional verification payload containing verification notes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A Result containing the verified qualification evidence response or domain error.</returns>
    public async Task<Result<VerifyQualificationEvidenceResponse>> HandleAsync(
        Guid id,
        VerifyQualificationEvidenceRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return Result.Failure<VerifyQualificationEvidenceResponse>(
                DomainError.Validation("Teaching.EmptyEvidenceId", "Qualification evidence ID cannot be empty."));
        }

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var existing = await connection.QuerySingleOrDefaultAsync<QualificationEvidenceSummaryRow>(new CommandDefinition(
            _selectEvidenceSql,
            new { Id = id },
            cancellationToken: cancellationToken));

        if (existing is null)
        {
            return Result.Failure<VerifyQualificationEvidenceResponse>(DomainError.NotFound(
                "Teaching.QualificationEvidenceNotFound",
                $"Qualification evidence with ID '{id}' was not found."));
        }

        if (!string.Equals(existing.VerificationState, "claimed", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(existing.VerificationState, "submitted", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<VerifyQualificationEvidenceResponse>(DomainError.Conflict(
                "Teaching.InvalidVerificationState",
                $"Cannot verify qualification evidence with ID '{id}' because current verification state is '{existing.VerificationState}'. Only evidence in 'claimed' or 'submitted' state can be verified."));
        }

        var verifiedBy = currentUser.Username ?? "system";
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var normalizedNotes = string.IsNullOrWhiteSpace(request?.VerificationNotes) ? null : request.VerificationNotes.Trim();

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var rowsAffected = await connection.ExecuteAsync(new CommandDefinition(
            _updateEvidenceSql,
            new
            {
                Id = id,
                VerifiedBy = verifiedBy,
                VerifiedDate = today.ToDateTime(TimeOnly.MinValue),
                VerificationNotes = normalizedNotes
            },
            transaction: transaction,
            cancellationToken: cancellationToken));

        if (rowsAffected == 0)
        {
            await transaction.RollbackAsync(cancellationToken);
            return Result.Failure<VerifyQualificationEvidenceResponse>(DomainError.Conflict(
                "Teaching.InvalidVerificationState",
                $"Qualification evidence with ID '{id}' could not be verified because its state was modified concurrently."));
        }

        // If verified evidence was linked to authorizations with unverified_credential_warning = 1,
        // checks if all supporting credentials for those authorizations are now verified; if so, clears unverified_credential_warning = 0.
        await connection.ExecuteAsync(new CommandDefinition(
            _clearAuthorizationWarningSql,
            new
            {
                Id = id,
                VerifiedBy = verifiedBy
            },
            transaction: transaction,
            cancellationToken: cancellationToken));

        await transaction.CommitAsync(cancellationToken);

        // Publish in-memory domain event
        var domainEvent = new QualificationEvidenceVerifiedEvent(
            id,
            existing.TeacherProfileId,
            existing.Title,
            "verified",
            verifiedBy);

        await publisher.Publish(domainEvent, cancellationToken);

        return Result.Success(new VerifyQualificationEvidenceResponse(
            id,
            existing.TeacherProfileId,
            existing.Title,
            "verified",
            verifiedBy,
            today,
            normalizedNotes));
    }

    private sealed record QualificationEvidenceSummaryRow(Guid Id, Guid TeacherProfileId, string Title, string VerificationState);
}
