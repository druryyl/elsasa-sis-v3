namespace Elsasa.Sis.ArchitectureTests;

/// <summary>
/// Trivial smoke test that keeps the architecture test project valid and
/// discoverable until P6-S10 adds the NetArchTest isolation rules.
/// </summary>
public sealed class AssemblySmokeTests
{
    [Fact]
    public void TestAssemblyIsLoadable()
    {
        Assert.NotNull(typeof(AssemblySmokeTests).Assembly);
    }
}
