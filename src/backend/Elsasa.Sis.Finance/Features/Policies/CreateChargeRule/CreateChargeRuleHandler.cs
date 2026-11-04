namespace Elsasa.Sis.Finance.Features.Policies.CreateChargeRule;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler creating a concrete charge rule linked to a governing financial policy (ADR-004, ADR-005).
/// </summary>
public sealed class CreateChargeRuleHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    private const string _selectPolicyStatusSql = """
        SELECT status
        FROM finance.financial_policies
        WHERE id = @PolicyId;
        """;

    private const string _checkDuplicateRuleCodeSql = """
        SELECT 1
        FROM finance.charge_rules
        WHERE rule_code = @RuleCode;
        """;

    private const string _insertChargeRuleSql = """
        INSERT INTO finance.charge_rules (
            id,
            rule_code,
            policy_id,
            name,
            charge_type,
            is_mandatory,
            determination_method,
            base_amount,
            currency_code,
            recurrence_pattern,
            due_rule_type,
            due_rule_value,
            applicable_grade,
            applicable_program,
            status,
            notes,
            created_at,
            created_by
        ) VALUES (
            @Id,
            @RuleCode,
            @PolicyId,
            @Name,
            @ChargeType,
            @IsMandatory,
            @DeterminationMethod,
            @BaseAmount,
            @CurrencyCode,
            @RecurrencePattern,
            @DueRuleType,
            @DueRuleValue,
            @ApplicableGrade,
            @ApplicableProgram,
            'active',
            @Notes,
            @CreatedAt,
            @CreatedBy
        );
        """;

    public async Task<Result<CreateChargeRuleResponse>> HandleAsync(
        Guid policyId,
        CreateChargeRuleRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Verify governing policy exists and is active
        var policyStatus = await connection.ExecuteScalarAsync<string?>(new CommandDefinition(
            _selectPolicyStatusSql,
            new { PolicyId = policyId },
            cancellationToken: cancellationToken));

        if (policyStatus is null)
        {
            return Result.Failure<CreateChargeRuleResponse>(
                DomainError.NotFound(
                    "Finance.Policy.NotFound",
                    $"Financial policy with ID '{policyId}' was not found."));
        }

        if (!string.Equals(policyStatus, "active", StringComparison.OrdinalIgnoreCase))
        {
            return Result.Failure<CreateChargeRuleResponse>(
                DomainError.Conflict(
                    "Finance.Policy.NotActive",
                    "Charge rules can only be attached to active financial policies."));
        }

        // 2. Validate unique rule code
        var duplicateExists = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            _checkDuplicateRuleCodeSql,
            new { RuleCode = request.RuleCode.Trim() },
            cancellationToken: cancellationToken));

        if (duplicateExists)
        {
            return Result.Failure<CreateChargeRuleResponse>(
                DomainError.Conflict(
                    "Finance.ChargeRule.DuplicateCode",
                    $"Charge rule with code '{request.RuleCode}' already exists."));
        }

        // 3. Persist charge rule
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var auditUser = currentUser.Username ?? "SYSTEM";
        var determinationMethod = string.IsNullOrWhiteSpace(request.DeterminationMethod) ? "fixed_amount" : request.DeterminationMethod.Trim();
        var currencyCode = string.IsNullOrWhiteSpace(request.CurrencyCode) ? "IDR" : request.CurrencyCode.Trim().ToUpperInvariant();

        await connection.ExecuteAsync(new CommandDefinition(
            _insertChargeRuleSql,
            new
            {
                Id = id,
                RuleCode = request.RuleCode.Trim(),
                PolicyId = policyId,
                Name = request.Name.Trim(),
                ChargeType = request.ChargeType.Trim(),
                IsMandatory = request.IsMandatory,
                DeterminationMethod = determinationMethod,
                BaseAmount = request.BaseAmount,
                CurrencyCode = currencyCode,
                RecurrencePattern = request.RecurrencePattern.Trim(),
                DueRuleType = request.DueRuleType.Trim(),
                DueRuleValue = request.DueRuleValue.Trim(),
                ApplicableGrade = string.IsNullOrWhiteSpace(request.ApplicableGrade) ? null : request.ApplicableGrade.Trim(),
                ApplicableProgram = string.IsNullOrWhiteSpace(request.ApplicableProgram) ? null : request.ApplicableProgram.Trim(),
                Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
                CreatedAt = now,
                CreatedBy = auditUser
            },
            cancellationToken: cancellationToken));

        var response = new CreateChargeRuleResponse(
            id,
            request.RuleCode.Trim(),
            policyId,
            request.Name.Trim(),
            request.ChargeType.Trim(),
            request.IsMandatory,
            determinationMethod,
            request.BaseAmount,
            currencyCode,
            request.RecurrencePattern.Trim(),
            request.DueRuleType.Trim(),
            request.DueRuleValue.Trim(),
            string.IsNullOrWhiteSpace(request.ApplicableGrade) ? null : request.ApplicableGrade.Trim(),
            string.IsNullOrWhiteSpace(request.ApplicableProgram) ? null : request.ApplicableProgram.Trim(),
            "active",
            string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            now,
            auditUser);

        return Result.Success(response);
    }
}
