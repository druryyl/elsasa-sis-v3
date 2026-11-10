namespace Elsasa.Sis.Teaching.Features.QualificationEvidence.RejectQualificationEvidence;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler executing qualification evidence rejection for Qualification Verifiers (TD-05, P3-S10).
/// Records documented rejection rationale and updates verification state to 'rejected'.
/// </summary>
public sealed class RejectQualificationEvidenceHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    static RejectQualificationEvidenceHandler()
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

    private const string _rejectEvidenceSql = """
        UPDATE teaching.qualification_evidence
        SET
            verification_state = 'rejected',
            verified_by = @VerifiedBy,
            verified_date = @VerifiedDate,
            rejection_reason = @RejectionReason,
            updated_at = SYSUTCDATETIME(),
            updated_by = @VerifiedBy
        WHERE id = @Id
          AND verification_state IN ('claimed', 'submitted');
        """;

    /// <summary>
    /// Executes qualification evidence rejection with mandatory rationale.
    /// </summary>
    /// <param name="id">The unique identifier of the qualification evidence.</param>
    /// <param name="request">The rejection request payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A Result containing the rejected qualification evidence response or domain error.</returns>
    public async Task<Result<RejectQualificationEvidenceResponse>> HandleAsync(
        Guid id,
        RejectQualificationEvidenceRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (id == Guid.Empty)
        {
            return Result.Failure<RejectQualificationEvidenceResponse>(
                DomainError.Validation("Teaching.EmptyEvidenceId", "Qualification evidence ID cannot be empty."));
        }

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var existing = await connection.QuerySingleOrDefaultAsync<QualificationEvidenceSummaryRow>(new CommandDefinition(
            _selectEvidenceSql,
            new { Id = id },
            cancellationToken: cancellationToken));

        if (existing is null)
        {
            return Result.Failure<RejectQualificationEvidenceResponse>(DomainError.NotFound(
                "Teaching.QualificationEvidenceNotFound",
                $"Qualification evidence with ID '{id}' was not found."));
        }

        if (!string.Equals(existing.VerificationState, "claimed", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(existing.VerificationState, "submitted", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<RejectQualificationEvidenceResponse>(DomainError.Conflict(
                "Teaching.InvalidVerificationState",
                $"Cannot reject qualification evidence with ID '{id}' because current verification state is '{existing.VerificationState}'. Only evidence in 'claimed' or 'submitted' state can be rejected."));
        }

        var verifiedBy = currentUser.Username ?? "system";
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var normalizedReason = request.RejectionReason.Trim();

        var rowsAffected = await connection.ExecuteAsync(new CommandDefinition(
            _rejectEvidenceSql,
            new
            {
                Id = id,
                VerifiedBy = verifiedBy,
                VerifiedDate = today.ToDateTime(TimeOnly.MinValue),
                RejectionReason = normalizedReason
            },
            cancellationToken: cancellationToken));

        if (rowsAffected == 0)
        {
            return Result.Failure<RejectQualificationEvidenceResponse>(DomainError.Conflict(
                "Teaching.InvalidVerificationState",
                $"Qualification evidence with ID '{id}' could not be rejected because its state was modified concurrently."));
        }

        return Result.Success(new RejectQualificationEvidenceResponse(
            id,
            existing.TeacherProfileId,
            existing.Title,
            "rejected",
            verifiedBy,
            today,
            normalizedReason));
    }

    private sealed record QualificationEvidenceSummaryRow(Guid Id, Guid TeacherProfileId, string Title, string VerificationState);
}
