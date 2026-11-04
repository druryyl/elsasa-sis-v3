namespace Elsasa.Sis.Finance.Features.Policies.GetFinancialPolicies;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler querying financial policies with their associated active charge rules (ADR-004).
/// </summary>
public sealed class GetFinancialPoliciesHandler(ISqlConnectionFactory connectionFactory)
{
    private const string _selectPoliciesWithRulesSql = """
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
            p.created_by AS CreatedBy
        FROM finance.financial_policies p
        WHERE (@Status IS NULL OR p.status = @Status)
          AND (@Category IS NULL OR p.category = @Category)
        ORDER BY p.effective_date DESC, p.created_at DESC;

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
            r.status AS Status
        FROM finance.charge_rules r
        INNER JOIN finance.financial_policies p ON p.id = r.policy_id
        WHERE (@Status IS NULL OR p.status = @Status)
          AND (@Category IS NULL OR p.category = @Category)
          AND r.status = 'active'
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
        string? CreatedBy);

    public async Task<Result<IReadOnlyList<FinancialPolicyListItemDto>>> HandleAsync(
        string? status = null,
        string? category = null,
        CancellationToken cancellationToken = default)
    {
        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        using var multi = await connection.QueryMultipleAsync(new CommandDefinition(
            _selectPoliciesWithRulesSql,
            new
            {
                Status = string.IsNullOrWhiteSpace(status) ? null : status.Trim(),
                Category = string.IsNullOrWhiteSpace(category) ? null : category.Trim()
            },
            cancellationToken: cancellationToken));

        var policyRows = (await multi.ReadAsync<PolicyHeaderRow>()).AsList();
        var ruleRows = (await multi.ReadAsync<PolicyChargeRuleDto>()).AsList();

        var rulesByPolicyId = ruleRows
            .GroupBy(r => r.PolicyId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<PolicyChargeRuleDto>)g.ToList());

        var resultList = new List<FinancialPolicyListItemDto>(policyRows.Count);
        foreach (var p in policyRows)
        {
            var rules = rulesByPolicyId.TryGetValue(p.Id, out var rList)
                ? rList
                : [];

            resultList.Add(new FinancialPolicyListItemDto(
                p.Id,
                p.PolicyCode,
                p.Name,
                p.Category,
                p.BusinessPurpose,
                p.ApplicableContext,
                p.EffectiveDate,
                p.EndDate,
                p.PolicyVersion,
                p.Status,
                p.ApprovingAuthority,
                p.ApprovedDate,
                p.SupersedesPolicyId,
                p.Notes,
                p.CreatedAt,
                p.CreatedBy,
                rules));
        }

        return Result.Success<IReadOnlyList<FinancialPolicyListItemDto>>(resultList);
    }
}
