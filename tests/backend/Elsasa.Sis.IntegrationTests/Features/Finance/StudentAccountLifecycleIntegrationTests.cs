namespace Elsasa.Sis.IntegrationTests.Features.Finance;

using System.Net;
using System.Net.Http.Json;
using Elsasa.Sis.Contracts.Students.Events;
using Elsasa.Sis.Finance.Features.Accounts.GetStudentFinancialAccount;
using Elsasa.Sis.Finance.Features.Accounts.Services;
using Elsasa.Sis.IntegrationTests.Fixtures;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

/// <summary>
/// Integration tests verifying the student financial account lifecycle (P4-S12, P2-S03, GAP-002, TD-03).
/// Verifies that publishing StudentEnrolledEvent triggers StudentEnrolledEventHandler to provision active accounts idempotently.
/// </summary>
public sealed class StudentAccountLifecycleIntegrationTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private sealed record AccountDbRow(
        Guid Id,
        string AccountNumber,
        Guid StudentId,
        string CurrencyCode,
        string AccountStatus,
        DateOnly OpenedDate,
        string CreatedBy);

    [SkippableFact]
    public async Task PublishingStudentEnrolledEvent_Should_ProvisionActiveStudentFinancialAccount()
    {
        SkipIfDatabaseUnavailable();

        await using var connection = await OpenConnectionAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var studentId = await FinanceIntegrationTestHelpers.SeedStudentAsync(connection, suffix);
        var (ayId, _) = await FinanceIntegrationTestHelpers.EnsureAcademicYearAndPeriodAsync(connection, suffix);

        using var scope = Factory.Services.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        var evt = new StudentEnrolledEvent(studentId, Guid.NewGuid(), ayId);
        await publisher.Publish(evt);

        const string selectAccountSql = """
            SELECT id AS Id, account_number AS AccountNumber, student_id AS StudentId,
                   currency_code AS CurrencyCode, account_status AS AccountStatus,
                   opened_date AS OpenedDate, created_by AS CreatedBy
            FROM finance.student_financial_accounts
            WHERE student_id = @StudentId;
            """;

        var account = await QuerySingleOrDefaultAsync<AccountDbRow>(selectAccountSql, new { StudentId = studentId });
        account.Should().NotBeNull();
        account!.StudentId.Should().Be(studentId);
        account.AccountStatus.Should().Be("active");
        account.CurrencyCode.Should().Be("IDR");
        account.CreatedBy.Should().Be("SYSTEM_ENROLLMENT");
        account.OpenedDate.Should().Be(DateOnly.FromDateTime(DateTime.UtcNow));
        account.AccountNumber.Should().StartWith($"ACC-{DateTime.UtcNow:yyyy}-");
        account.AccountNumber.Should().EndWith(studentId.ToString()[..8].ToUpperInvariant());
    }

    [SkippableFact]
    public async Task PublishingStudentEnrolledEvent_MultipleTimes_Should_BeStrictlyIdempotent()
    {
        SkipIfDatabaseUnavailable();

        await using var connection = await OpenConnectionAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var studentId = await FinanceIntegrationTestHelpers.SeedStudentAsync(connection, suffix);
        var (ayId, _) = await FinanceIntegrationTestHelpers.EnsureAcademicYearAndPeriodAsync(connection, suffix);

        using var scope = Factory.Services.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

        var evt = new StudentEnrolledEvent(studentId, Guid.NewGuid(), ayId);

        // Publish multiple times
        await publisher.Publish(evt);
        await publisher.Publish(evt);

        const string countSql = """
            SELECT COUNT(1)
            FROM finance.student_financial_accounts
            WHERE student_id = @StudentId;
            """;

        var count = await ExecuteScalarAsync<int>(countSql, new { StudentId = studentId });
        count.Should().Be(1, "publishing duplicate StudentEnrolledEvent must be strictly idempotent");
    }

    [SkippableFact]
    public async Task StudentAccountService_GetOrCreateAccountAsync_Should_IdempotentlyProvisionOrReturnExistingAccount()
    {
        SkipIfDatabaseUnavailable();

        await using var connection = await OpenConnectionAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var studentId = await FinanceIntegrationTestHelpers.SeedStudentAsync(connection, suffix);

        using var scope = Factory.Services.CreateScope();
        var accountService = scope.ServiceProvider.GetRequiredService<IStudentAccountService>();

        // 1. Initial invocation provisions new account
        var account1 = await accountService.GetOrCreateAccountAsync(studentId, connection);
        account1.Should().NotBeNull();
        account1.StudentId.Should().Be(studentId);
        account1.AccountStatus.Should().Be("active");

        // 2. Subsequent invocation returns existing account without throwing
        var account2 = await accountService.GetOrCreateAccountAsync(studentId, connection);
        account2.Should().NotBeNull();
        account2.Id.Should().Be(account1.Id);
        account2.AccountNumber.Should().Be(account1.AccountNumber);

        const string countSql = """
            SELECT COUNT(1)
            FROM finance.student_financial_accounts
            WHERE student_id = @StudentId;
            """;

        var count = await ExecuteScalarAsync<int>(countSql, new { StudentId = studentId });
        count.Should().Be(1);
    }

    [SkippableFact]
    public async Task GetStudentFinancialAccount_Should_ReturnAccountDossier_ViaEndpoint()
    {
        SkipIfDatabaseUnavailable();

        await using var connection = await OpenConnectionAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var studentId = await FinanceIntegrationTestHelpers.SeedStudentAsync(connection, suffix);
        var (ayId, _) = await FinanceIntegrationTestHelpers.EnsureAcademicYearAndPeriodAsync(connection, suffix);

        using var scope = Factory.Services.CreateScope();
        var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();
        await publisher.Publish(new StudentEnrolledEvent(studentId, Guid.NewGuid(), ayId));

        var response = await Client.GetAsync($"/api/finance/accounts/{studentId}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var dossier = await response.Content.ReadFromJsonAsync<GetStudentFinancialAccountResponse>();
        dossier.Should().NotBeNull();
        dossier!.StudentId.Should().Be(studentId);
        dossier.AccountStatus.Should().Be("active");
        dossier.CurrencyCode.Should().Be("IDR");
        dossier.TotalAssessed.Should().Be(0);
        dossier.TotalSettled.Should().Be(0);
        dossier.CurrentBalance.Should().Be(0);
        dossier.OpenObligations.Should().BeEmpty();
    }
}
