namespace Elsasa.Sis.IntegrationTests.UnitTests.Students;

using System.Globalization;
using Elsasa.Sis.Shared.Database;
using Elsasa.Sis.Students;
using Elsasa.Sis.Students.Features.Enrollment.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

[Trait("Category", "UnitTest")]
public sealed class StudentNumberGeneratorTests
{
    [Theory]
    [InlineData(2026, 1, "STD-2026-0001")]
    [InlineData(2026, 2, "STD-2026-0002")]
    [InlineData(2026, 42, "STD-2026-0042")]
    [InlineData(2026, 100, "STD-2026-0100")]
    [InlineData(2026, 999, "STD-2026-0999")]
    [InlineData(2026, 1000, "STD-2026-1000")]
    [InlineData(2026, 9999, "STD-2026-9999")]
    [InlineData(2026, 10000, "STD-2026-10000")]
    [InlineData(2025, 1, "STD-2025-0001")]
    [InlineData(2030, 500, "STD-2030-0500")]
    public void FormatStudentNumber_Should_ReturnExpectedFormat(int year, int sequence, string expected)
    {
        // Act
        var result = SqlStudentNumberGenerator.FormatStudentNumber(year, sequence);

        // Assert
        result.Should().Be(expected);
    }

    [Fact]
    public void FormatStudentNumber_Should_BeCultureInvariant_AcrossDifferentCultures()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = new CultureInfo("ar-SA");

            // Act
            var result = SqlStudentNumberGenerator.FormatStudentNumber(2026, 1);

            // Assert - Must remain standard ASCII digits STD-2026-0001
            result.Should().Be("STD-2026-0001");
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
        }
    }

    [Fact]
    public void AddStudentsDomain_Should_RegisterStudentNumberGenerator()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ISqlConnectionFactory>(new DummySqlConnectionFactory());

        // Act
        services.AddStudentsDomain();
        using var provider = services.BuildServiceProvider();

        // Assert
        var generator = provider.GetService<IStudentNumberGenerator>();
        generator.Should().NotBeNull();
        generator.Should().BeOfType<SqlStudentNumberGenerator>();
    }

    private sealed class DummySqlConnectionFactory : ISqlConnectionFactory
    {
        public ValueTask<System.Data.Common.DbConnection> CreateConnectionAsync(CancellationToken cancellationToken = default) =>
            throw new NotImplementedException();
    }
}
