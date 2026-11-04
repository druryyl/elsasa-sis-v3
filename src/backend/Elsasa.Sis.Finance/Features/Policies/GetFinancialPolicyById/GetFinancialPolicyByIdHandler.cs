namespace Elsasa.Sis.Finance.Features.Policies.GetFinancialPolicyById;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler querying complete financial policy detail along with all attached charge rules (ADR-004).
/// </summary>
public sealed class GetFinancialPolicyByIdHandler(ISqlConnectionFactory connectionFactory)
{
    private const string _selectPolicyDetailMultiSql = """
        SELECT
            p.id AS Id,
            p.policy_code AS PolicyCode,
            p.name AS Name,
            p.category AS Category,
            p.business_purpose AS BusinessPurpose,
            p.applicable_context AS ApplicableContext,
            p.effective_date AS EffectiveDate,
            p.end_date AS EndDate,
            p.policy_version AS PolicyVersion,
            p.status AS Status,
            p.approving_authority AS ApprovingAuthority,
            p.approved_date AS ApprovedDate,
            p.supersedes_policy_id AS SupersedesPolicyId,
            p.notes AS Notes,
            p.created_at AS CreatedAt,
            p.created_by AS CreatedBy,
            p.updated_at AS UpdatedAt,
            p.updated_by AS UpdatedBy
        FROM finance.financial_policies p
        WHERE p.id = @Id;

        SELECT
            r.id AS Id,
            r.rule_code AS RuleCode,
            r.policy_id AS PolicyId,
            r.name AS Name,
            r.charge_type AS ChargeType,
            r.is_mandatory AS IsMandatory,
            r.determination_method AS DeterminationMethod,
            r.base_amount AS BaseAmount,
            r.currency_code AS CurrencyCode,
            r.recurrence_pattern AS RecurrencePattern,
            r.due_rule_type AS DueRuleType,
            r.due_rule_value AS DueRuleValue,
            r.applicable_grade AS ApplicableGrade,
            r.applicable_program AS ApplicableProgram,
            r.status AS Status,
            r.notes AS Notes,
            r.created_at AS CreatedAt,
            r.created_by AS CreatedBy,
            r.updated_at AS UpdatedAt,
            r.updated_by AS UpdatedBy
        FROM finance.charge_rules r
        WHERE r.policy_id = @Id
        ORDER BY r.created_at ASC;
        """;

    private sealed record PolicyHeaderRow(
        Guid Id,
        string PolicyCode,
        string Name,
        string Category,
        string BusinessPurpose,
        string? ApplicableContext,
        DateOnly EffectiveDate,
        DateOnly? EndDate,
        string PolicyVersion,
        string Status,
        string? ApprovingAuthority,
        DateOnly? ApprovedDate,
        Guid? SupersedesPolicyId,
        string? Notes,
        DateTimeOffset CreatedAt,
        string? CreatedBy,
        DateTimeOffset? UpdatedAt,
        string? UpdatedBy);

    public async Task<Result<FinancialPolicyDetailResponse>> HandleAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        using var multi = await connection.QueryMultipleAsync(new CommandDefinition(
            _selectPolicyDetailMultiSql,
            new { Id = id },
            cancellationToken: cancellationToken));

        var header = await multi.ReadSingleOrDefaultAsync<PolicyHeaderRow>();
        if (header is null)
        {
            return Result.Failure<FinancialPolicyDetailResponse>(
                DomainError.NotFound(
                    "Finance.Policy.NotFound",
                    $"Financial policy with ID '{id}' was not found."));
        }

        var rules = (await multi.ReadAsync<ChargeRuleDetailDto>()).AsList();

        var response = new FinancialPolicyDetailResponse(
            header.Id,
            header.PolicyCode,
            header.Name,
            header.Category,
            header.BusinessPurpose,
            header.ApplicableContext,
            header.EffectiveDate,
            header.EndDate,
            header.PolicyVersion,
            header.Status,
            header.ApprovingAuthority,
            header.ApprovedDate,
            header.SupersedesPolicyId,
            header.Notes,
            header.CreatedAt,
            header.CreatedBy,
            header.UpdatedAt,
            header.UpdatedBy,
            rules);

        return Result.Success(response);
    }
}
