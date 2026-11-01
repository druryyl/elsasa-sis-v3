namespace Elsasa.Sis.IntegrationTests.UnitTests.Database;

using Elsasa.Sis.Shared.Database;

/// <summary>
/// Unit tests for <see cref="SqlConnectionFactory"/> argument validation.
/// Connection establishment itself requires a live SQL Server and is covered by
/// the integration harness delivered in P6-S11.
/// </summary>
public sealed class SqlConnectionFactoryTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_Should_ThrowArgumentException_When_ConnectionStringIsNullOrWhitespace(string? connectionString)
    {
        Assert.Throws<ArgumentException>(() => new SqlConnectionFactory(connectionString!));
    }
}
