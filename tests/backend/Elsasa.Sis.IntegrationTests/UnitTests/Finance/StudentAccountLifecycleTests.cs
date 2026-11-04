namespace Elsasa.Sis.IntegrationTests.UnitTests.Finance;

using Elsasa.Sis.Contracts.Students.Events;
using Elsasa.Sis.Finance;
using Elsasa.Sis.Finance.Features.Accounts.Events;
using Elsasa.Sis.Finance.Features.Accounts.Services;
using Elsasa.Sis.Shared.Database;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

public sealed class StudentAccountLifecycleTests
{
    [Fact]
    public void GenerateAccountNumber_Should_FollowDeterministicFormat()
    {
        // Arrange
        var studentId = Guid.Parse("12345678-abcd-ef01-2345-6789abcdef01");
        var expectedPrefix = $"ACC-{DateTime.UtcNow:yyyy}-12345678";

        // Act
        var accountNumber = StudentAccountService.GenerateAccountNumber(studentId);

        // Assert
        Assert.Equal(expectedPrefix, accountNumber);
    }

    [Fact]
    public void StudentFinancialAccountSummary_Should_SupportImplicitGuidConversion()
    {
        // Arrange
        var accountId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var summary = new StudentFinancialAccountSummary(
            accountId,
            "ACC-2026-TEST1234",
            studentId,
            "active",
            new DateOnly(2026, 11, 4));

        // Act
        Guid convertedGuid = summary;

        // Assert
        Assert.Equal(accountId, convertedGuid);
        Assert.Equal(studentId, summary.StudentId);
        Assert.Equal("ACC-2026-TEST1234", summary.AccountNumber);
        Assert.Equal("active", summary.AccountStatus);
        Assert.Equal(new DateOnly(2026, 11, 4), summary.OpenedDate);
    }

    [Fact]
    public void StudentEnrolledEventHandler_Should_ImplementINotificationHandlerOfStudentEnrolledEvent()
    {
        // Assert
        Assert.True(typeof(INotificationHandler<StudentEnrolledEvent>).IsAssignableFrom(typeof(StudentEnrolledEventHandler)));
    }

    [Fact]
    public void AddFinanceDomain_Should_RegisterStudentAccountServicesAndEventHandler()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ISqlConnectionFactory>(new SqlConnectionFactory("Server=localhost;Database=test;Trusted_Connection=True;TrustServerCertificate=True;"));

        // Act
        services.AddFinanceDomain();
        var provider = services.BuildServiceProvider();

        // Assert
        var accountService = provider.GetService<IStudentAccountService>();
        Assert.NotNull(accountService);
        Assert.IsType<StudentAccountService>(accountService);

        var eventHandler = provider.GetService<INotificationHandler<StudentEnrolledEvent>>();
        Assert.NotNull(eventHandler);
        Assert.IsType<StudentEnrolledEventHandler>(eventHandler);
    }
}
