using System.Reflection;

namespace Elsasa.Sis.ArchitectureTests;

/// <summary>
/// Enforces the compile-time dependency rules of ADR-001 and ADR-010.
/// </summary>
/// <remarks>
/// Every rule is verified twice: against the assembly references recorded in
/// the assembly metadata (the authoritative compile-time project reference
/// graph) and against the type-level dependencies detected by NetArchTest. The
/// metadata checks stay meaningful even while an assembly contains only its
/// marker type.
/// </remarks>
public sealed class AssemblyDependencyTests
{
    private static readonly string[] _contractsForbiddenTypeDependencies =
        [TestAssemblies.Api, TestAssemblies.Database, TestAssemblies.Finance, TestAssemblies.Operations, TestAssemblies.Students, TestAssemblies.Academics, TestAssemblies.Teaching];

    [Theory]
    [InlineData(TestAssemblies.Operations, TestAssemblies.Students)]
    [InlineData(TestAssemblies.Operations, TestAssemblies.Finance)]
    [InlineData(TestAssemblies.Operations, TestAssemblies.Academics)]
    [InlineData(TestAssemblies.Operations, TestAssemblies.Teaching)]
    [InlineData(TestAssemblies.Students, TestAssemblies.Operations)]
    [InlineData(TestAssemblies.Students, TestAssemblies.Finance)]
    [InlineData(TestAssemblies.Students, TestAssemblies.Academics)]
    [InlineData(TestAssemblies.Students, TestAssemblies.Teaching)]
    [InlineData(TestAssemblies.Finance, TestAssemblies.Operations)]
    [InlineData(TestAssemblies.Finance, TestAssemblies.Students)]
    [InlineData(TestAssemblies.Finance, TestAssemblies.Academics)]
    [InlineData(TestAssemblies.Finance, TestAssemblies.Teaching)]
    [InlineData(TestAssemblies.Academics, TestAssemblies.Operations)]
    [InlineData(TestAssemblies.Academics, TestAssemblies.Students)]
    [InlineData(TestAssemblies.Academics, TestAssemblies.Finance)]
    [InlineData(TestAssemblies.Academics, TestAssemblies.Teaching)]
    [InlineData(TestAssemblies.Teaching, TestAssemblies.Operations)]
    [InlineData(TestAssemblies.Teaching, TestAssemblies.Students)]
    [InlineData(TestAssemblies.Teaching, TestAssemblies.Finance)]
    [InlineData(TestAssemblies.Teaching, TestAssemblies.Academics)]
    public void DomainAssembly_Should_NotReferenceSiblingDomain_When_AssemblyMetadataIsInspected(
        string assemblyName,
        string forbiddenAssemblyName)
    {
        // Arrange
        var assembly = TestAssemblies.Get(assemblyName);

        // Act
        var references = TestAssemblies.References(assembly);
        var forbiddenReferences = TestAssemblies.FindForbiddenReferences(references, forbiddenAssemblyName);

        // Assert
        Assert.NotEmpty(references);
        Assert.Empty(forbiddenReferences);
        TestAssemblies.AssertNoTypeLevelDependency(assembly, forbiddenAssemblyName);
    }

    [Theory]
    [InlineData(TestAssemblies.Operations, TestAssemblies.Api)]
    [InlineData(TestAssemblies.Operations, TestAssemblies.Database)]
    [InlineData(TestAssemblies.Students, TestAssemblies.Api)]
    [InlineData(TestAssemblies.Students, TestAssemblies.Database)]
    [InlineData(TestAssemblies.Finance, TestAssemblies.Api)]
    [InlineData(TestAssemblies.Finance, TestAssemblies.Database)]
    [InlineData(TestAssemblies.Academics, TestAssemblies.Api)]
    [InlineData(TestAssemblies.Academics, TestAssemblies.Database)]
    [InlineData(TestAssemblies.Teaching, TestAssemblies.Api)]
    [InlineData(TestAssemblies.Teaching, TestAssemblies.Database)]
    public void DomainAssembly_Should_NotReferenceInfrastructureAssembly_When_AssemblyMetadataIsInspected(
        string assemblyName,
        string forbiddenAssemblyName)
    {
        // Arrange
        var assembly = TestAssemblies.Get(assemblyName);

        // Act
        var references = TestAssemblies.References(assembly);
        var forbiddenReferences = TestAssemblies.FindForbiddenReferences(references, forbiddenAssemblyName);

        // Assert
        Assert.NotEmpty(references);
        Assert.Empty(forbiddenReferences);
        TestAssemblies.AssertNoTypeLevelDependency(assembly, forbiddenAssemblyName);
    }

    [Fact]
    public void Academics_Should_ReferenceOnlySharedAndContracts_When_AssemblyMetadataIsInspected()
    {
        // Arrange
        var assembly = TestAssemblies.Get(TestAssemblies.Academics);

        // Act
        var references = TestAssemblies.References(assembly);
        var sisReferences = TestAssemblies.SisReferences(assembly);

        // Assert
        Assert.NotEmpty(references);
        Assert.All(
            sisReferences,
            name => Assert.True(
                string.Equals(name, TestAssemblies.Shared, StringComparison.Ordinal) ||
                string.Equals(name, TestAssemblies.Contracts, StringComparison.Ordinal),
                $"Elsasa.Sis.Academics must reference only Elsasa.Sis.Shared and Elsasa.Sis.Contracts but references '{name}'."));

        TestAssemblies.AssertNoTypeLevelDependency(
            assembly,
            TestAssemblies.Operations,
            TestAssemblies.Students,
            TestAssemblies.Finance,
            TestAssemblies.Teaching,
            TestAssemblies.Api,
            TestAssemblies.Database);
    }

    [Fact]
    public void Teaching_Should_ReferenceOnlySharedAndContracts_When_AssemblyMetadataIsInspected()
    {
        // Arrange
        var assembly = TestAssemblies.Get(TestAssemblies.Teaching);

        // Act
        var references = TestAssemblies.References(assembly);
        var sisReferences = TestAssemblies.SisReferences(assembly);

        // Assert
        Assert.NotEmpty(references);
        Assert.All(
            sisReferences,
            name => Assert.True(
                string.Equals(name, TestAssemblies.Shared, StringComparison.Ordinal) ||
                string.Equals(name, TestAssemblies.Contracts, StringComparison.Ordinal),
                $"Elsasa.Sis.Teaching must reference only Elsasa.Sis.Shared and Elsasa.Sis.Contracts but references '{name}'."));

        TestAssemblies.AssertNoTypeLevelDependency(
            assembly,
            TestAssemblies.Operations,
            TestAssemblies.Students,
            TestAssemblies.Finance,
            TestAssemblies.Academics,
            TestAssemblies.Api,
            TestAssemblies.Database);
    }

    [Fact]
    public void Contracts_Should_ReferenceOnlySharedAndMediatRContracts_When_AssemblyMetadataIsInspected()
    {
        // Arrange
        var assembly = TestAssemblies.Get(TestAssemblies.Contracts);

        // Act
        var references = TestAssemblies.References(assembly);
        var sisReferences = TestAssemblies.SisReferences(assembly);
        var mediatRReferences = references
            .Select(reference => reference.Name ?? string.Empty)
            .Where(name => name.StartsWith("MediatR", StringComparison.Ordinal))
            .ToArray();

        // Assert
        Assert.NotEmpty(references);

        // Elsasa.Sis.Contracts may reference only Elsasa.Sis.Shared among SIS projects.
        // Once types in Contracts consume Shared types (P4-S07), this reference will
        // appear in IL metadata; until then, Assert.All ensures no illegal SIS project
        // reference exists in the assembly manifest.
        Assert.All(
            sisReferences,
            name => Assert.True(
                string.Equals(name, TestAssemblies.Shared, StringComparison.Ordinal),
                $"Elsasa.Sis.Contracts must reference only Elsasa.Sis.Shared but references '{name}'."));

        // MediatR.Contracts is added to Contracts in P4-S07; until then this
        // assertion is vacuous by design and guards against the full MediatR
        // package leaking into the contracts assembly later.
        Assert.All(
            mediatRReferences,
            name => Assert.True(
                string.Equals(name, "MediatR.Contracts", StringComparison.Ordinal),
                $"Elsasa.Sis.Contracts may reference only MediatR.Contracts but references '{name}'."));

        TestAssemblies.AssertNoTypeLevelDependency(assembly, _contractsForbiddenTypeDependencies);
    }

    [Fact]
    public void Shared_Should_NotReferenceAnyOtherSisAssembly_When_AssemblyMetadataIsInspected()
    {
        // Arrange
        var assembly = TestAssemblies.Get(TestAssemblies.Shared);

        // Act
        var references = TestAssemblies.References(assembly);
        var sisReferences = TestAssemblies.SisReferences(assembly);

        // Assert
        Assert.NotEmpty(references);
        Assert.Empty(sisReferences);
        TestAssemblies.AssertNoTypeLevelDependency(assembly, TestAssemblies.OtherSisAssemblyNames);
    }

    [Theory]
    [InlineData(TestAssemblies.Operations)]
    [InlineData(TestAssemblies.Students)]
    [InlineData(TestAssemblies.Finance)]
    [InlineData(TestAssemblies.Academics)]
    [InlineData(TestAssemblies.Teaching)]
    public void Database_Should_NotReferenceDomainAssembly_When_AssemblyMetadataIsInspected(string forbiddenAssemblyName)
    {
        // Arrange
        var assembly = TestAssemblies.Get(TestAssemblies.Database);

        // Act
        var references = TestAssemblies.References(assembly);
        var forbiddenReferences = TestAssemblies.FindForbiddenReferences(references, forbiddenAssemblyName);

        // Assert
        Assert.NotEmpty(references);
        Assert.Empty(forbiddenReferences);
        TestAssemblies.AssertNoTypeLevelDependency(assembly, forbiddenAssemblyName);
    }

    [Fact]
    public void ForbiddenReferenceFinder_Should_ReportViolation_When_ForbiddenAssemblyIsReferenced()
    {
        // Arrange: known-positive sample guarding against a vacuous detector.
        var references = new[]
        {
            new AssemblyName(TestAssemblies.Students),
            new AssemblyName("System.Runtime"),
        };

        // Act
        var violations = TestAssemblies.FindForbiddenReferences(references, TestAssemblies.Students);

        // Assert
        Assert.Single(violations);
        Assert.Equal(TestAssemblies.Students, violations[0]);
    }
}
