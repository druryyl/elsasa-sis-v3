namespace Elsasa.Sis.IntegrationTests.Features.Finance.Migration;

using Dapper;
using Elsasa.Sis.Database;
using Elsasa.Sis.IntegrationTests.Fixtures;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Xunit;

/// <summary>
/// Integration test suite verifying DbUp schema migration 20261104_0301 (IS007 / CR-FIN-PAYMENTS-RECEIVABLES, P1-S01, P4-S11, ADR-002, ADR-006).
/// Validates script journaling via DatabaseMigrator, dual reference pattern alterations on student financial accounts and obligations,
/// candidate check constraints, filtered unique indexes, table structure and constraints for payers, payments, and payment allocations,
/// and dynamic aging and balance calculation in projection view finance.v_student_receivables.
/// </summary>
public sealed class FinancePaymentsReceivablesMigrationTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private const string _migrationScriptName = "20261104_0301_Finance_PaymentsReceivables_Tables.sql";

    [SkippableFact]
    public void FinancePaymentsReceivablesMigration_Should_RunSuccessfullyViaDatabaseMigrator()
    {
        SkipIfDatabaseUnavailable();

        var success = DatabaseMigrator.UpgradeDatabase(Factory.ContainerFixture.ConnectionString);

        success.Should().BeTrue("DatabaseMigrator.UpgradeDatabase should succeed idempotently on target database");
    }

    [SkippableFact]
    public async Task FinancePaymentsReceivablesMigration_Should_BeJournaledInSchemaVersions()
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
    public async Task StudentFinancialAccountsTable_Should_HaveDualReferenceColumnsAndConstraints()
    {
        SkipIfDatabaseUnavailable();

        // 1. Verify student_id is nullable
        const string studentIdNullSql = """
            SELECT is_nullable
            FROM sys.columns
            WHERE object_id = OBJECT_ID('finance.student_financial_accounts') AND name = 'student_id';
            """;
        var studentIdIsNullable = await ExecuteScalarAsync<bool>(studentIdNullSql);
        studentIdIsNullable.Should().BeTrue("student_financial_accounts.student_id must be nullable after migration 0301");

        // 2. Verify applicant_id exists, is uniqueidentifier, and is nullable
        const string applicantIdSql = """
            SELECT c.is_nullable AS IsNullable, t.name AS TypeName
            FROM sys.columns c
            JOIN sys.types t ON c.user_type_id = t.user_type_id
            WHERE c.object_id = OBJECT_ID('finance.student_financial_accounts') AND c.name = 'applicant_id';
            """;
        var applicantCol = await QuerySingleOrDefaultAsync<ColumnInfo>(applicantIdSql);
        applicantCol.Should().NotBeNull("student_financial_accounts.applicant_id column must exist");
        applicantCol!.IsNullable.Should().BeTrue("student_financial_accounts.applicant_id must be nullable");
        applicantCol.TypeName.Should().Be("uniqueidentifier", "applicant_id must be of type uniqueidentifier");

        // 3. Verify filtered unique indexes exist on student_id and applicant_id
        const string indexesSql = """
            SELECT name AS Name, is_unique AS IsUnique, has_filter AS HasFilter, filter_definition AS FilterDefinition
            FROM sys.indexes
            WHERE object_id = OBJECT_ID('finance.student_financial_accounts') AND name IS NOT NULL;
            """;
        var indexes = (await QueryAsync<IndexInfo>(indexesSql)).ToList();

        var studentIndex = indexes.SingleOrDefault(i => i.Name == "ux_finance_student_accounts_student_id");
        studentIndex.Should().NotBeNull("filtered unique index ux_finance_student_accounts_student_id must exist");
        studentIndex!.IsUnique.Should().BeTrue();
        studentIndex.HasFilter.Should().BeTrue();

        var applicantIndex = indexes.SingleOrDefault(i => i.Name == "ux_finance_student_accounts_applicant_id");
        applicantIndex.Should().NotBeNull("filtered unique index ux_finance_student_accounts_applicant_id must exist");
        applicantIndex!.IsUnique.Should().BeTrue();
        applicantIndex.HasFilter.Should().BeTrue();

        // 4. Verify candidate check constraint exists
        const string checkConstraintSql = """
            SELECT COUNT(1)
            FROM sys.check_constraints
            WHERE parent_object_id = OBJECT_ID('finance.student_financial_accounts')
              AND name = 'ck_finance_student_accounts_candidate';
            """;
        var checkCount = await ExecuteScalarAsync<int>(checkConstraintSql);
        checkCount.Should().Be(1, "check constraint ck_finance_student_accounts_candidate must exist");
    }

    [SkippableFact]
    public async Task StudentFinancialAccounts_Should_AllowNullStudentIdWithNonNullApplicantId()
    {
        SkipIfDatabaseUnavailable();

        await using var connection = await OpenConnectionAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var applicantId = Guid.NewGuid();

        var accountId = await InsertStudentAccountRawAsync(connection, suffix, studentId: null, applicantId: applicantId);

        var retrieved = await connection.QuerySingleOrDefaultAsync<AccountRow>(
            "SELECT id AS Id, student_id AS StudentId, applicant_id AS ApplicantId FROM finance.student_financial_accounts WHERE id = @Id;",
            new { Id = accountId });

        retrieved.Should().NotBeNull();
        retrieved!.StudentId.Should().BeNull("account should allow NULL student_id for pre-enrollment applicants");
        retrieved.ApplicantId.Should().Be(applicantId);
    }

    [SkippableFact]
    public async Task StudentFinancialAccounts_Should_AllowNullApplicantIdWithNonNullStudentId()
    {
        SkipIfDatabaseUnavailable();

        await using var connection = await OpenConnectionAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var studentId = Guid.NewGuid();

        var accountId = await InsertStudentAccountRawAsync(connection, suffix, studentId: studentId, applicantId: null);

        var retrieved = await connection.QuerySingleOrDefaultAsync<AccountRow>(
            "SELECT id AS Id, student_id AS StudentId, applicant_id AS ApplicantId FROM finance.student_financial_accounts WHERE id = @Id;",
            new { Id = accountId });

        retrieved.Should().NotBeNull();
        retrieved!.StudentId.Should().Be(studentId);
        retrieved.ApplicantId.Should().BeNull("account should allow NULL applicant_id for direct enrolled students");
    }

    [SkippableFact]
    public async Task StudentFinancialAccounts_CheckConstraint_Should_RejectRowsWhereBothStudentAndApplicantAreNull()
    {
        SkipIfDatabaseUnavailable();

        await using var connection = await OpenConnectionAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var act = async () =>
        {
            await InsertStudentAccountRawAsync(connection, suffix, studentId: null, applicantId: null);
        };

        var ex = await act.Should().ThrowAsync<SqlException>();
        ex.Which.Number.Should().Be(547, "violating ck_finance_student_accounts_candidate must throw check constraint violation (547)");
    }

    [SkippableFact]
    public async Task StudentFinancialAccounts_FilteredIndexes_Should_EnforceUniquenessOnStudentIdAndApplicantId()
    {
        SkipIfDatabaseUnavailable();

        await using var connection = await OpenConnectionAsync();
        var studentId = Guid.NewGuid();
        var applicantId = Guid.NewGuid();

        // 1. Insert first account with studentId -> succeeds
        await InsertStudentAccountRawAsync(connection, Guid.NewGuid().ToString("N")[..8], studentId: studentId, applicantId: null);

        // Attempt second account with same studentId -> must fail unique index
        var actDuplicateStudent = async () =>
        {
            await InsertStudentAccountRawAsync(connection, Guid.NewGuid().ToString("N")[..8], studentId: studentId, applicantId: null);
        };
        var exStudent = await actDuplicateStudent.Should().ThrowAsync<SqlException>();
        exStudent.Which.Number.Should().Match(n => n == 2601 || n == 2627, "duplicate student_id must violate unique index");

        // 2. Insert first account with applicantId -> succeeds
        await InsertStudentAccountRawAsync(connection, Guid.NewGuid().ToString("N")[..8], studentId: null, applicantId: applicantId);

        // Attempt second account with same applicantId -> must fail unique index
        var actDuplicateApplicant = async () =>
        {
            await InsertStudentAccountRawAsync(connection, Guid.NewGuid().ToString("N")[..8], studentId: null, applicantId: applicantId);
        };
        var exApplicant = await actDuplicateApplicant.Should().ThrowAsync<SqlException>();
        exApplicant.Which.Number.Should().Match(n => n == 2601 || n == 2627, "duplicate applicant_id must violate unique index");

        // 3. Filtered uniqueness allows multiple NULLs for student_id (when applicant_id differs)
        var actMultipleNullStudents = async () =>
        {
            await InsertStudentAccountRawAsync(connection, Guid.NewGuid().ToString("N")[..8], studentId: null, applicantId: Guid.NewGuid());
            await InsertStudentAccountRawAsync(connection, Guid.NewGuid().ToString("N")[..8], studentId: null, applicantId: Guid.NewGuid());
        };
        await actMultipleNullStudents.Should().NotThrowAsync("filtered unique index must allow multiple NULL student_id values");

        // 4. Filtered uniqueness allows multiple NULLs for applicant_id (when student_id differs)
        var actMultipleNullApplicants = async () =>
        {
            await InsertStudentAccountRawAsync(connection, Guid.NewGuid().ToString("N")[..8], studentId: Guid.NewGuid(), applicantId: null);
            await InsertStudentAccountRawAsync(connection, Guid.NewGuid().ToString("N")[..8], studentId: Guid.NewGuid(), applicantId: null);
        };
        await actMultipleNullApplicants.Should().NotThrowAsync("filtered unique index must allow multiple NULL applicant_id values");
    }

    [SkippableFact]
    public async Task FinancialObligationsTable_Should_HaveDualReferenceColumnsAndConstraints()
    {
        SkipIfDatabaseUnavailable();

        // 1. Verify student_id is nullable
        const string studentIdNullSql = """
            SELECT is_nullable
            FROM sys.columns
            WHERE object_id = OBJECT_ID('finance.financial_obligations') AND name = 'student_id';
            """;
        var studentIdIsNullable = await ExecuteScalarAsync<bool>(studentIdNullSql);
        studentIdIsNullable.Should().BeTrue("financial_obligations.student_id must be nullable after migration 0301");

        // 2. Verify applicant_id exists, is uniqueidentifier, and is nullable
        const string applicantIdSql = """
            SELECT c.is_nullable AS IsNullable, t.name AS TypeName
            FROM sys.columns c
            JOIN sys.types t ON c.user_type_id = t.user_type_id
            WHERE c.object_id = OBJECT_ID('finance.financial_obligations') AND c.name = 'applicant_id';
            """;
        var applicantCol = await QuerySingleOrDefaultAsync<ColumnInfo>(applicantIdSql);
        applicantCol.Should().NotBeNull("financial_obligations.applicant_id column must exist");
        applicantCol!.IsNullable.Should().BeTrue();
        applicantCol.TypeName.Should().Be("uniqueidentifier");

        // 3. Verify indexes exist
        const string indexesSql = """
            SELECT name
            FROM sys.indexes
            WHERE object_id = OBJECT_ID('finance.financial_obligations') AND name IS NOT NULL;
            """;
        var indexes = (await QueryAsync<string>(indexesSql)).ToList();
        indexes.Should().Contain("ix_finance_obligations_student_status");
        indexes.Should().Contain("ix_finance_obligations_applicant_status");

        // 4. Verify candidate check constraint exists
        const string checkConstraintSql = """
            SELECT COUNT(1)
            FROM sys.check_constraints
            WHERE parent_object_id = OBJECT_ID('finance.financial_obligations')
              AND name = 'ck_finance_obligations_candidate';
            """;
        var checkCount = await ExecuteScalarAsync<int>(checkConstraintSql);
        checkCount.Should().Be(1, "check constraint ck_finance_obligations_candidate must exist");
    }

    [SkippableFact]
    public async Task FinancialObligations_Should_AllowNullStudentIdWithNonNullApplicantId()
    {
        SkipIfDatabaseUnavailable();

        await using var connection = await OpenConnectionAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var applicantId = Guid.NewGuid();
        var accountId = await InsertStudentAccountRawAsync(connection, suffix, studentId: null, applicantId: applicantId);
        var policyId = await FinanceIntegrationTestHelpers.EnsureActivePolicyAsync(connection);

        var obligationId = await InsertObligationRawAsync(
            connection,
            accountId: accountId,
            studentId: null,
            applicantId: applicantId,
            policyId: policyId,
            amount: 500000,
            dueDayOffset: 10);

        var retrieved = await connection.QuerySingleOrDefaultAsync<ObligationRow>(
            "SELECT id AS Id, student_id AS StudentId, applicant_id AS ApplicantId FROM finance.financial_obligations WHERE id = @Id;",
            new { Id = obligationId });

        retrieved.Should().NotBeNull();
        retrieved!.StudentId.Should().BeNull();
        retrieved.ApplicantId.Should().Be(applicantId);
    }

    [SkippableFact]
    public async Task FinancialObligations_CheckConstraint_Should_RejectRowsWhereBothStudentAndApplicantAreNull()
    {
        SkipIfDatabaseUnavailable();

        await using var connection = await OpenConnectionAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var accountId = await InsertStudentAccountRawAsync(connection, suffix, studentId: Guid.NewGuid(), applicantId: null);
        var policyId = await FinanceIntegrationTestHelpers.EnsureActivePolicyAsync(connection);

        var act = async () =>
        {
            await InsertObligationRawAsync(
                connection,
                accountId: accountId,
                studentId: null,
                applicantId: null,
                policyId: policyId,
                amount: 500000,
                dueDayOffset: 10);
        };

        var ex = await act.Should().ThrowAsync<SqlException>();
        ex.Which.Number.Should().Be(547, "financial_obligations with both student_id and applicant_id null must throw check constraint violation (547)");
    }

    [SkippableFact]
    public async Task PayersTable_Should_ContainAllColumnsIndexesAndUniqueConstraint()
    {
        SkipIfDatabaseUnavailable();

        // 1. Verify columns
        const string columnsSql = """
            SELECT COLUMN_NAME
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'finance' AND TABLE_NAME = 'payers';
            """;
        var columns = (await QueryAsync<string>(columnsSql)).ToList();

        columns.Should().Contain([
            "id",
            "payer_number",
            "payer_name",
            "payer_type",
            "identification_number",
            "email",
            "phone_number",
            "billing_address",
            "bank_account_name",
            "bank_account_number",
            "bank_name",
            "notes",
            "created_at",
            "created_by",
            "updated_at",
            "updated_by"
        ]);

        // 2. Verify indexes
        const string indexesSql = """
            SELECT name AS Name, is_unique AS IsUnique, has_filter AS HasFilter
            FROM sys.indexes
            WHERE object_id = OBJECT_ID('finance.payers') AND name IS NOT NULL;
            """;
        var indexes = (await QueryAsync<IndexInfo>(indexesSql)).ToList();

        indexes.Should().Contain(i => i.Name == "pk_finance_payers");
        indexes.Should().Contain(i => i.Name == "uq_finance_payers_number" && i.IsUnique);
        indexes.Should().Contain(i => i.Name == "ix_finance_payers_identification" && i.HasFilter);
        indexes.Should().Contain(i => i.Name == "ix_finance_payers_contact");

        // 3. Verify unique constraint on payer_number
        await using var connection = await OpenConnectionAsync();
        var payerNumber = $"PYR-{Guid.NewGuid().ToString("N")[..8]}";
        await InsertPayerRawAsync(connection, payerNumber, "Payer One");

        var actDuplicate = async () =>
        {
            await InsertPayerRawAsync(connection, payerNumber, "Payer Two");
        };

        var ex = await actDuplicate.Should().ThrowAsync<SqlException>();
        ex.Which.Number.Should().Match(n => n == 2601 || n == 2627, "duplicate payer_number must violate unique constraint");
    }

    [SkippableFact]
    public async Task PaymentsTable_Should_ContainAllColumnsIndexesAndForeignKeys()
    {
        SkipIfDatabaseUnavailable();

        // 1. Verify columns
        const string columnsSql = """
            SELECT COLUMN_NAME
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'finance' AND TABLE_NAME = 'payments';
            """;
        var columns = (await QueryAsync<string>(columnsSql)).ToList();

        columns.Should().Contain([
            "id",
            "payment_number",
            "student_account_id",
            "student_id",
            "applicant_id",
            "payer_id",
            "payment_amount",
            "currency_code",
            "payment_method",
            "external_reference",
            "payment_date",
            "submission_date",
            "confirmation_status",
            "confirmation_date",
            "confirmed_by",
            "receipt_number",
            "receipt_issued_at",
            "rejection_reason",
            "notes",
            "created_at",
            "created_by",
            "updated_at",
            "updated_by"
        ]);

        // 2. Verify payment_amount is BIGINT
        const string amountTypeSql = """
            SELECT DATA_TYPE
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'finance' AND TABLE_NAME = 'payments' AND COLUMN_NAME = 'payment_amount';
            """;
        var amountType = await ExecuteScalarAsync<string>(amountTypeSql);
        amountType.Should().Be("bigint", "payments.payment_amount must be BIGINT");

        // 3. Verify indexes
        const string indexesSql = """
            SELECT name AS Name, is_unique AS IsUnique, has_filter AS HasFilter
            FROM sys.indexes
            WHERE object_id = OBJECT_ID('finance.payments') AND name IS NOT NULL;
            """;
        var indexes = (await QueryAsync<IndexInfo>(indexesSql)).ToList();

        indexes.Should().Contain(i => i.Name == "pk_finance_payments");
        indexes.Should().Contain(i => i.Name == "uq_finance_payments_number" && i.IsUnique);
        indexes.Should().Contain(i => i.Name == "uq_finance_payments_receipt_number" && i.IsUnique && i.HasFilter);
        indexes.Should().Contain(i => i.Name == "ix_finance_payments_queue");
        indexes.Should().Contain(i => i.Name == "ix_finance_payments_account");
        indexes.Should().Contain(i => i.Name == "ix_finance_payments_student_applicant");

        // 4. Verify foreign keys
        const string fkSql = """
            SELECT name AS Name, delete_referential_action AS DeleteReferentialAction
            FROM sys.foreign_keys
            WHERE parent_object_id = OBJECT_ID('finance.payments');
            """;
        var fks = (await QueryAsync<ForeignKeyInfo>(fkSql)).ToList();

        fks.Should().ContainSingle(fk => fk.Name == "fk_finance_payments_account");
        fks.Single(fk => fk.Name == "fk_finance_payments_account").DeleteReferentialAction.Should().Be(0);

        fks.Should().ContainSingle(fk => fk.Name == "fk_finance_payments_payer");
        fks.Single(fk => fk.Name == "fk_finance_payments_payer").DeleteReferentialAction.Should().Be(0);

        // 5. Verify candidate check constraint
        const string checkSql = """
            SELECT COUNT(1)
            FROM sys.check_constraints
            WHERE parent_object_id = OBJECT_ID('finance.payments') AND name = 'ck_finance_payments_candidate';
            """;
        var checkCount = await ExecuteScalarAsync<int>(checkSql);
        checkCount.Should().Be(1, "ck_finance_payments_candidate constraint must exist on finance.payments");
    }

    [SkippableFact]
    public async Task PaymentsTable_Should_EnforceUniqueReceiptNumberWhenNotNull_AndAllowMultipleNulls()
    {
        SkipIfDatabaseUnavailable();

        await using var connection = await OpenConnectionAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var studentId = Guid.NewGuid();
        var accountId = await InsertStudentAccountRawAsync(connection, suffix, studentId: studentId, applicantId: null);
        var payerId = await InsertPayerRawAsync(connection, $"PYR-{suffix}", "Test Payer");

        var receiptNumber = $"REC-2026-{suffix}";

        // First payment with receipt -> succeeds
        await InsertPaymentRawAsync(
            connection,
            accountId: accountId,
            studentId: studentId,
            applicantId: null,
            payerId: payerId,
            amount: 1000000,
            receiptNumber: receiptNumber,
            status: "confirmed");

        // Duplicate receipt number -> must violate filtered unique index
        var actDuplicate = async () =>
        {
            await InsertPaymentRawAsync(
                connection,
                accountId: accountId,
                studentId: studentId,
                applicantId: null,
                payerId: payerId,
                amount: 2000000,
                receiptNumber: receiptNumber,
                status: "confirmed");
        };

        var ex = await actDuplicate.Should().ThrowAsync<SqlException>();
        ex.Which.Number.Should().Match(n => n == 2601 || n == 2627, "duplicate receipt_number must violate unique index");

        // Multiple payments with receipt_number = NULL (submitted) -> must succeed
        var actMultipleNullReceipts = async () =>
        {
            await InsertPaymentRawAsync(connection, accountId, studentId, null, payerId, 500000, receiptNumber: null, status: "submitted");
            await InsertPaymentRawAsync(connection, accountId, studentId, null, payerId, 600000, receiptNumber: null, status: "submitted");
        };

        await actMultipleNullReceipts.Should().NotThrowAsync("filtered unique index must allow multiple payments with NULL receipt_number");
    }

    [SkippableFact]
    public async Task PaymentsTable_CheckConstraint_Should_RejectRowsWhereBothStudentAndApplicantAreNull()
    {
        SkipIfDatabaseUnavailable();

        await using var connection = await OpenConnectionAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var studentId = Guid.NewGuid();
        var accountId = await InsertStudentAccountRawAsync(connection, suffix, studentId: studentId, applicantId: null);
        var payerId = await InsertPayerRawAsync(connection, $"PYR-{suffix}", "Test Payer");

        var act = async () =>
        {
            await InsertPaymentRawAsync(
                connection,
                accountId: accountId,
                studentId: null,
                applicantId: null,
                payerId: payerId,
                amount: 1000000,
                receiptNumber: null,
                status: "submitted");
        };

        var ex = await act.Should().ThrowAsync<SqlException>();
        ex.Which.Number.Should().Be(547, "payments where both student_id and applicant_id are null must fail candidate check constraint (547)");
    }

    [SkippableFact]
    public async Task PaymentAllocationsTable_Should_ContainAllColumnsIndexesAndForeignKeys()
    {
        SkipIfDatabaseUnavailable();

        // 1. Verify columns
        const string columnsSql = """
            SELECT COLUMN_NAME
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'finance' AND TABLE_NAME = 'payment_allocations';
            """;
        var columns = (await QueryAsync<string>(columnsSql)).ToList();

        columns.Should().Contain([
            "id",
            "allocation_number",
            "student_account_id",
            "obligation_id",
            "source_type",
            "payment_id",
            "credit_balance_id",
            "allocated_amount",
            "currency_code",
            "allocation_date",
            "allocated_by",
            "status",
            "superseded_by_id",
            "reversal_reason",
            "notes",
            "created_at",
            "created_by",
            "updated_at",
            "updated_by"
        ]);

        // 2. Verify allocated_amount is BIGINT
        const string amountTypeSql = """
            SELECT DATA_TYPE
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'finance' AND TABLE_NAME = 'payment_allocations' AND COLUMN_NAME = 'allocated_amount';
            """;
        var amountType = await ExecuteScalarAsync<string>(amountTypeSql);
        amountType.Should().Be("bigint", "payment_allocations.allocated_amount must be BIGINT");

        // 3. Verify indexes
        const string indexesSql = """
            SELECT name AS Name, is_unique AS IsUnique
            FROM sys.indexes
            WHERE object_id = OBJECT_ID('finance.payment_allocations') AND name IS NOT NULL;
            """;
        var indexes = (await QueryAsync<IndexInfo>(indexesSql)).ToList();

        indexes.Should().Contain(i => i.Name == "pk_finance_payment_allocations");
        indexes.Should().Contain(i => i.Name == "uq_finance_allocations_number" && i.IsUnique);
        indexes.Should().Contain(i => i.Name == "ix_finance_allocations_obligation_status");
        indexes.Should().Contain(i => i.Name == "ix_finance_allocations_payment_status");

        // 4. Verify foreign keys
        const string fkSql = """
            SELECT name AS Name, delete_referential_action AS DeleteReferentialAction
            FROM sys.foreign_keys
            WHERE parent_object_id = OBJECT_ID('finance.payment_allocations');
            """;
        var fks = (await QueryAsync<ForeignKeyInfo>(fkSql)).ToList();

        fks.Should().ContainSingle(fk => fk.Name == "fk_finance_allocations_account");
        fks.Single(fk => fk.Name == "fk_finance_allocations_account").DeleteReferentialAction.Should().Be(0);

        fks.Should().ContainSingle(fk => fk.Name == "fk_finance_allocations_obligation");
        fks.Single(fk => fk.Name == "fk_finance_allocations_obligation").DeleteReferentialAction.Should().Be(0);

        fks.Should().ContainSingle(fk => fk.Name == "fk_finance_allocations_payment");
        fks.Single(fk => fk.Name == "fk_finance_allocations_payment").DeleteReferentialAction.Should().Be(0);

        fks.Should().ContainSingle(fk => fk.Name == "fk_finance_allocations_superseded");
        fks.Single(fk => fk.Name == "fk_finance_allocations_superseded").DeleteReferentialAction.Should().Be(0);
    }

    [SkippableFact]
    public async Task PaymentAllocationsTable_Should_SupportSelfReferentialSupersededLineage()
    {
        SkipIfDatabaseUnavailable();

        await using var connection = await OpenConnectionAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var studentId = Guid.NewGuid();
        var accountId = await InsertStudentAccountRawAsync(connection, suffix, studentId: studentId, applicantId: null);
        var policyId = await FinanceIntegrationTestHelpers.EnsureActivePolicyAsync(connection);
        var payerId = await InsertPayerRawAsync(connection, $"PYR-{suffix}", "Lineage Payer");
        var paymentId = await InsertPaymentRawAsync(connection, accountId, studentId, null, payerId, 1000000, $"REC-{suffix}", "confirmed");
        var obligationId = await InsertObligationRawAsync(connection, accountId, studentId, null, policyId, 1000000, 15);

        // 1. Initial allocation 1
        var alloc1Id = await InsertAllocationRawAsync(connection, accountId, obligationId, paymentId, 1000000, "superseded");

        // 2. Replacement allocation 2
        var alloc2Id = await InsertAllocationRawAsync(connection, accountId, obligationId, paymentId, 800000, "active");

        // 3. Link allocation 1 -> allocation 2
        const string updateSql = """
            UPDATE finance.payment_allocations
            SET superseded_by_id = @SupersededById,
                reversal_reason = 'Cashier reallocation correction'
            WHERE id = @Id;
            """;
        await connection.ExecuteAsync(updateSql, new { SupersededById = alloc2Id, Id = alloc1Id });

        var updatedAlloc1 = await connection.QuerySingleAsync<AllocationRow>(
            "SELECT id AS Id, superseded_by_id AS SupersededById, reversal_reason AS ReversalReason FROM finance.payment_allocations WHERE id = @Id;",
            new { Id = alloc1Id });

        updatedAlloc1.SupersededById.Should().Be(alloc2Id);
        updatedAlloc1.ReversalReason.Should().Be("Cashier reallocation correction");

        // 4. Invalid foreign key reference -> fails FK check
        var actInvalidFk = async () =>
        {
            await connection.ExecuteAsync(updateSql, new { SupersededById = Guid.NewGuid(), Id = alloc2Id });
        };
        var ex = await actInvalidFk.Should().ThrowAsync<SqlException>();
        ex.Which.Number.Should().Be(547, "invalid superseded_by_id must violate self-referencing foreign key constraint (547)");
    }

    [SkippableFact]
    public async Task StudentReceivablesView_Should_ExistAndContainAllExpectedColumns()
    {
        SkipIfDatabaseUnavailable();

        // 1. Verify view object exists
        const string viewObjSql = "SELECT OBJECT_ID(N'finance.v_student_receivables', N'V');";
        var viewId = await ExecuteScalarAsync<int?>(viewObjSql);
        viewId.Should().NotBeNull("view finance.v_student_receivables must exist in database");

        // 2. Verify all view columns
        const string columnsSql = """
            SELECT COLUMN_NAME
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = 'finance' AND TABLE_NAME = 'v_student_receivables';
            """;
        var columns = (await QueryAsync<string>(columnsSql)).ToList();

        columns.Should().Contain([
            "obligation_id",
            "obligation_number",
            "student_account_id",
            "student_id",
            "applicant_id",
            "charge_type",
            "description",
            "is_mandatory",
            "due_date",
            "original_amount",
            "currency_code",
            "academic_period_id",
            "academic_period_label",
            "obligation_status",
            "net_obligation",
            "total_paid",
            "outstanding_amount",
            "is_overdue",
            "days_overdue",
            "aging_bracket",
            "receivable_status"
        ]);
    }

    [SkippableFact]
    public async Task StudentReceivablesView_Should_ComputeAgingBracketsAndOutstandingBalancesAccurately()
    {
        SkipIfDatabaseUnavailable();

        await using var connection = await OpenConnectionAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var studentId = Guid.NewGuid();
        var accountId = await InsertStudentAccountRawAsync(connection, suffix, studentId: studentId, applicantId: null);
        var policyId = await FinanceIntegrationTestHelpers.EnsureActivePolicyAsync(connection);
        var payerId = await InsertPayerRawAsync(connection, $"PYR-{suffix}", "Aging Test Payer");
        var paymentId = await InsertPaymentRawAsync(connection, accountId, studentId, null, payerId, 10000000, $"REC-{suffix}", "confirmed");

        // Obligation 1: Future due date (+15 days) -> Current, current
        var obl1Id = await InsertObligationRawAsync(connection, accountId, studentId, null, policyId, 1000000, dueDayOffset: 15);

        // Obligation 2: Overdue 10 days ago (-10 days), partial paid 500,000 -> 1-30 Days, overdue
        var obl2Id = await InsertObligationRawAsync(connection, accountId, studentId, null, policyId, 2000000, dueDayOffset: -10);
        await InsertAllocationRawAsync(connection, accountId, obl2Id, paymentId, 500000, "active");

        // Obligation 3: Overdue 45 days ago (-45 days), unpaid -> 31-60 Days, overdue
        var obl3Id = await InsertObligationRawAsync(connection, accountId, studentId, null, policyId, 3000000, dueDayOffset: -45);

        // Obligation 4: Overdue 75 days ago (-75 days), unpaid -> 61-90 Days, overdue
        var obl4Id = await InsertObligationRawAsync(connection, accountId, studentId, null, policyId, 4000000, dueDayOffset: -75);

        // Obligation 5: Overdue 100 days ago (-100 days), unpaid -> 90+ Days, overdue
        var obl5Id = await InsertObligationRawAsync(connection, accountId, studentId, null, policyId, 5000000, dueDayOffset: -100);

        // Obligation 6: Overdue 20 days ago (-20 days), fully settled 1,500,000 -> Current, settled
        var obl6Id = await InsertObligationRawAsync(connection, accountId, studentId, null, policyId, 1500000, dueDayOffset: -20);
        await InsertAllocationRawAsync(connection, accountId, obl6Id, paymentId, 1500000, "active");

        // Obligation 7: Overdue 25 days ago (-25 days), superseded 1,000,000 + active 300,000 -> 1-30 Days, overdue (outstanding 700,000)
        var obl7Id = await InsertObligationRawAsync(connection, accountId, studentId, null, policyId, 1000000, dueDayOffset: -25);
        var supersededAllocId = await InsertAllocationRawAsync(connection, accountId, obl7Id, paymentId, 1000000, "superseded");
        var activeAllocId = await InsertAllocationRawAsync(connection, accountId, obl7Id, paymentId, 300000, "active");
        await connection.ExecuteAsync(
            "UPDATE finance.payment_allocations SET superseded_by_id = @SupersededById, reversal_reason = 'Correction' WHERE id = @Id;",
            new { SupersededById = activeAllocId, Id = supersededAllocId });

        // Obligation 8: Cancelled obligation -> should be filtered out from v_student_receivables
        var obl8Id = await InsertObligationRawAsync(connection, accountId, studentId, null, policyId, 2500000, dueDayOffset: -30, status: "cancelled");

        // Query view for this student account
        const string queryViewSql = """
            SELECT 
                obligation_id AS ObligationId,
                obligation_number AS ObligationNumber,
                student_account_id AS StudentAccountId,
                student_id AS StudentId,
                applicant_id AS ApplicantId,
                charge_type AS ChargeType,
                description AS Description,
                is_mandatory AS IsMandatory,
                due_date AS DueDate,
                original_amount AS OriginalAmount,
                currency_code AS CurrencyCode,
                academic_period_id AS AcademicPeriodId,
                academic_period_label AS AcademicPeriodLabel,
                obligation_status AS ObligationStatus,
                net_obligation AS NetObligation,
                total_paid AS TotalPaid,
                outstanding_amount AS OutstandingAmount,
                is_overdue AS IsOverdue,
                days_overdue AS DaysOverdue,
                aging_bracket AS AgingBracket,
                receivable_status AS ReceivableStatus
            FROM finance.v_student_receivables
            WHERE student_account_id = @AccountId;
            """;

        var viewRows = (await connection.QueryAsync<ReceivableViewRow>(queryViewSql, new { AccountId = accountId })).ToList();

        // 1. Future obligation (+15 days)
        var row1 = viewRows.Single(r => r.ObligationId == obl1Id);
        row1.OriginalAmount.Should().Be(1000000);
        row1.NetObligation.Should().Be(1000000);
        row1.TotalPaid.Should().Be(0);
        row1.OutstandingAmount.Should().Be(1000000);
        row1.IsOverdue.Should().Be(0);
        row1.DaysOverdue.Should().Be(0);
        row1.AgingBracket.Should().Be("Current");
        row1.ReceivableStatus.Should().Be("current");

        // 2. Overdue 10 days, partial paid
        var row2 = viewRows.Single(r => r.ObligationId == obl2Id);
        row2.OriginalAmount.Should().Be(2000000);
        row2.NetObligation.Should().Be(2000000);
        row2.TotalPaid.Should().Be(500000);
        row2.OutstandingAmount.Should().Be(1500000);
        row2.IsOverdue.Should().Be(1);
        row2.DaysOverdue.Should().Be(10);
        row2.AgingBracket.Should().Be("1-30 Days");
        row2.ReceivableStatus.Should().Be("overdue");

        // 3. Overdue 45 days
        var row3 = viewRows.Single(r => r.ObligationId == obl3Id);
        row3.OriginalAmount.Should().Be(3000000);
        row3.TotalPaid.Should().Be(0);
        row3.OutstandingAmount.Should().Be(3000000);
        row3.IsOverdue.Should().Be(1);
        row3.DaysOverdue.Should().Be(45);
        row3.AgingBracket.Should().Be("31-60 Days");
        row3.ReceivableStatus.Should().Be("overdue");

        // 4. Overdue 75 days
        var row4 = viewRows.Single(r => r.ObligationId == obl4Id);
        row4.OriginalAmount.Should().Be(4000000);
        row4.TotalPaid.Should().Be(0);
        row4.OutstandingAmount.Should().Be(4000000);
        row4.IsOverdue.Should().Be(1);
        row4.DaysOverdue.Should().Be(75);
        row4.AgingBracket.Should().Be("61-90 Days");
        row4.ReceivableStatus.Should().Be("overdue");

        // 5. Overdue 100 days
        var row5 = viewRows.Single(r => r.ObligationId == obl5Id);
        row5.OriginalAmount.Should().Be(5000000);
        row5.TotalPaid.Should().Be(0);
        row5.OutstandingAmount.Should().Be(5000000);
        row5.IsOverdue.Should().Be(1);
        row5.DaysOverdue.Should().Be(100);
        row5.AgingBracket.Should().Be("90+ Days");
        row5.ReceivableStatus.Should().Be("overdue");

        // 6. Overdue 20 days but fully settled -> Outstanding 0, Current, settled
        var row6 = viewRows.Single(r => r.ObligationId == obl6Id);
        row6.OriginalAmount.Should().Be(1500000);
        row6.TotalPaid.Should().Be(1500000);
        row6.OutstandingAmount.Should().Be(0);
        row6.IsOverdue.Should().Be(0);
        row6.DaysOverdue.Should().Be(0);
        row6.AgingBracket.Should().Be("Current");
        row6.ReceivableStatus.Should().Be("settled");

        // 7. Obligation with superseded allocation: superseded ignored, only active 300,000 counted
        var row7 = viewRows.Single(r => r.ObligationId == obl7Id);
        row7.OriginalAmount.Should().Be(1000000);
        row7.TotalPaid.Should().Be(300000, "superseded allocations must be excluded from total_paid");
        row7.OutstandingAmount.Should().Be(700000);
        row7.IsOverdue.Should().Be(1);
        row7.DaysOverdue.Should().Be(25);
        row7.AgingBracket.Should().Be("1-30 Days");
        row7.ReceivableStatus.Should().Be("overdue");

        // 8. Cancelled obligation must be excluded from view
        viewRows.Should().NotContain(r => r.ObligationId == obl8Id, "cancelled obligations must be excluded from v_student_receivables");
    }

    #region Helpers

    private static async Task<Guid> InsertStudentAccountRawAsync(
        SqlConnection connection,
        string suffix,
        Guid? studentId,
        Guid? applicantId)
    {
        var id = Guid.NewGuid();
        const string sql = """
            INSERT INTO finance.student_financial_accounts (
                id, account_number, student_id, applicant_id, currency_code, account_status, opened_date, created_at, created_by
            ) VALUES (
                @Id, @AccountNumber, @StudentId, @ApplicantId, 'IDR', 'active', '2026-08-01', SYSUTCDATETIME(), 'MIGRATION_TEST'
            );
            """;

        await connection.ExecuteAsync(sql, new
        {
            Id = id,
            AccountNumber = $"ACC-2026-{suffix}",
            StudentId = studentId,
            ApplicantId = applicantId
        });

        return id;
    }

    private static async Task<Guid> InsertPayerRawAsync(
        SqlConnection connection,
        string payerNumber,
        string payerName,
        string? idNumber = null)
    {
        var id = Guid.NewGuid();
        const string sql = """
            INSERT INTO finance.payers (
                id, payer_number, payer_name, payer_type, identification_number,
                email, phone_number, created_at, created_by
            ) VALUES (
                @Id, @PayerNumber, @PayerName, 'parent_guardian', @IdNumber,
                'payer@test.local', '+628123456789', SYSUTCDATETIME(), 'MIGRATION_TEST'
            );
            """;

        await connection.ExecuteAsync(sql, new
        {
            Id = id,
            PayerNumber = payerNumber,
            PayerName = payerName,
            IdNumber = idNumber
        });

        return id;
    }

    private static async Task<Guid> InsertPaymentRawAsync(
        SqlConnection connection,
        Guid accountId,
        Guid? studentId,
        Guid? applicantId,
        Guid payerId,
        long amount,
        string? receiptNumber,
        string status = "confirmed")
    {
        var id = Guid.NewGuid();
        var paymentNumber = $"PAY-2026-{Guid.NewGuid().ToString("N")[..8]}";

        const string sql = """
            INSERT INTO finance.payments (
                id, payment_number, student_account_id, student_id, applicant_id, payer_id,
                payment_amount, currency_code, payment_method, payment_date, submission_date,
                confirmation_status, receipt_number, created_at, created_by
            ) VALUES (
                @Id, @PaymentNumber, @AccountId, @StudentId, @ApplicantId, @PayerId,
                @Amount, 'IDR', 'bank_transfer', CAST(SYSUTCDATETIME() AS DATE), SYSUTCDATETIME(),
                @Status, @ReceiptNumber, SYSUTCDATETIME(), 'MIGRATION_TEST'
            );
            """;

        await connection.ExecuteAsync(sql, new
        {
            Id = id,
            PaymentNumber = paymentNumber,
            AccountId = accountId,
            StudentId = studentId,
            ApplicantId = applicantId,
            PayerId = payerId,
            Amount = amount,
            Status = status,
            ReceiptNumber = receiptNumber
        });

        return id;
    }

    private static async Task<Guid> InsertObligationRawAsync(
        SqlConnection connection,
        Guid accountId,
        Guid? studentId,
        Guid? applicantId,
        Guid policyId,
        long amount,
        int dueDayOffset,
        string status = "assessed")
    {
        var id = Guid.NewGuid();
        var obligationNumber = $"OBL-2026-{Guid.NewGuid().ToString("N")[..8]}";

        const string sql = """
            INSERT INTO finance.financial_obligations (
                id, obligation_number, student_account_id, student_id, applicant_id,
                charge_rule_id, policy_id, description, charge_type, is_mandatory,
                original_amount, currency_code, issue_date, due_date,
                academic_period_id, academic_period_label, status, authorized_by, created_at, created_by
            ) VALUES (
                @Id, @ObligationNumber, @AccountId, @StudentId, @ApplicantId,
                NULL, @PolicyId, 'Test Obligation', 'tuition', 1,
                @Amount, 'IDR', CAST(SYSUTCDATETIME() AS DATE), DATEADD(DAY, @DueDayOffset, CAST(SYSUTCDATETIME() AS DATE)),
                NEWID(), '2026/2027 Odd Semester', @Status, 'TEST_FINANCE', SYSUTCDATETIME(), 'MIGRATION_TEST'
            );
            """;

        await connection.ExecuteAsync(sql, new
        {
            Id = id,
            ObligationNumber = obligationNumber,
            AccountId = accountId,
            StudentId = studentId,
            ApplicantId = applicantId,
            PolicyId = policyId,
            Amount = amount,
            DueDayOffset = dueDayOffset,
            Status = status
        });

        return id;
    }

    private static async Task<Guid> InsertAllocationRawAsync(
        SqlConnection connection,
        Guid accountId,
        Guid obligationId,
        Guid paymentId,
        long amount,
        string status = "active",
        Guid? supersededById = null)
    {
        var id = Guid.NewGuid();
        var allocationNumber = $"ALC-2026-{Guid.NewGuid().ToString("N")[..8]}";

        const string sql = """
            INSERT INTO finance.payment_allocations (
                id, allocation_number, student_account_id, obligation_id, source_type,
                payment_id, allocated_amount, currency_code, allocation_date, allocated_by,
                status, superseded_by_id, created_at, created_by
            ) VALUES (
                @Id, @AllocationNumber, @AccountId, @ObligationId, 'payment',
                @PaymentId, @Amount, 'IDR', SYSUTCDATETIME(), 'TEST_CASHIER',
                @Status, @SupersededById, SYSUTCDATETIME(), 'MIGRATION_TEST'
            );
            """;

        await connection.ExecuteAsync(sql, new
        {
            Id = id,
            AllocationNumber = allocationNumber,
            AccountId = accountId,
            ObligationId = obligationId,
            PaymentId = paymentId,
            Amount = amount,
            Status = status,
            SupersededById = supersededById
        });

        return id;
    }

    private sealed record ColumnInfo(bool IsNullable, string TypeName);
    private sealed record ForeignKeyInfo(string Name, byte DeleteReferentialAction);
    private sealed record IndexInfo(string Name, bool IsUnique, bool HasFilter);
    private sealed record AccountRow(Guid Id, Guid? StudentId, Guid? ApplicantId);
    private sealed record ObligationRow(Guid Id, Guid? StudentId, Guid? ApplicantId);
    private sealed record AllocationRow(Guid Id, Guid? SupersededById, string? ReversalReason);

    private sealed record ReceivableViewRow(
        Guid ObligationId,
        string ObligationNumber,
        Guid StudentAccountId,
        Guid? StudentId,
        Guid? ApplicantId,
        string ChargeType,
        string Description,
        bool IsMandatory,
        DateTime DueDate,
        long OriginalAmount,
        string CurrencyCode,
        Guid AcademicPeriodId,
        string AcademicPeriodLabel,
        string ObligationStatus,
        long NetObligation,
        long TotalPaid,
        long OutstandingAmount,
        int IsOverdue,
        int DaysOverdue,
        string AgingBracket,
        string ReceivableStatus);

    #endregion
}
