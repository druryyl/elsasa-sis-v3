namespace Elsasa.Sis.Finance.Features.Policies.CreateFinancialPolicy;

using Dapper;
using Elsasa.Sis.Shared.Common;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Feature handler executing financial policy draft persistence (ADR-004, ADR-005).
/// </summary>
public sealed class CreateFinancialPolicyHandler(
    ISqlConnectionFactory connectionFactory,
    ICurrentUser currentUser)
{
    private const string _checkDuplicateCodeSql = """
        SELECT 1
        FROM finance.financial_policies
        WHERE policy_code = @PolicyCode;
        """;

    private const string _checkActiveCategorySql = """
        SELECT code
        FROM finance.policy_categories
        WHERE code = @Category
          AND is_active = 1;
        """;

    private const string _checkSupersedesExistsSql = """
        SELECT 1
        FROM finance.financial_policies
        WHERE id = @SupersedesPolicyId;
        """;

    private const string _insertPolicySql = """
        INSERT INTO finance.financial_policies (
            id,
            policy_code,
            name,
            category,
            business_purpose,
            applicable_context,
            effective_date,
            end_date,
            policy_version,
            status,
            approving_authority,
            approved_date,
            supersedes_policy_id,
            notes,
            created_at,
            created_by
        ) VALUES (
            @Id,
            @PolicyCode,
            @Name,
            @Category,
            @BusinessPurpose,
            @ApplicableContext,
            @EffectiveDate,
            @EndDate,
            @PolicyVersion,
            'draft',
            NULL,
            NULL,
            @SupersedesPolicyId,
            @Notes,
            @CreatedAt,
            @CreatedBy
        );
        """;

    public async Task<Result<CreateFinancialPolicyResponse>> HandleAsync(
        CreateFinancialPolicyRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);

        // 1. Validate unique policy code
        var duplicateExists = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            _checkDuplicateCodeSql,
            new { PolicyCode = request.PolicyCode.Trim() },
            cancellationToken: cancellationToken));

        if (duplicateExists)
        {
            return Result.Failure<CreateFinancialPolicyResponse>(
                DomainError.Conflict(
                    "Finance.Policy.DuplicateCode",
                    $"Financial policy with code '{request.PolicyCode}' already exists."));
        }

        // 2. Validate active master policy category and resolve canonical code
        var canonicalCategory = await connection.ExecuteScalarAsync<string?>(new CommandDefinition(
            _checkActiveCategorySql,
            new { Category = request.Category.Trim() },
            cancellationToken: cancellationToken));

        if (string.IsNullOrWhiteSpace(canonicalCategory))
        {
            return Result.Failure<CreateFinancialPolicyResponse>(
                DomainError.Validation(
                    "Finance.Policy.InvalidCategory",
                    $"Policy category '{request.Category}' is invalid or inactive."));
        }

        // 3. Validate superseded policy if provided
        if (request.SupersedesPolicyId.HasValue)
        {
            var supersedesExists = await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
                _checkSupersedesExistsSql,
                new { SupersedesPolicyId = request.SupersedesPolicyId.Value },
                cancellationToken: cancellationToken));

            if (!supersedesExists)
            {
                return Result.Failure<CreateFinancialPolicyResponse>(
                    DomainError.NotFound(
                        "Finance.Policy.SupersededNotFound",
                        $"Superseded financial policy with ID '{request.SupersedesPolicyId.Value}' was not found."));
            }
        }

        // 4. Persist financial policy draft
        var id = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var auditUser = currentUser.Username ?? "SYSTEM";
        var policyVersion = string.IsNullOrWhiteSpace(request.PolicyVersion) ? "1.0" : request.PolicyVersion.Trim();

        await connection.ExecuteAsync(new CommandDefinition(
            _insertPolicySql,
            new
            {
                Id = id,
                PolicyCode = request.PolicyCode.Trim(),
                Name = request.Name.Trim(),
                Category = canonicalCategory,
                BusinessPurpose = request.BusinessPurpose.Trim(),
                ApplicableContext = string.IsNullOrWhiteSpace(request.ApplicableContext) ? null : request.ApplicableContext.Trim(),
                EffectiveDate = request.EffectiveDate,
                EndDate = request.EndDate,
                PolicyVersion = policyVersion,
                SupersedesPolicyId = request.SupersedesPolicyId,
                Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
                CreatedAt = now,
                CreatedBy = auditUser
            },
            cancellationToken: cancellationToken));

        var response = new CreateFinancialPolicyResponse(
            id,
            request.PolicyCode.Trim(),
            request.Name.Trim(),
            canonicalCategory,
            request.BusinessPurpose.Trim(),
            string.IsNullOrWhiteSpace(request.ApplicableContext) ? null : request.ApplicableContext.Trim(),
            request.EffectiveDate,
            request.EndDate,
            policyVersion,
            "draft",
            request.SupersedesPolicyId,
            string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            now,
            auditUser);

        return Result.Success(response);
    }
}
