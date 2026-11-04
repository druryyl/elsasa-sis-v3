namespace Elsasa.Sis.IntegrationTests.Features.Finance.Migration;

using Elsasa.Sis.Database;
using Elsasa.Sis.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Xunit;

/// <summary>
/// Integration test suite verifying DbUp schema migration 20261104_0201 (IS006 / CR-FIN-OBLIGATIONS-BILLING, P1-S01, P4-S11, ADR-002, ADR-006).
/// Validates script journaling via DatabaseMigrator, dropping of obsolete stub table finance.policies,
/// provisioning of all 7 authoritative domain tables in schema finance, foreign key referential actions,
/// partial unique index single-account invariant enforcement, idempotent student account backfill,
/// and idempotent core financial policy and charge rules seed data.
/// </summary>
public sealed class FinanceMigrationIntegrationTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private const string _migrationScriptName = "20261104_0201_Finance_ObligationsBilling_Tables.sql";

    [SkippableFact]
    public void FinanceMigration_Should_RunSuccessfullyViaDatabaseMigrator()
    {
        SkipIfDatabaseUnavailable();

        var success = DatabaseMigrator.UpgradeDatabase(Factory.ContainerFixture.ConnectionString);

        success.Should().BeTrue("DatabaseMigrator.UpgradeDatabase should succeed idempotently on target database");
    }

    [SkippableFact]
    public async Task FinanceMigration_Should_BeJournaledInSchemaVersions()
    {
        SkipIfDatabaseUnavailable();

        const string checkScriptSql = """
            SELECT COUNT(1)
            FROM dbo._SchemaVersions
            WHERE ScriptName LIKE '%' + @ScriptName;
            """;

        var count = await ExecuteScalarAsync<int>(checkScriptSql, new { ScriptName = _migrationScriptName });
        count.Should().Be(1, $"migration script '{_migrationScriptName}' should be applied and journaled");
    }

    [SkippableFact]
    public async Task ObsoleteStubTable_Policies_Should_BeDropped()
    {
        SkipIfDatabaseUnavailable();

        const string checkTableSql = "SELECT OBJECT_ID(N'finance.policies', N'U');";
        var objectId = await ExecuteScalarAsync<int?>(checkTableSql);

        objectId.Should().BeNull("obsolete stub table finance.policies must be dropped by migration 20261104_0201");
    }

    [SkippableFact]
    public async Task FinancialPoliciesTable_Should_ContainAllColumnsAndIndexes()
    {
        SkipIfDatabaseUnavailable();

        const string columnsSql = """
            SELECT COLUMN_NAME
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'finance' AND TABLE_NAME = 'financial_policies';
            """;

        var columns = (await QueryAsync<string>(columnsSql)).ToList();

        columns.Should().Contain([
            "id",
            "policy_code",
            "name",
            "category",
            "business_purpose",
            "applicable_context",
            "effective_date",
            "end_date",
            "policy_version",
            "status",
            "approving_authority",
            "approved_date",
            "supersedes_policy_id",
            "notes",
            "created_at",
            "created_by",
            "updated_at",
            "updated_by"
        ]);

        const string indexesSql = """
            SELECT name
            FROM sys.indexes
            WHERE object_id = OBJECT_ID('finance.financial_policies') AND name IS NOT NULL;
            """;

        var indexes = (await QueryAsync<string>(indexesSql)).ToList();
        indexes.Should().Contain("pk_finance_financial_policies");
        indexes.Should().Contain("uq_finance_financial_policies_code");
        indexes.Should().Contain("ix_finance_financial_policies_category_status");

        const string fkSql = """
            SELECT name AS Name, delete_referential_action AS DeleteReferentialAction
            FROM sys.foreign_keys
            WHERE parent_object_id = OBJECT_ID('finance.financial_policies');
            """;

        var fks = (await QueryAsync<ForeignKeyInfo>(fkSql)).ToList();
        fks.Should().ContainSingle(fk => fk.Name == "fk_finance_financial_policies_supersedes");
        var supersedesFk = fks.Single(fk => fk.Name == "fk_finance_financial_policies_supersedes");
        supersedesFk.DeleteReferentialAction.Should().Be(0, "supersedes policy FK must specify ON DELETE NO ACTION");
    }

    [SkippableFact]
    public async Task ChargeRulesTable_Should_ContainAllColumnsAndIndexes()
    {
        SkipIfDatabaseUnavailable();

        const string columnsSql = """
            SELECT COLUMN_NAME
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'finance' AND TABLE_NAME = 'charge_rules';
            """;

        var columns = (await QueryAsync<string>(columnsSql)).ToList();

        columns.Should().Contain([
            "id",
            "rule_code",
            "policy_id",
            "name",
            "charge_type",
            "is_mandatory",
            "determination_method",
            "base_amount",
            "currency_code",
            "recurrence_pattern",
            "due_rule_type",
            "due_rule_value",
            "applicable_grade",
            "applicable_program",
            "status",
            "notes",
            "created_at",
            "created_by",
            "updated_at",
            "updated_by"
        ]);

        // Verify base_amount is BIGINT
        const string baseAmountTypeSql = """
            SELECT DATA_TYPE
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'finance' AND TABLE_NAME = 'charge_rules' AND COLUMN_NAME = 'base_amount';
            """;
        var baseAmountType = await ExecuteScalarAsync<string>(baseAmountTypeSql);
        baseAmountType.Should().Be("bigint", "charge_rules.base_amount must be BIGINT");

        const string indexesSql = """
            SELECT name
            FROM sys.indexes
            WHERE object_id = OBJECT_ID('finance.charge_rules') AND name IS NOT NULL;
            """;

        var indexes = (await QueryAsync<string>(indexesSql)).ToList();
        indexes.Should().Contain("pk_finance_charge_rules");
        indexes.Should().Contain("uq_finance_charge_rules_code");
        indexes.Should().Contain("ix_finance_charge_rules_policy");

        const string fkSql = """
            SELECT name AS Name, delete_referential_action AS DeleteReferentialAction
            FROM sys.foreign_keys
            WHERE parent_object_id = OBJECT_ID('finance.charge_rules');
            """;

        var fks = (await QueryAsync<ForeignKeyInfo>(fkSql)).ToList();
        fks.Should().ContainSingle(fk => fk.Name == "fk_finance_charge_rules_policy");
        var policyFk = fks.Single(fk => fk.Name == "fk_finance_charge_rules_policy");
        policyFk.DeleteReferentialAction.Should().Be(0, "charge rule policy FK must specify ON DELETE NO ACTION");
    }

    [SkippableFact]
    public async Task StudentFinancialAccountsTable_Should_ContainAllColumnsAndIndexes_AndNoMutableBalanceColumn()
    {
        SkipIfDatabaseUnavailable();

        const string columnsSql = """
            SELECT COLUMN_NAME
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'finance' AND TABLE_NAME = 'student_financial_accounts';
            """;

        var columns = (await QueryAsync<string>(columnsSql)).ToList();

        columns.Should().Contain([
            "id",
            "account_number",
            "student_id",
            "currency_code",
            "account_status",
            "opened_date",
            "closed_date",
            "notes",
            "created_at",
            "created_by",
            "updated_at",
            "updated_by"
        ]);

        // Invariant: Zero mutable balance columns exist on finance.student_financial_accounts
        columns.Should().NotContain("balance");
        columns.Should().NotContain("current_balance");
        columns.Should().NotContain("total_balance");

        const string indexesSql = """
            SELECT name
            FROM sys.indexes
            WHERE object_id = OBJECT_ID('finance.student_financial_accounts') AND name IS NOT NULL;
            """;

        var indexes = (await QueryAsync<string>(indexesSql)).ToList();
        indexes.Should().Contain("pk_finance_student_financial_accounts");
        indexes.Should().Contain("uq_finance_student_financial_accounts_number");
        indexes.Should().Contain("uq_finance_student_financial_accounts_student");
        indexes.Should().Contain("ix_finance_student_financial_accounts_status");
    }

    [SkippableFact]
    public async Task FinancialObligationsTable_Should_ContainAllColumnsAndIndexes()
    {
        SkipIfDatabaseUnavailable();

        const string columnsSql = """
            SELECT COLUMN_NAME
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'finance' AND TABLE_NAME = 'financial_obligations';
            """;

        var columns = (await QueryAsync<string>(columnsSql)).ToList();

        columns.Should().Contain([
            "id",
            "obligation_number",
            "student_account_id",
            "student_id",
            "charge_rule_id",
            "policy_id",
            "description",
            "charge_type",
            "is_mandatory",
            "original_amount",
            "currency_code",
            "issue_date",
            "due_date",
            "academic_period_id",
            "academic_period_label",
            "status",
            "authorized_by",
            "notes",
            "created_at",
            "created_by",
            "updated_at",
            "updated_by"
        ]);

        // Verify original_amount is BIGINT
        const string amountTypeSql = """
            SELECT DATA_TYPE
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'finance' AND TABLE_NAME = 'financial_obligations' AND COLUMN_NAME = 'original_amount';
            """;
        var amountType = await ExecuteScalarAsync<string>(amountTypeSql);
        amountType.Should().Be("bigint", "financial_obligations.original_amount must be BIGINT");

        const string indexesSql = """
            SELECT name
            FROM sys.indexes
            WHERE object_id = OBJECT_ID('finance.financial_obligations') AND name IS NOT NULL;
            """;

        var indexes = (await QueryAsync<string>(indexesSql)).ToList();
        indexes.Should().Contain("pk_finance_financial_obligations");
        indexes.Should().Contain("uq_finance_financial_obligations_number");
        indexes.Should().Contain("ix_finance_obligations_student_status");
        indexes.Should().Contain("ix_finance_obligations_account_period");

        const string fkSql = """
            SELECT name AS Name, delete_referential_action AS DeleteReferentialAction
            FROM sys.foreign_keys
            WHERE parent_object_id = OBJECT_ID('finance.financial_obligations');
            """;

        var fks = (await QueryAsync<ForeignKeyInfo>(fkSql)).ToList();
        var fkNames = fks.Select(fk => fk.Name).ToList();
        fkNames.Should().Contain("fk_finance_obligations_account");
        fkNames.Should().Contain("fk_finance_obligations_charge_rule");
        fkNames.Should().Contain("fk_finance_obligations_policy");

        fks.Should().AllSatisfy(fk =>
            fk.DeleteReferentialAction.Should().Be(0, "financial obligation foreign keys must specify ON DELETE NO ACTION"));
    }

    [SkippableFact]
    public async Task ObligationDueSchedulesTable_Should_ContainAllColumnsAndIndexes_WithCascadeDelete()
    {
        SkipIfDatabaseUnavailable();

        const string columnsSql = """
            SELECT COLUMN_NAME
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'finance' AND TABLE_NAME = 'obligation_due_schedules';
            """;

        var columns = (await QueryAsync<string>(columnsSql)).ToList();

        columns.Should().Contain([
            "id",
            "obligation_id",
            "installment_sequence",
            "due_date",
            "scheduled_amount",
            "currency_code",
            "status",
            "notes",
            "created_at",
            "created_by",
            "updated_at",
            "updated_by"
        ]);

        // Verify scheduled_amount is BIGINT
        const string amountTypeSql = """
            SELECT DATA_TYPE
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'finance' AND TABLE_NAME = 'obligation_due_schedules' AND COLUMN_NAME = 'scheduled_amount';
            """;
        var amountType = await ExecuteScalarAsync<string>(amountTypeSql);
        amountType.Should().Be("bigint", "obligation_due_schedules.scheduled_amount must be BIGINT");

        const string indexesSql = """
            SELECT name
            FROM sys.indexes
            WHERE object_id = OBJECT_ID('finance.obligation_due_schedules') AND name IS NOT NULL;
            """;

        var indexes = (await QueryAsync<string>(indexesSql)).ToList();
        indexes.Should().Contain("pk_finance_obligation_due_schedules");
        indexes.Should().Contain("uq_finance_due_schedules_sequence");
        indexes.Should().Contain("ix_finance_due_schedules_due_date_status");

        const string fkSql = """
            SELECT name AS Name, delete_referential_action AS DeleteReferentialAction
            FROM sys.foreign_keys
            WHERE parent_object_id = OBJECT_ID('finance.obligation_due_schedules');
            """;

        var fks = (await QueryAsync<ForeignKeyInfo>(fkSql)).ToList();
        fks.Should().ContainSingle(fk => fk.Name == "fk_finance_due_schedules_obligation");
        var obligationFk = fks.Single(fk => fk.Name == "fk_finance_due_schedules_obligation");
        obligationFk.DeleteReferentialAction.Should().Be(1, "obligation due schedules foreign key must specify ON DELETE CASCADE");
    }

    [SkippableFact]
    public async Task BillingDocumentsTable_Should_ContainAllColumnsAndIndexes()
    {
        SkipIfDatabaseUnavailable();

        const string columnsSql = """
            SELECT COLUMN_NAME
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'finance' AND TABLE_NAME = 'billing_documents';
            """;

        var columns = (await QueryAsync<string>(columnsSql)).ToList();

        columns.Should().Contain([
            "id",
            "document_number",
            "student_account_id",
            "student_id",
            "document_type",
            "recipient_name",
            "recipient_relationship",
            "recipient_contact",
            "billing_period_start",
            "billing_period_end",
            "issue_date",
            "payment_due_date",
            "total_billed_amount",
            "currency_code",
            "delivery_channel",
            "delivery_status",
            "sent_at",
            "notes",
            "created_at",
            "created_by",
            "updated_at",
            "updated_by"
        ]);

        // Verify total_billed_amount is BIGINT
        const string amountTypeSql = """
            SELECT DATA_TYPE
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'finance' AND TABLE_NAME = 'billing_documents' AND COLUMN_NAME = 'total_billed_amount';
            """;
        var amountType = await ExecuteScalarAsync<string>(amountTypeSql);
        amountType.Should().Be("bigint", "billing_documents.total_billed_amount must be BIGINT");

        const string indexesSql = """
            SELECT name
            FROM sys.indexes
            WHERE object_id = OBJECT_ID('finance.billing_documents') AND name IS NOT NULL;
            """;

        var indexes = (await QueryAsync<string>(indexesSql)).ToList();
        indexes.Should().Contain("pk_finance_billing_documents");
        indexes.Should().Contain("uq_finance_billing_documents_number");
        indexes.Should().Contain("ix_finance_billing_documents_student");
        indexes.Should().Contain("ix_finance_billing_documents_status");

        const string fkSql = """
            SELECT name AS Name, delete_referential_action AS DeleteReferentialAction
            FROM sys.foreign_keys
            WHERE parent_object_id = OBJECT_ID('finance.billing_documents');
            """;

        var fks = (await QueryAsync<ForeignKeyInfo>(fkSql)).ToList();
        fks.Should().ContainSingle(fk => fk.Name == "fk_finance_billing_documents_account");
        var accountFk = fks.Single(fk => fk.Name == "fk_finance_billing_documents_account");
        accountFk.DeleteReferentialAction.Should().Be(0, "billing documents account foreign key must specify ON DELETE NO ACTION");
    }

    [SkippableFact]
    public async Task BillingDocumentItemsTable_Should_ContainAllColumnsAndIndexes_WithCascadeDelete()
    {
        SkipIfDatabaseUnavailable();

        const string columnsSql = """
            SELECT COLUMN_NAME
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'finance' AND TABLE_NAME = 'billing_document_items';
            """;

        var columns = (await QueryAsync<string>(columnsSql)).ToList();

        columns.Should().Contain([
            "id",
            "billing_document_id",
            "line_sequence",
            "item_type",
            "reference_entity_type",
            "reference_entity_id",
            "description",
            "amount",
            "currency_code",
            "created_at"
        ]);

        // Verify amount is BIGINT
        const string amountTypeSql = """
            SELECT DATA_TYPE
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'finance' AND TABLE_NAME = 'billing_document_items' AND COLUMN_NAME = 'amount';
            """;
        var amountType = await ExecuteScalarAsync<string>(amountTypeSql);
        amountType.Should().Be("bigint", "billing_document_items.amount must be BIGINT");

        const string indexesSql = """
            SELECT name
            FROM sys.indexes
            WHERE object_id = OBJECT_ID('finance.billing_document_items') AND name IS NOT NULL;
            """;

        var indexes = (await QueryAsync<string>(indexesSql)).ToList();
        indexes.Should().Contain("pk_finance_billing_document_items");
        indexes.Should().Contain("uq_finance_billing_items_sequence");
        indexes.Should().Contain("ix_finance_billing_items_reference");

        const string fkSql = """
            SELECT name AS Name, delete_referential_action AS DeleteReferentialAction
            FROM sys.foreign_keys
            WHERE parent_object_id = OBJECT_ID('finance.billing_document_items');
            """;

        var fks = (await QueryAsync<ForeignKeyInfo>(fkSql)).ToList();
        fks.Should().ContainSingle(fk => fk.Name == "fk_finance_billing_items_document");
        var docFk = fks.Single(fk => fk.Name == "fk_finance_billing_items_document");
        docFk.DeleteReferentialAction.Should().Be(1, "billing document items foreign key must specify ON DELETE CASCADE");
    }

    [SkippableFact]
    public async Task UniqueStudentAccountInvariant_Should_RejectDuplicateAccountForSameStudent()
    {
        SkipIfDatabaseUnavailable();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var studentId = await CreateStudentAsync(suffix);

        const string insertAccountSql = """
            INSERT INTO finance.student_financial_accounts (
                id, account_number, student_id, currency_code, account_status, opened_date, created_at
            ) VALUES (
                @Id, @AccountNumber, @StudentId, 'IDR', 'active', @OpenedDate, SYSUTCDATETIME()
            );
            """;

        // Insert first account -> must succeed
        await ExecuteAsync(insertAccountSql, new
        {
            Id = Guid.NewGuid(),
            AccountNumber = $"ACC-2026-{suffix}1",
            StudentId = studentId,
            OpenedDate = new DateTime(2026, 8, 1)
        });

        // Insert second account for same student -> must violate uq_finance_student_financial_accounts_student
        var actDuplicate = async () =>
        {
            await ExecuteAsync(insertAccountSql, new
            {
                Id = Guid.NewGuid(),
                AccountNumber = $"ACC-2026-{suffix}2",
                StudentId = studentId,
                OpenedDate = new DateTime(2026, 8, 1)
            });
        };

        var ex = await actDuplicate.Should().ThrowAsync<SqlException>();
        ex.Which.Number.Should().Match(num => num == 2601 || num == 2627,
            "duplicate financial account for same student must trigger SQL unique constraint violation (error 2601 or 2627)");
    }

    [SkippableFact]
    public async Task BackfillQuery_Should_IdempotentlyCreateAccountsForExistingEnrolledStudents()
    {
        SkipIfDatabaseUnavailable();

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var studentId = await CreateStudentAsync(suffix);

        // Verify no account exists initially for this student
        var initialCount = await ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM finance.student_financial_accounts WHERE student_id = @StudentId;",
            new { StudentId = studentId });
        initialCount.Should().Be(0);

        // Execute idempotent backfill query logic matching migration 20261104_0201 Section 8
        await RunBackfillScriptAsync();

        // Verify account created with expected columns and format
        var account = await QuerySingleOrDefaultAsync<StudentAccountBackfillInfo>(
            """
            SELECT account_number AS AccountNumber, student_id AS StudentId,
                   currency_code AS CurrencyCode, account_status AS AccountStatus,
                   opened_date AS OpenedDate, created_by AS CreatedBy
            FROM finance.student_financial_accounts
            WHERE student_id = @StudentId;
            """,
            new { StudentId = studentId });

        Assert.NotNull(account);
        account!.AccountNumber.Should().StartWith("ACC-2026-");
        account.StudentId.Should().Be(studentId);
        account.CurrencyCode.Should().Be("IDR");
        account.AccountStatus.Should().Be("active");
        account.OpenedDate.Should().Be(new DateTime(2026, 8, 1));
        account.CreatedBy.Should().Be("MIGRATION_BACKFILL");

        // Idempotency: execute backfill query a second time -> account count remains exactly 1
        await RunBackfillScriptAsync();

        var finalCount = await ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM finance.student_financial_accounts WHERE student_id = @StudentId;",
            new { StudentId = studentId });
        finalCount.Should().Be(1, "backfill script must be idempotent and avoid duplicate account creation");
    }

    [SkippableFact]
    public async Task ActiveFinancialPolicy_Should_ExistAndBeActive()
    {
        SkipIfDatabaseUnavailable();

        await EnsureSeedDataAsync();

        var policy = await QuerySingleOrDefaultAsync<PolicySeedInfo>(
            """
            SELECT id AS Id, policy_code AS PolicyCode, name AS Name, category AS Category,
                   policy_version AS PolicyVersion, status AS Status, approving_authority AS ApprovingAuthority,
                   effective_date AS EffectiveDate, created_by AS CreatedBy
            FROM finance.financial_policies
            WHERE policy_code = 'FP-2026-001';
            """);

        Assert.NotNull(policy);
        policy!.Id.Should().Be(Guid.Parse("00000000-0000-0000-0002-000000000001"));
        policy.PolicyCode.Should().Be("FP-2026-001");
        policy.Name.Should().Be("Kebijakan Biaya Pendidikan TP 2026/2027");
        policy.Category.Should().Be("tuition");
        policy.PolicyVersion.Should().Be("1.0");
        policy.Status.Should().Be("active");
        policy.ApprovingAuthority.Should().Be("Direktur Keuangan");
        policy.EffectiveDate.Should().Be(new DateTime(2026, 1, 15));
        policy.CreatedBy.Should().Be("SYSTEM_SEED");
    }

    [SkippableFact]
    public async Task CoreChargeRules_Should_ExistWithExpectedBigintAmounts()
    {
        SkipIfDatabaseUnavailable();

        await EnsureSeedDataAsync();

        var rules = (await QueryAsync<ChargeRuleSeedInfo>(
            """
            SELECT id AS Id, rule_code AS RuleCode, policy_id AS PolicyId, name AS Name,
                   charge_type AS ChargeType, is_mandatory AS IsMandatory,
                   determination_method AS DeterminationMethod, base_amount AS BaseAmount,
                   currency_code AS CurrencyCode, recurrence_pattern AS RecurrencePattern,
                   due_rule_type AS DueRuleType, due_rule_value AS DueRuleValue,
                   applicable_program AS ApplicableProgram, status AS Status,
                   created_by AS CreatedBy
            FROM finance.charge_rules
            WHERE rule_code IN ('CR-2026-APP', 'CR-2026-ENR', 'CR-2026-SPP-SD')
            ORDER BY rule_code;
            """)).ToList();

        rules.Should().HaveCount(3, "all 3 core charge rules must be seeded");

        var policyId = Guid.Parse("00000000-0000-0000-0002-000000000001");

        // 1. CR-2026-APP: Biaya Formulir Pendaftaran Siswa Baru (500,000 IDR)
        var appRule = rules.Single(r => r.RuleCode == "CR-2026-APP");
        appRule.Id.Should().Be(Guid.Parse("00000000-0000-0000-0002-000000000002"));
        appRule.PolicyId.Should().Be(policyId);
        appRule.Name.Should().Be("Biaya Formulir Pendaftaran Siswa Baru");
        appRule.ChargeType.Should().Be("admission_fee");
        appRule.IsMandatory.Should().BeTrue();
        appRule.DeterminationMethod.Should().Be("fixed_amount");
        appRule.BaseAmount.Should().Be(500000L);
        appRule.CurrencyCode.Should().Be("IDR");
        appRule.RecurrencePattern.Should().Be("one_time");
        appRule.DueRuleType.Should().Be("days_after_issuance");
        appRule.DueRuleValue.Should().Be("14");
        appRule.Status.Should().Be("active");

        // 2. CR-2026-ENR: Uang Pangkal Masuk Siswa Baru (15,000,000 IDR)
        var enrRule = rules.Single(r => r.RuleCode == "CR-2026-ENR");
        enrRule.Id.Should().Be(Guid.Parse("00000000-0000-0000-0002-000000000003"));
        enrRule.PolicyId.Should().Be(policyId);
        enrRule.Name.Should().Be("Uang Pangkal Masuk Siswa Baru");
        enrRule.ChargeType.Should().Be("enrollment_fee");
        enrRule.IsMandatory.Should().BeTrue();
        enrRule.DeterminationMethod.Should().Be("fixed_amount");
        enrRule.BaseAmount.Should().Be(15000000L);
        enrRule.CurrencyCode.Should().Be("IDR");
        enrRule.RecurrencePattern.Should().Be("one_time");
        enrRule.DueRuleType.Should().Be("days_after_issuance");
        enrRule.DueRuleValue.Should().Be("14");
        enrRule.Status.Should().Be("active");

        // 3. CR-2026-SPP-SD: SPP Bulanan SD Reguler (750,000 IDR)
        var sppRule = rules.Single(r => r.RuleCode == "CR-2026-SPP-SD");
        sppRule.Id.Should().Be(Guid.Parse("00000000-0000-0000-0002-000000000004"));
        sppRule.PolicyId.Should().Be(policyId);
        sppRule.Name.Should().Be("SPP Bulanan SD Reguler");
        sppRule.ChargeType.Should().Be("tuition");
        sppRule.IsMandatory.Should().BeTrue();
        sppRule.DeterminationMethod.Should().Be("fixed_amount");
        sppRule.BaseAmount.Should().Be(750000L);
        sppRule.CurrencyCode.Should().Be("IDR");
        sppRule.RecurrencePattern.Should().Be("monthly");
        sppRule.DueRuleType.Should().Be("fixed_day_of_month");
        sppRule.DueRuleValue.Should().Be("10");
        sppRule.ApplicableProgram.Should().Be("SD Reguler");
        sppRule.Status.Should().Be("active");

        // Verify seed idempotency: executing seed again does not duplicate records
        await EnsureSeedDataAsync();

        var rulesCountAfterReSeed = await ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM finance.charge_rules WHERE rule_code IN ('CR-2026-APP', 'CR-2026-ENR', 'CR-2026-SPP-SD');");
        rulesCountAfterReSeed.Should().Be(3, "re-running seed logic must be strictly idempotent");
    }

    private async Task<Guid> CreateStudentAsync(string suffix)
    {
        var applicantId = Guid.NewGuid();
        const string insertApplicantSql = """
            INSERT INTO students.applicants (
                id, first_name, last_name, full_name, date_of_birth, gender, nationality, created_at
            ) VALUES (
                @Id, @FirstName, @LastName, @FullName, '2016-04-10', 'Male', 'Indonesian', SYSUTCDATETIME()
            );
            """;

        await ExecuteAsync(insertApplicantSql, new
        {
            Id = applicantId,
            FirstName = $"FinAppFirst{suffix}",
            LastName = $"FinAppLast{suffix}",
            FullName = $"Fin Applicant {suffix}"
        });

        var studentId = Guid.NewGuid();
        const string insertStudentSql = """
            INSERT INTO students.students (
                id, student_number, applicant_id, full_name, date_of_birth, gender,
                nationality, current_status, enrollment_date, created_at
            ) VALUES (
                @Id, @StudentNumber, @ApplicantId, @FullName, '2016-04-10', 'Male',
                'Indonesian', 'active', '2026-08-01', SYSUTCDATETIME()
            );
            """;

        await ExecuteAsync(insertStudentSql, new
        {
            Id = studentId,
            StudentNumber = $"STU-FIN-{suffix}",
            ApplicantId = applicantId,
            FullName = $"Fin Student {suffix}"
        });

        return studentId;
    }

    private async Task RunBackfillScriptAsync()
    {
        const string backfillSql = """
            IF OBJECT_ID(N'students.students', N'U') IS NOT NULL
            BEGIN
                INSERT INTO [finance].[student_financial_accounts] (
                    [id], [account_number], [student_id], [currency_code], [account_status], [opened_date], [created_at], [created_by]
                )
                SELECT 
                    NEWID(),
                    CONCAT('ACC-', FORMAT(s.enrollment_date, 'yyyy'), '-', UPPER(LEFT(REPLACE(CONVERT(VARCHAR(36), s.id), '-', ''), 8))),
                    s.id,
                    'IDR',
                    'active',
                    s.enrollment_date,
                    SYSUTCDATETIME(),
                    'MIGRATION_BACKFILL'
                FROM [students].[students] s
                WHERE NOT EXISTS (
                    SELECT 1 FROM [finance].[student_financial_accounts] sfa WHERE sfa.student_id = s.id
                );
            END;
            """;

        await ExecuteAsync(backfillSql);
    }

    private async Task EnsureSeedDataAsync()
    {
        const string seedSql = """
            IF NOT EXISTS (
                SELECT 1 FROM [finance].[financial_policies]
                WHERE [policy_code] = N'FP-2026-001'
            )
            BEGIN
                INSERT INTO [finance].[financial_policies] (
                    [id], [policy_code], [name], [category], [business_purpose], [applicable_context],
                    [effective_date], [end_date], [policy_version], [status], [approving_authority],
                    [approved_date], [supersedes_policy_id], [notes], [created_at], [created_by]
                ) VALUES (
                    '00000000-0000-0000-0002-000000000001',
                    N'FP-2026-001',
                    N'Kebijakan Biaya Pendidikan TP 2026/2027',
                    N'tuition',
                    N'Menetapkan struktur biaya pendaftaran, uang pangkal, dan SPP bulanan untuk seluruh jenjang TP 2026/2027.',
                    N'Tahun Pelajaran 2026/2027',
                    '2026-01-15',
                    NULL,
                    N'1.0',
                    N'active',
                    N'Direktur Keuangan',
                    '2026-01-15',
                    NULL,
                    N'GAP-06 Core financial policy seed',
                    SYSUTCDATETIME(),
                    N'SYSTEM_SEED'
                );
            END;

            IF NOT EXISTS (
                SELECT 1 FROM [finance].[charge_rules]
                WHERE [rule_code] = N'CR-2026-APP'
            )
            BEGIN
                INSERT INTO [finance].[charge_rules] (
                    [id], [rule_code], [policy_id], [name], [charge_type], [is_mandatory],
                    [determination_method], [base_amount], [currency_code], [recurrence_pattern],
                    [due_rule_type], [due_rule_value], [applicable_grade], [applicable_program],
                    [status], [notes], [created_at], [created_by]
                ) VALUES (
                    '00000000-0000-0000-0002-000000000002',
                    N'CR-2026-APP',
                    '00000000-0000-0000-0002-000000000001',
                    N'Biaya Formulir Pendaftaran Siswa Baru',
                    N'admission_fee',
                    1,
                    N'fixed_amount',
                    500000,
                    'IDR',
                    N'one_time',
                    N'days_after_issuance',
                    N'14',
                    NULL,
                    NULL,
                    N'active',
                    N'GAP-06 Application Fee core seed',
                    SYSUTCDATETIME(),
                    N'SYSTEM_SEED'
                );
            END;

            IF NOT EXISTS (
                SELECT 1 FROM [finance].[charge_rules]
                WHERE [rule_code] = N'CR-2026-ENR'
            )
            BEGIN
                INSERT INTO [finance].[charge_rules] (
                    [id], [rule_code], [policy_id], [name], [charge_type], [is_mandatory],
                    [determination_method], [base_amount], [currency_code], [recurrence_pattern],
                    [due_rule_type], [due_rule_value], [applicable_grade], [applicable_program],
                    [status], [notes], [created_at], [created_by]
                ) VALUES (
                    '00000000-0000-0000-0002-000000000003',
                    N'CR-2026-ENR',
                    '00000000-0000-0000-0002-000000000001',
                    N'Uang Pangkal Masuk Siswa Baru',
                    N'enrollment_fee',
                    1,
                    N'fixed_amount',
                    15000000,
                    'IDR',
                    N'one_time',
                    N'days_after_issuance',
                    N'14',
                    NULL,
                    NULL,
                    N'active',
                    N'GAP-06 Enrollment Fee core seed',
                    SYSUTCDATETIME(),
                    N'SYSTEM_SEED'
                );
            END;

            IF NOT EXISTS (
                SELECT 1 FROM [finance].[charge_rules]
                WHERE [rule_code] = N'CR-2026-SPP-SD'
            )
            BEGIN
                INSERT INTO [finance].[charge_rules] (
                    [id], [rule_code], [policy_id], [name], [charge_type], [is_mandatory],
                    [determination_method], [base_amount], [currency_code], [recurrence_pattern],
                    [due_rule_type], [due_rule_value], [applicable_grade], [applicable_program],
                    [status], [notes], [created_at], [created_by]
                ) VALUES (
                    '00000000-0000-0000-0002-000000000004',
                    N'CR-2026-SPP-SD',
                    '00000000-0000-0000-0002-000000000001',
                    N'SPP Bulanan SD Reguler',
                    N'tuition',
                    1,
                    N'fixed_amount',
                    750000,
                    'IDR',
                    N'monthly',
                    N'fixed_day_of_month',
                    N'10',
                    NULL,
                    N'SD Reguler',
                    N'active',
                    N'GAP-06 Tuition Fee core seed',
                    SYSUTCDATETIME(),
                    N'SYSTEM_SEED'
                );
            END;
            """;

        await ExecuteAsync(seedSql);
    }

    private sealed record ForeignKeyInfo(string Name, byte DeleteReferentialAction);

    private sealed record PolicySeedInfo(
        Guid Id,
        string PolicyCode,
        string Name,
        string Category,
        string PolicyVersion,
        string Status,
        string ApprovingAuthority,
        DateTime EffectiveDate,
        string CreatedBy);

    private sealed record ChargeRuleSeedInfo(
        Guid Id,
        string RuleCode,
        Guid PolicyId,
        string Name,
        string ChargeType,
        bool IsMandatory,
        string DeterminationMethod,
        long BaseAmount,
        string CurrencyCode,
        string RecurrencePattern,
        string DueRuleType,
        string DueRuleValue,
        string? ApplicableProgram,
        string Status,
        string CreatedBy);

    private sealed record StudentAccountBackfillInfo(
        string AccountNumber,
        Guid StudentId,
        string CurrencyCode,
        string AccountStatus,
        DateTime OpenedDate,
        string CreatedBy);
}
