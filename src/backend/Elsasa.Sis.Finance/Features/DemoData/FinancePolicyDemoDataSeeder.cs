namespace Elsasa.Sis.Finance.Features.DemoData;

using Dapper;
using Elsasa.Sis.Contracts.DemoData;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Autonomous seeder for the Finance domain institutional fee policies and charge rules (ADR-002, ADR-004).
/// Seeds the Hagios Institutional Fee Policy 2026/2027 with core charge rules
/// for Tuition (SPP), Facility Fees, PACE booklet material fees, and the
/// Pastoral Scholarship Waiver Rule.
/// </summary>
public sealed class FinancePolicyDemoDataSeeder(
    ISqlConnectionFactory connectionFactory) : IFinancePolicyDemoDataSeeder
{
    private const string _createdBy = "demo-data-seeder";

    private const string _insertPolicySql = """
        INSERT INTO finance.financial_policies (
            id, policy_code, name, category, business_purpose, applicable_context,
            effective_date, end_date, policy_version, status, approving_authority,
            approved_date, supersedes_policy_id, notes, created_at, created_by
        ) VALUES (
            @Id, @PolicyCode, @Name, @Category, @BusinessPurpose, @ApplicableContext,
            @EffectiveDate, @EndDate, @PolicyVersion, @Status, @ApprovingAuthority,
            @ApprovedDate, @SupersedesPolicyId, @Notes, @CreatedAt, @CreatedBy
        );
        """;

    private const string _insertChargeRuleSql = """
        INSERT INTO finance.charge_rules (
            id, rule_code, policy_id, name, charge_type, is_mandatory,
            determination_method, base_amount, currency_code, recurrence_pattern,
            due_rule_type, due_rule_value, applicable_grade, applicable_program,
            status, notes, created_at, created_by
        ) VALUES (
            @Id, @RuleCode, @PolicyId, @Name, @ChargeType, @IsMandatory,
            @DeterminationMethod, @BaseAmount, @CurrencyCode, @RecurrencePattern,
            @DueRuleType, @DueRuleValue, @ApplicableGrade, @ApplicableProgram,
            @Status, @Notes, @CreatedAt, @CreatedBy
        );
        """;

    public async Task<FinancePolicySeedResult> SeedFinancePoliciesAsync(
        OperationsSeedResult operationsContext,
        CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        // Generate deterministic but unique policy ID
        var policyId = Guid.NewGuid();

        // Charge rule IDs
        var tuitionRuleId = Guid.NewGuid();
        var facilityRuleId = Guid.NewGuid();
        var paceMaterialRuleId = Guid.NewGuid();

        // Academic period label derived from context
        var academicPeriodLabel = $"TP {operationsContext.AcademicYearCode.Replace("AY-", "")} - S1";
        var academicPeriodId = operationsContext.ActivePeriodId;

        // Effective date range: TP 2026/2027 spans Jul 2026 - Jun 2027
        var effectiveDate = new DateOnly(2026, 7, 1);
        var endDate = new DateOnly(2027, 6, 30);

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            // 1. Insert the institutional fee policy
            await connection.ExecuteAsync(new CommandDefinition(
                _insertPolicySql,
                new
                {
                    Id = policyId,
                    PolicyCode = "FP-2026-002",
                    Name = "Hagios Institutional Fee Policy 2026/2027",
                    Category = "general_fee",
                    BusinessPurpose = "Menerapkan struktur biaya pendidikan institusional mencakup SPP bulanan, uang gedung tahunan, dan biaya materi PACE untuk Tahun Pelajaran 2026/2027.",
                    ApplicableContext = "Tahun Pelajaran 2026/2027",
                    EffectiveDate = effectiveDate,
                    EndDate = endDate,
                    PolicyVersion = "1.0",
                    Status = "active",
                    ApprovingAuthority = "Direktur Keuangan",
                    ApprovedDate = effectiveDate,
                    SupersedesPolicyId = (Guid?)null,
                    Notes = "Institutional fee policy for TP 2026/2027 - covers tuition, facility, and PACE material fees",
                    CreatedAt = now,
                    CreatedBy = _createdBy
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

            // 2. Insert Monthly Tuition (SPP) charge rule
            await connection.ExecuteAsync(new CommandDefinition(
                _insertChargeRuleSql,
                new
                {
                    Id = tuitionRuleId,
                    RuleCode = "SPP-REG",
                    PolicyId = policyId,
                    Name = "SPP Bulanan Reguler",
                    ChargeType = "tuition",
                    IsMandatory = true,
                    DeterminationMethod = "fixed_amount",
                    BaseAmount = 1500000L,
                    CurrencyCode = "IDR",
                    RecurrencePattern = "monthly",
                    DueRuleType = "fixed_day_of_month",
                    DueRuleValue = "10",
                    ApplicableGrade = (string?)null,
                    ApplicableProgram = "Reguler",
                    Status = "active",
                    Notes = "Monthly tuition fee (SPP) IDR 1,500,000 per month for all regular students",
                    CreatedAt = now,
                    CreatedBy = _createdBy
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

            // 3. Insert Annual Facility Fee (Uang Gedung) charge rule
            await connection.ExecuteAsync(new CommandDefinition(
                _insertChargeRuleSql,
                new
                {
                    Id = facilityRuleId,
                    RuleCode = "FACILITY-ANNUAL",
                    PolicyId = policyId,
                    Name = "Uang Gedung Tahunan",
                    ChargeType = "enrollment_fee",
                    IsMandatory = true,
                    DeterminationMethod = "fixed_amount",
                    BaseAmount = 10000000L,
                    CurrencyCode = "IDR",
                    RecurrencePattern = "annual",
                    DueRuleType = "days_after_issuance",
                    DueRuleValue = "14",
                    ApplicableGrade = (string?)null,
                    ApplicableProgram = "All",
                    Status = "active",
                    Notes = "Annual facility fee (Uang Gedung) IDR 10,000,000 per year",
                    CreatedAt = now,
                    CreatedBy = _createdBy
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

            // 4. Insert PACE Material Fee charge rule
            await connection.ExecuteAsync(new CommandDefinition(
                _insertChargeRuleSql,
                new
                {
                    Id = paceMaterialRuleId,
                    RuleCode = "PACE-UNIT-FEE",
                    PolicyId = policyId,
                    Name = "Biaya Materi PACE Per Unit",
                    ChargeType = "activity_fee",
                    IsMandatory = false,
                    DeterminationMethod = "fixed_amount",
                    BaseAmount = 75000L,
                    CurrencyCode = "IDR",
                    RecurrencePattern = "per_unit",
                    DueRuleType = "fixed_day_of_month",
                    DueRuleValue = "1",
                    ApplicableGrade = (string?)null,
                    ApplicableProgram = "PACE",
                    Status = "active",
                    Notes = "PACE booklet material fee IDR 75,000 per unit booklet",
                    CreatedAt = now,
                    CreatedBy = _createdBy
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

            // 5. Insert Pastoral Scholarship Waiver Rule - 100% discount on tuition for pastoral children
            await connection.ExecuteAsync(new CommandDefinition(
                _insertChargeRuleSql,
                new
                {
                    Id = Guid.NewGuid(),
                    RuleCode = "PASTORAL-WAIVER",
                    PolicyId = policyId,
                    Name = "Beasiswa Pastoral - Pembebasan Total SPP",
                    ChargeType = "tuition",
                    IsMandatory = false,
                    DeterminationMethod = "fixed_amount",
                    BaseAmount = -1500000L,
                    CurrencyCode = "IDR",
                    RecurrencePattern = "monthly",
                    DueRuleType = "fixed_day_of_month",
                    DueRuleValue = "10",
                    ApplicableGrade = (string?)null,
                    ApplicableProgram = "Pastoral",
                    Status = "active",
                    Notes = "100% tuition discount (waiver) for pastoral children",
                    CreatedAt = now,
                    CreatedBy = _createdBy
                },
                transaction: transaction,
                cancellationToken: cancellationToken));

            await transaction.CommitAsync(cancellationToken);

            return new FinancePolicySeedResult(
                PolicyId: policyId,
                TuitionChargeRuleId: tuitionRuleId,
                FacilityChargeRuleId: facilityRuleId,
                PaceMaterialChargeRuleId: paceMaterialRuleId);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
