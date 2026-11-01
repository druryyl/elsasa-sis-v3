namespace Elsasa.Sis.IntegrationTests.UnitTests.Host;

public sealed class StartupCliTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void RunMigration_Should_ReturnNonZero_When_ConnectionStringIsNullOrWhitespace(string? connectionString)
    {
        // Act
        var exitCode = Program.RunMigration(connectionString);

        // Assert
        Assert.NotEqual(0, exitCode);
        Assert.Equal(1, exitCode);
    }

    [Fact]
    public void RunMigration_Should_ReturnNonZero_When_ConnectionStringIsUnreachable()
    {
        // Arrange: An invalid connection string with an unreachable host and short timeout
        const string unreachableConnStr = "Server=127.0.0.1,59999;Database=NonExistentDb;User Id=sa;Password=Wrong;Connection Timeout=1;TrustServerCertificate=True;";

        // Act
        var exitCode = Program.RunMigration(unreachableConnStr);

        // Assert
        Assert.NotEqual(0, exitCode);
        Assert.Equal(1, exitCode);
    }

    private static readonly string[] _migrateArgsWithNoConnection = ["--migrate", "--ConnectionStrings:DefaultConnection="];

    [Fact]
    public async Task EntryPoint_Should_ExitWithNonZero_When_MigrateArgPassedWithNoConnectionString()
    {
        var entryPoint = typeof(Program).Assembly.EntryPoint;
        Assert.NotNull(entryPoint);

        var result = entryPoint.Invoke(null, [_migrateArgsWithNoConnection]);

        if (result is Task<int> task)
        {
            var exitCode = await task;
            Assert.Equal(1, exitCode);
        }
        else if (result is int exitCode)
        {
            Assert.Equal(1, exitCode);
        }
    }
}
