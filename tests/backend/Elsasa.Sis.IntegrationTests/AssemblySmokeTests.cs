namespace Elsasa.Sis.IntegrationTests;

/// <summary>
/// Trivial smoke test that keeps the integration test project valid and
/// discoverable until P6-S11 adds the SQL Server container fixtures and
/// P7-S12 adds the end-to-end vertical slice tests.
/// </summary>
public sealed class AssemblySmokeTests
{
    [Fact]
    public void TestAssemblyIsLoadable()
    {
        Assert.NotNull(typeof(AssemblySmokeTests).Assembly);
    }
}
