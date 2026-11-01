namespace Elsasa.Sis.IntegrationTests.UnitTests.Observability;

using Elsasa.Sis.Api.Configuration;
using Serilog;

public sealed class SerilogConfigurationTests
{
    [Fact]
    public void ConfigureStandardLogging_Should_ThrowArgumentNullException_When_LoggerConfigurationIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => SerilogConfiguration.ConfigureStandardLogging(null!));
    }

    [Fact]
    public void ConfigureSerilog_Should_ThrowArgumentNullException_When_BuilderIsNull()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => SerilogConfiguration.ConfigureSerilog(null!));
    }

    [Fact]
    public void ConfigureStandardLogging_Should_ProduceValidLogger_When_Configured()
    {
        // Arrange
        var config = new LoggerConfiguration();

        // Act
        var result = config.ConfigureStandardLogging();
        using var logger = result.CreateLogger();

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(logger);

        // Verify emitting a log event causes no exception with enrichers and json formatter active
        var exception = Record.Exception(() => logger.Information("Serilog test message {Id}", 42));
        Assert.Null(exception);
    }
}
