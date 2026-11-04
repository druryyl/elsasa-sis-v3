namespace Elsasa.Sis.IntegrationTests.Features.Finance;

using Dapper;
using Elsasa.Sis.Contracts.Students.Events;
using Elsasa.Sis.Finance.Services;
using Elsasa.Sis.IntegrationTests.Fixtures;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

/// <summary>
/// Integration test suite verifying the Ledger-Backed Enrollment Payment Gate and
/// candidate account graduation upon official matriculation (P4-S12, GAP-001, GAP-006, OQ-001, OQ-005, TD-02, TD-06).
/// </summary>
public sealed class EnrollmentFeeGateIntegrationTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private sealed record CandidateReferenceDbRow(Guid Id, Guid? StudentId, Guid? ApplicantId);

    [SkippableFact]
    public async Task IsEnrollmentFeePaidAsync_Should_ReturnTrue_WhenNoMandatoryEnrollmentFeesExist()
    {
        SkipIfDatabaseUnavailable();

        await using var connection = await OpenConnectionAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var applicantId = await FinanceIntegrationTestHelpers.SeedApplicantAsync(connection, suffix);
        var (ayId, _) = await FinanceIntegrationTestHelpers.EnsureAcademicYearAndPeriodAsync(connection, suffix);

        // Authoritative ledger gate module
        var financeModule = new FinanceModule(ConnectionFactory);

        var result = await financeModule.IsEnrollmentFeePaidAsync(applicantId, ayId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue("candidates with zero mandatory enrollment obligations must not be blocked by the enrollment gate");
    }

    [SkippableFact]
    public async Task IsEnrollmentFeePaidAsync_Should_ReturnFalse_WhenUnsettledMandatoryEnrollmentFeeExists()
    {
        SkipIfDatabaseUnavailable();

        await using var connection = await OpenConnectionAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var applicantId = await FinanceIntegrationTestHelpers.SeedApplicantAsync(connection, suffix);
        var (ayId, periodId) = await FinanceIntegrationTestHelpers.EnsureAcademicYearAndPeriodAsync(connection, suffix);
        var policyId = await FinanceIntegrationTestHelpers.EnsureActivePolicyAsync(connection);
        var accountId = await FinanceIntegrationTestHelpers.SeedCandidateAccountAsync(connection, suffix, studentId: null, applicantId: applicantId);

        // Seed mandatory enrollment fee of 5,000,000 IDR (unsettled)
        await FinanceIntegrationTestHelpers.SeedObligationAsync(
            connection,
            accountId: accountId,
            studentId: null,
            applicantId: applicantId,
            policyId: policyId,
            chargeType: "enrollment_fee",
            description: "Uang Pangkal Masuk Siswa Baru",
            amount: 5_000_000,
            dueDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)),
            academicPeriodId: periodId,
            isMandatory: true,
            status: "assessed");

        var financeModule = new FinanceModule(ConnectionFactory);

        var result = await financeModule.IsEnrollmentFeePaidAsync(applicantId, ayId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse("unsettled mandatory enrollment fee obligation must evaluate to false at the enrollment gate");
    }

    [SkippableFact]
    public async Task IsEnrollmentFeePaidAsync_Should_ReturnFalse_WhenMandatoryEnrollmentFeeIsOnlyPartiallyPaid()
    {
        SkipIfDatabaseUnavailable();

        await using var connection = await OpenConnectionAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var applicantId = await FinanceIntegrationTestHelpers.SeedApplicantAsync(connection, suffix);
        var (ayId, periodId) = await FinanceIntegrationTestHelpers.EnsureAcademicYearAndPeriodAsync(connection, suffix);
        var policyId = await FinanceIntegrationTestHelpers.EnsureActivePolicyAsync(connection);
        var accountId = await FinanceIntegrationTestHelpers.SeedCandidateAccountAsync(connection, suffix, studentId: null, applicantId: applicantId);
        var payerId = await FinanceIntegrationTestHelpers.SeedPayerAsync(connection, $"PYR-2026-{suffix[..4]}", "Payer Gate Test");

        // Obligation: 5,000,000 IDR
        var oblId = await FinanceIntegrationTestHelpers.SeedObligationAsync(
            connection,
            accountId: accountId,
            studentId: null,
            applicantId: applicantId,
            policyId: policyId,
            chargeType: "admission_fee",
            description: "Biaya Penerimaan Siswa Baru",
            amount: 5_000_000,
            dueDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)),
            academicPeriodId: periodId,
            isMandatory: true,
            status: "assessed");

        // Partial payment and allocation of only 2,000,000 IDR
        var paymentId = await FinanceIntegrationTestHelpers.SeedPaymentAsync(
            connection, accountId, null, applicantId, payerId, 2_000_000, "bank_transfer", "confirmed", $"REC-2026-{suffix[..6]}");

        await FinanceIntegrationTestHelpers.SeedAllocationAsync(connection, accountId, oblId, paymentId, 2_000_000);

        var financeModule = new FinanceModule(ConnectionFactory);

        var result = await financeModule.IsEnrollmentFeePaidAsync(applicantId, ayId);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse("partially paid mandatory enrollment obligations must not unblock the gate until 100% satisfied");
    }

    [SkippableFact]
    public async Task IsEnrollmentFeePaidAsync_Should_ReturnTrue_ImmediatelyAfterPaymentIsAllocatedToMandatoryEnrollmentObligation()
    {
        SkipIfDatabaseUnavailable();

        await using var connection = await OpenConnectionAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var applicantId = await FinanceIntegrationTestHelpers.SeedApplicantAsync(connection, suffix);
        var (ayId, periodId) = await FinanceIntegrationTestHelpers.EnsureAcademicYearAndPeriodAsync(connection, suffix);
        var policyId = await FinanceIntegrationTestHelpers.EnsureActivePolicyAsync(connection);
        var accountId = await FinanceIntegrationTestHelpers.SeedCandidateAccountAsync(connection, suffix, studentId: null, applicantId: applicantId);
        var payerId = await FinanceIntegrationTestHelpers.SeedPayerAsync(connection, $"PYR-2026-{suffix[..4]}", "Payer Full Gate Test");

        var oblId = await FinanceIntegrationTestHelpers.SeedObligationAsync(
            connection,
            accountId: accountId,
            studentId: null,
            applicantId: applicantId,
            policyId: policyId,
            chargeType: "enrollment_fee",
            description: "Uang Pangkal Masuk Siswa Baru",
            amount: 3_500_000,
            dueDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(14)),
            academicPeriodId: periodId,
            isMandatory: true,
            status: "assessed");

        // Initial check: must be false
        var financeModule = new FinanceModule(ConnectionFactory);
        var checkBefore = await financeModule.IsEnrollmentFeePaidAsync(applicantId, ayId);
        checkBefore.Value.Should().BeFalse();

        // Cashier records & confirms payment of 3,500,000 IDR and allocates 100% to obligation
        var paymentId = await FinanceIntegrationTestHelpers.SeedPaymentAsync(
            connection, accountId, null, applicantId, payerId, 3_500_000, "bank_transfer", "confirmed", $"REC-2026-{suffix[..6]}");

        await FinanceIntegrationTestHelpers.SeedAllocationAsync(connection, accountId, oblId, paymentId, 3_500_000);

        // Immediate subsequent inquiry against real ledger: must be true
        var checkAfter = await financeModule.IsEnrollmentFeePaidAsync(applicantId, ayId);
        checkAfter.IsSuccess.Should().BeTrue();
        checkAfter.Value.Should().BeTrue("allocating 100% of mandatory enrollment obligation amount must immediately clear the gate");
    }

    [SkippableFact]
    public async Task StudentEnrolledEventHandler_Should_GraduatePreEnrollmentAccount_Obligations_AndPayments()
    {
        SkipIfDatabaseUnavailable();

        await using var connection = await OpenConnectionAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var applicantId = await FinanceIntegrationTestHelpers.SeedApplicantAsync(connection, suffix);
        var (ayId, periodId) = await FinanceIntegrationTestHelpers.EnsureAcademicYearAndPeriodAsync(connection, suffix);
        var policyId = await FinanceIntegrationTestHelpers.EnsureActivePolicyAsync(connection);
        var payerId = await FinanceIntegrationTestHelpers.SeedPayerAsync(connection, $"PYR-2026-{suffix[..4]}", "Payer Grad Test");

        // 1. Create pre-enrollment entities tied to applicant_id with student_id = NULL
        var accountId = await FinanceIntegrationTestHelpers.SeedCandidateAccountAsync(
            connection, suffix, studentId: null, applicantId: applicantId);

        var oblId = await FinanceIntegrationTestHelpers.SeedObligationAsync(
            connection,
            accountId: accountId,
            studentId: null,
            applicantId: applicantId,
            policyId: policyId,
            chargeType: "enrollment_fee",
            description: "Uang Pangkal Pre-Enrollment",
            amount: 5_000_000,
            dueDate: DateOnly.FromDateTime(DateTime.UtcNow.AddDays(7)),
            academicPeriodId: periodId,
            isMandatory: true,
            status: "assessed");

        var paymentId = await FinanceIntegrationTestHelpers.SeedPaymentAsync(
            connection,
            accountId: accountId,
            studentId: null,
            applicantId: applicantId,
            payerId: payerId,
            amount: 5_000_000,
            paymentMethod: "bank_transfer",
            status: "confirmed",
            receiptNumber: $"REC-2026-{suffix[..6]}");

        // Verify pre-condition: student_id is NULL
        const string checkPreSql = """
            SELECT id AS Id, student_id AS StudentId, applicant_id AS ApplicantId
            FROM finance.student_financial_accounts
            WHERE id = @AccountId;
            """;
        var preAccount = await QuerySingleOrDefaultAsync<CandidateReferenceDbRow>(checkPreSql, new { AccountId = accountId });
        preAccount.Should().NotBeNull();
        preAccount!.StudentId.Should().BeNull();
        preAccount.ApplicantId.Should().Be(applicantId);

        // 2. Officially matriculate student in students.students table (satisfying FK)
        var studentId = Guid.NewGuid();
        const string insertStudentSql = """
            INSERT INTO students.students (
                id, student_number, applicant_id, full_name, date_of_birth, gender,
                nationality, current_status, enrollment_date, created_at
            ) VALUES (
                @Id, @StudentNumber, @ApplicantId, 'Enrolled Matriculated Student', '2016-04-10', 'Female',
                'Indonesian', 'enrolled', '2026-08-01', SYSUTCDATETIME()
            );
            """;
        await connection.ExecuteAsync(insertStudentSql, new
        {
            Id = studentId,
            StudentNumber = $"STU-{suffix}",
            ApplicantId = applicantId
        });

        // 3. Act: Publish StudentEnrolledEvent
        using var scope = Factory.Services.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();
        var evt = new StudentEnrolledEvent(studentId, applicantId, ayId);
        await publisher.Publish(evt);

        // 4. Verify post-graduation database state
        // Account has graduated: student_id is set, applicant_id preserved
        var postAccount = await QuerySingleOrDefaultAsync<CandidateReferenceDbRow>(checkPreSql, new { AccountId = accountId });
        postAccount.Should().NotBeNull();
        postAccount!.StudentId.Should().Be(studentId, "account student_id must graduate to newly enrolled student ID");
        postAccount.ApplicantId.Should().Be(applicantId, "pre-enrollment applicant_id must be preserved for audit trail");

        // Obligation has graduated
        const string checkOblSql = "SELECT id AS Id, student_id AS StudentId, applicant_id AS ApplicantId FROM finance.financial_obligations WHERE id = @Id;";
        var postObligation = await QuerySingleOrDefaultAsync<CandidateReferenceDbRow>(checkOblSql, new { Id = oblId });
        postObligation.Should().NotBeNull();
        postObligation!.StudentId.Should().Be(studentId, "obligation student_id must graduate to newly enrolled student ID");
        postObligation.ApplicantId.Should().Be(applicantId);

        // Payment has graduated
        const string checkPaySql = "SELECT id AS Id, student_id AS StudentId, applicant_id AS ApplicantId FROM finance.payments WHERE id = @Id;";
        var postPayment = await QuerySingleOrDefaultAsync<CandidateReferenceDbRow>(checkPaySql, new { Id = paymentId });
        postPayment.Should().NotBeNull();
        postPayment!.StudentId.Should().Be(studentId, "payment student_id must graduate to newly enrolled student ID");
        postPayment.ApplicantId.Should().Be(applicantId);
    }

    [SkippableFact]
    public async Task StudentEnrolledEventHandler_WithoutPreEnrollmentAccount_Should_ProvisionNewActiveAccount()
    {
        SkipIfDatabaseUnavailable();

        await using var connection = await OpenConnectionAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var studentId = await FinanceIntegrationTestHelpers.SeedStudentAsync(connection, suffix);
        var (ayId, _) = await FinanceIntegrationTestHelpers.EnsureAcademicYearAndPeriodAsync(connection, suffix);

        // Verify pre-condition: no account exists
        const string checkAccountSql = "SELECT COUNT(1) FROM finance.student_financial_accounts WHERE student_id = @StudentId;";
        var countBefore = await ExecuteScalarAsync<int>(checkAccountSql, new { StudentId = studentId });
        countBefore.Should().Be(0);

        // Act: Publish StudentEnrolledEvent with random/unknown applicant ID
        using var scope = Factory.Services.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();
        var evt = new StudentEnrolledEvent(studentId, Guid.NewGuid(), ayId);
        await publisher.Publish(evt);

        // Verify new active account provisioned
        var countAfter = await ExecuteScalarAsync<int>(checkAccountSql, new { StudentId = studentId });
        countAfter.Should().Be(1, "StudentEnrolledEventHandler must provision active account when no applicant account exists");

        const string selectAccountSql = """
            SELECT id AS Id, student_id AS StudentId, applicant_id AS ApplicantId
            FROM finance.student_financial_accounts
            WHERE student_id = @StudentId;
            """;
        var account = await QuerySingleOrDefaultAsync<CandidateReferenceDbRow>(selectAccountSql, new { StudentId = studentId });
        account.Should().NotBeNull();
        account!.StudentId.Should().Be(studentId);
    }
}
