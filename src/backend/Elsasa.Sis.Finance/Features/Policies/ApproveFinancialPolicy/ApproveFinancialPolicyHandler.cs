namespace Elsasa.Sis.Finance.Features.Policies.ApproveFinancialPolicy;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler executing policy status transition from draft to active with audit footprint (ADR-004, ADR-005).
/// </summary>
public sealed class ApproveFinancialPolicyHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    private const string _selectPolicyStatusSql = """
        SELECT
            id AS Id,
            policy_code AS PolicyCode,
            name AS Name,
            status AS Status
        FROM finance.financial_policies
        WHERE id = @Id;
        """;

    private const string _updatePolicyStatusSql = """
        UPDATE finance.financial_policies
        SET status = 'active',
            approving_authority = @ApprovingAuthority,
            approved_date = @ApprovedDate,
            notes = CASE WHEN @Notes IS NOT NULL THEN @Notes ELSE notes END,
            updated_at = @UpdatedAt,
            updated_by = @UpdatedBy
        WHERE id = @Id;
        """;

    private sealed record PolicyStatusRow(Guid Id, string PolicyCode, string Name, string Status);

    public async Task<Result<ApproveFinancialPolicyResponse>> HandleAsync(
        Guid id,
        ApproveFinancialPolicyRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        var policy = await connection.QuerySingleOrDefaultAsync<PolicyStatusRow>(new CommandDefinition(
            _selectPolicyStatusSql,
            new { Id = id },
            cancellationToken: cancellationToken));

        if (policy is null)
        {
            return Result.Failure<ApproveFinancialPolicyResponse>(
                DomainError.NotFound(
                    "Finance.Policy.NotFound",
                    $"Financial policy with ID '{id}' was not found."));
        }

        if (!string.Equals(policy.Status, "draft", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<ApproveFinancialPolicyResponse>(
                DomainError.Conflict(
                    "Finance.Policy.InvalidStatus",
                    $"Financial policy '{id}' cannot be approved because its current status is '{policy.Status}'. Only policies in 'draft' status may be approved."));
        }

        var authority = !string.IsNullOrWhiteSpace(request?.ApprovingAuthority)
            ? request.ApprovingAuthority.Trim()
            : (!string.IsNullOrWhiteSpace(currentUser.Username) ? currentUser.Username : "Finance Authority");

        var approvedDate = DateOnly.FromDateTime(DateTime.UtcNow);
        var now = DateTimeOffset.UtcNow;
        var auditUser = currentUser.Username ?? "SYSTEM";

        await connection.ExecuteAsync(new CommandDefinition(
            _updatePolicyStatusSql,
            new
            {
                Id = id,
                ApprovingAuthority = authority,
                ApprovedDate = approvedDate,
                Notes = string.IsNullOrWhiteSpace(request?.Notes) ? null : request.Notes.Trim(),
                UpdatedAt = now,
                UpdatedBy = auditUser
            },
            cancellationToken: cancellationToken));

        var response = new ApproveFinancialPolicyResponse(
            policy.Id,
            policy.PolicyCode,
            policy.Name,
            "active",
            authority,
            approvedDate,
            now,
            auditUser);

        return Result.Success(response);
    }
}
