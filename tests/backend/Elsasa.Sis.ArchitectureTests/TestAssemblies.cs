using System.Reflection;
using NetArchTest.Rules;

namespace Elsasa.Sis.ArchitectureTests;

/// <summary>
/// Central access point for the SIS assemblies under test.
/// </summary>
/// <remarks>
/// The assemblies are obtained through the assembly marker types declared by
/// each project. This keeps the test project's project references meaningful
/// (they are compile-time verified) and guarantees that the exact assemblies
/// produced by the solution are inspected, including assemblies that currently
/// contain only their marker type. The rules therefore stay valid while the
/// domain assemblies are still empty.
/// </remarks>
internal static class TestAssemblies
{
    internal const string Shared = "Elsasa.Sis.Shared";
    internal const string Contracts = "Elsasa.Sis.Contracts";
    internal const string Database = "Elsasa.Sis.Database";
    internal const string Operations = "Elsasa.Sis.Operations";
    internal const string Students = "Elsasa.Sis.Students";
    internal const string Finance = "Elsasa.Sis.Finance";
    internal const string Api = "Elsasa.Sis.Api";

    /// <summary>
    /// Stage 1 domain assemblies (ADR-001, ADR-010). They may reference only
    /// <c>Shared</c> and <c>Contracts</c> and must never reference each other.
    /// </summary>
    internal static readonly string[] DomainAssemblyNames = [Operations, Students, Finance];

    /// <summary>
    /// Infrastructure assemblies that domain assemblies must never reference.
    /// </summary>
    internal static readonly string[] InfrastructureAssemblyNames = [Api, Database];

    /// <summary>
    /// Every SIS assembly that is not <c>Shared</c>; used to prove that the
    /// shared kernel is the root of the dependency graph.
    /// </summary>
    internal static readonly string[] OtherSisAssemblyNames =
        [Api, Contracts, Database, Finance, Operations, Students];

    private static readonly Dictionary<string, Assembly> _assembliesByName = new(StringComparer.Ordinal)
    {
        [Shared] = typeof(Elsasa.Sis.Shared.SharedAssemblyMarker).Assembly,
        [Contracts] = typeof(Elsasa.Sis.Contracts.ContractsAssemblyMarker).Assembly,
        [Database] = typeof(Elsasa.Sis.Database.DatabaseAssemblyMarker).Assembly,
        [Operations] = typeof(Elsasa.Sis.Operations.OperationsAssemblyMarker).Assembly,
        [Students] = typeof(Elsasa.Sis.Students.StudentsAssemblyMarker).Assembly,
        [Finance] = typeof(Elsasa.Sis.Finance.FinanceAssemblyMarker).Assembly,
        [Api] = typeof(Program).Assembly,
    };

    /// <summary>
    /// Resolves an assembly by its simple name.
    /// </summary>
    internal static Assembly Get(string assemblyName) => _assembliesByName[assemblyName];

    /// <summary>
    /// Returns the compile-time references recorded in the assembly metadata.
    /// </summary>
    internal static IReadOnlyList<AssemblyName> References(Assembly assembly) => assembly.GetReferencedAssemblies();

    /// <summary>
    /// Returns the SIS project references recorded in the assembly metadata.
    /// </summary>
    internal static IReadOnlyList<string> SisReferences(Assembly assembly) =>
        References(assembly)
            .Select(reference => reference.Name ?? string.Empty)
            .Where(name => name.StartsWith("Elsasa.Sis.", StringComparison.Ordinal))
            .ToArray();

    /// <summary>
    /// Returns every reference whose simple name is in
    /// <paramref name="forbiddenAssemblyNames"/>. A non-empty result is an
    /// ADR-001 violation.
    /// </summary>
    internal static IReadOnlyList<string> FindForbiddenReferences(
        IEnumerable<AssemblyName> references,
        params string[] forbiddenAssemblyNames) =>
        references
            .Select(reference => reference.Name ?? string.Empty)
            .Where(name => Array.IndexOf(forbiddenAssemblyNames, name) >= 0)
            .ToArray();

    /// <summary>
    /// Asserts that no type in <paramref name="assembly"/> has a type-level
    /// dependency on any of the forbidden assemblies.
    /// </summary>
    /// <remarks>
    /// NetArchTest inspects type-level dependencies (using Mono.Cecil), which
    /// complements the assembly-reference assertions: an assembly can only
    /// contain such a dependency when the project reference exists, but the
    /// combined check reports both the metadata rule and the type-level rule.
    /// The rule is skipped when the assembly exposes no types, because
    /// NetArchTest cannot evaluate an empty type set; the assembly-reference
    /// assertion in the calling test remains authoritative.
    /// </remarks>
    internal static void AssertNoTypeLevelDependency(Assembly assembly, params string[] forbiddenAssemblyNames)
    {
        var types = Types.InAssembly(assembly).GetTypes().ToArray();
        if (types.Length == 0 || forbiddenAssemblyNames.Length == 0)
        {
            return;
        }

        var result = Types.InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOnAny(forbiddenAssemblyNames)
            .GetResult();

        Assert.True(
            result.IsSuccessful,
            $"{assembly.GetName().Name} declares a forbidden type-level dependency on one of " +
            $"[{string.Join(", ", forbiddenAssemblyNames)}]. Failing types: {DescribeFailures(result)}.");
    }

    private static string DescribeFailures(TestResult result)
    {
        var failingTypeNames = result.FailingTypeNames?.ToArray() ?? [];
        return failingTypeNames.Length == 0 ? "(none reported)" : string.Join(", ", failingTypeNames);
    }
}
