namespace Elsasa.Sis.IntegrationTests.Infrastructure;

using System.Net;
using Elsasa.Sis.IntegrationTests.Fixtures;
using FluentAssertions;
using Xunit;

/// <summary>
/// Infrastructure smoke tests verifying SQL Server container booting, DbUp migrations execution,
/// and API host health check probe responsiveness (ADR-008).
/// </summary>
public sealed class SmokeTests(IntegrationTestWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [SkippableFact]
    public async Task HealthEndpoint_Should_ReturnOk_When_DatabaseIsHealthy()
    {
        // Arrange
        SkipIfDatabaseUnavailable();

        // Act
        var response = await Client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Be("Healthy");
    }

    [SkippableFact]
    public async Task DatabaseMigrations_Should_BeApplied_When_ContainerStarts()
    {
        // Arrange
        SkipIfDatabaseUnavailable();

        // Act
        var scriptCount = await ExecuteScalarAsync<int>("SELECT COUNT(1) FROM dbo._SchemaVersions;");
        var academicYearsTableExists = await ExecuteScalarAsync<int>(
            """
            SELECT COUNT(1)
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_SCHEMA = 'operations' AND TABLE_NAME = 'academic_years';
            """);

        // Assert
        scriptCount.Should().BeGreaterOrEqualTo(5);
        academicYearsTableExists.Should().Be(1);
    }

    [Fact]
    public void Environment_Should_ProvideCleanAvailabilityDiagnosis_When_Inspected()
    {
        // Arrange & Act
        var isAvailable = Factory.ContainerFixture.IsAvailable;
        var reason = Factory.ContainerFixture.UnavailableReason;

        // Assert
        if (!isAvailable)
        {
            reason.Should().NotBeNullOrWhiteSpace();
        }
        else
        {
            Factory.ContainerFixture.ConnectionString.Should().NotBeNullOrWhiteSpace();
        }
    }
}
