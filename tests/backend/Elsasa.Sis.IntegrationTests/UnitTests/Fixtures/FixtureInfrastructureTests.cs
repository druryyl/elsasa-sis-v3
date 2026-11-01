namespace Elsasa.Sis.IntegrationTests.UnitTests.Fixtures;

using Elsasa.Sis.IntegrationTests.Fixtures;
using FluentAssertions;
using Xunit;

public sealed class FixtureInfrastructureTests
{
    [Fact]
    public void SqlServerContainerFixture_Should_ExposeExpectedConstants_When_Inspected()
    {
        // Assert
        SqlServerContainerFixture.TestConnectionStringEnvVar.Should().Be("TEST_SQLSERVER_CONNECTION_STRING");
        SqlServerContainerFixture.SqlServerImage.Should().Be("mcr.microsoft.com/mssql/server:2022-latest");
    }

    [Fact]
    public async Task SqlServerContainerFixture_Should_InitializeWithoutUnhandledCrash_When_DockerUnavailable()
    {
        // Arrange
        var fixture = new SqlServerContainerFixture();

        // Act
        var act = async () => await fixture.InitializeAsync();

        // Assert
        await act.Should().NotThrowAsync();
        if (!fixture.IsAvailable)
        {
            fixture.UnavailableReason.Should().NotBeNullOrWhiteSpace();
            fixture.ConnectionString.Should().BeEmpty();
            fixture.Respawner.Should().BeNull();
        }
    }

    [Fact]
    public async Task IntegrationTestWebAppFactory_Should_InstantiateAndExposeFixtures_When_Constructed()
    {
        // Arrange & Act
        await using var factory = new IntegrationTestWebAppFactory();

        // Assert
        factory.ContainerFixture.Should().NotBeNull();
        factory.CurrentUser.Should().NotBeNull();
        factory.CurrentUser.IsAuthenticated.Should().BeTrue();
    }
}
