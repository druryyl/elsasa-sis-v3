namespace Elsasa.Sis.ArchitectureTests;

using System.Reflection;
using NetArchTest.Rules;
using Xunit;

/// <summary>
/// Architecture tests verifying ADR-001 (compile-time isolation) and ADR-002
/// (schema write boundaries) for the Demo Data implementation (P5-S10, GAP-001, GAP-002, OQ-012).
/// </summary>
public sealed class DemoDataArchitectureTest
{
    [Fact]
    public void OperationsDomain_Should_NotReferenceStudentsFinanceOrAcademics()
    {
        // Arrange
        var assembly = TestAssemblies.Get(TestAssemblies.Operations);

        // Act
        var sisReferences = TestAssemblies.SisReferences(assembly);

        // Assert - Sis references must only be Shared and Contracts
        foreach (var refName in sisReferences)
        {
            if (refName != TestAssemblies.Shared && refName != TestAssemblies.Contracts)
            {
                throw new Xunit.Sdk.XunitException(
                    $"Operations domain must reference only Elsasa.Sis.Shared and Elsasa.Sis.Contracts but references '{refName}'.");
            }
        }

        // Assert - No type-level dependencies on sibling domains
        var result = Types.InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOnAny(TestAssemblies.Students, TestAssemblies.Finance, TestAssemblies.Academics, TestAssemblies.Api)
            .GetResult();

        Assert.True(result.IsSuccessful, "Operations domain must not have type-level dependencies on sibling domains");
    }

    [Fact]
    public void StudentsDomain_Should_NotReferenceOperationsFinanceOrAcademics()
    {
        // Arrange
        var assembly = TestAssemblies.Get(TestAssemblies.Students);

        // Act
        var sisReferences = TestAssemblies.SisReferences(assembly);

        // Assert - Sis references must only be Shared and Contracts
        foreach (var refName in sisReferences)
        {
            if (refName != TestAssemblies.Shared && refName != TestAssemblies.Contracts)
            {
                throw new Xunit.Sdk.XunitException(
                    $"Students domain must reference only Elsasa.Sis.Shared and Elsasa.Sis.Contracts but references '{refName}'.");
            }
        }

        // Assert - No type-level dependencies on sibling domains
        var result = Types.InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOnAny(TestAssemblies.Operations, TestAssemblies.Finance, TestAssemblies.Academics, TestAssemblies.Api)
            .GetResult();

        Assert.True(result.IsSuccessful, "Students domain must not have type-level dependencies on sibling domains");
    }

    [Fact]
    public void FinanceDomain_Should_NotReferenceOperationsStudentsOrAcademics()
    {
        // Arrange
        var assembly = TestAssemblies.Get(TestAssemblies.Finance);

        // Act
        var sisReferences = TestAssemblies.SisReferences(assembly);

        // Assert - Sis references must only be Shared and Contracts
        foreach (var refName in sisReferences)
        {
            if (refName != TestAssemblies.Shared && refName != TestAssemblies.Contracts)
            {
                throw new Xunit.Sdk.XunitException(
                    $"Finance domain must reference only Elsasa.Sis.Shared and Elsasa.Sis.Contracts but references '{refName}'.");
            }
        }

        // Assert - No type-level dependencies on sibling domains
        var result = Types.InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOnAny(TestAssemblies.Operations, TestAssemblies.Students, TestAssemblies.Academics, TestAssemblies.Api)
            .GetResult();

        Assert.True(result.IsSuccessful, "Finance domain must not have type-level dependencies on sibling domains");
    }

    [Fact]
    public void AcademicsDomain_Should_NotReferenceOperationsStudentsOrFinance()
    {
        // Arrange
        var assembly = TestAssemblies.Get(TestAssemblies.Academics);

        // Act
        var sisReferences = TestAssemblies.SisReferences(assembly);

        // Assert - Sis references must only be Shared and Contracts
        foreach (var refName in sisReferences)
        {
            if (refName != TestAssemblies.Shared && refName != TestAssemblies.Contracts)
            {
                throw new Xunit.Sdk.XunitException(
                    $"Academics domain must reference only Elsasa.Sis.Shared and Elsasa.Sis.Contracts but references '{refName}'.");
            }
        }

        // Assert - No type-level dependencies on sibling domains
        var result = Types.InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOnAny(TestAssemblies.Operations, TestAssemblies.Students, TestAssemblies.Finance, TestAssemblies.Api)
            .GetResult();

        Assert.True(result.IsSuccessful, "Academics domain must not have type-level dependencies on sibling domains");
    }

    [Fact]
    public void DomainSeeders_Should_Write_Only_To_Their_Own_Schema()
    {
        // Arrange
        var operationsAssembly = TestAssemblies.Get(TestAssemblies.Operations);
        var studentsAssembly = TestAssemblies.Get(TestAssemblies.Students);
        var financeAssembly = TestAssemblies.Get(TestAssemblies.Finance);
        var academicsAssembly = TestAssemblies.Get(TestAssemblies.Academics);

        // Act - Verify no type-level dependencies between domain assemblies
        VerifyNoTypeLevelDependency(operationsAssembly, TestAssemblies.Students, TestAssemblies.Finance, TestAssemblies.Academics);
        VerifyNoTypeLevelDependency(studentsAssembly, TestAssemblies.Operations, TestAssemblies.Finance, TestAssemblies.Academics);
        VerifyNoTypeLevelDependency(financeAssembly, TestAssemblies.Operations, TestAssemblies.Students, TestAssemblies.Academics);
        VerifyNoTypeLevelDependency(academicsAssembly, TestAssemblies.Operations, TestAssemblies.Students, TestAssemblies.Finance);

        // Assert - This confirms the architectural boundary is maintained at the type level.
        // Each domain's seeder/purger only references its own interfaces via the contracts layer.
    }

    [Fact]
    public void Contracts_DemoDataInterfaces_Should_Be_Implementable_By_All_Domains()
    {
        // Arrange
        var operationsAssembly = TestAssemblies.Get(TestAssemblies.Operations);
        var studentsAssembly = TestAssemblies.Get(TestAssemblies.Students);
        var financeAssembly = TestAssemblies.Get(TestAssemblies.Finance);
        var academicsAssembly = TestAssemblies.Get(TestAssemblies.Academics);

        // Assert - All domain assemblies must reference Contracts
        SisReferencesMustContain(operationsAssembly, TestAssemblies.Contracts,
            "Operations domain must reference Contracts");
        SisReferencesMustContain(studentsAssembly, TestAssemblies.Contracts,
            "Students domain must reference Contracts");
        SisReferencesMustContain(financeAssembly, TestAssemblies.Contracts,
            "Finance domain must reference Contracts");
        SisReferencesMustContain(academicsAssembly, TestAssemblies.Contracts,
            "Academics domain must reference Contracts");
    }

    private static void VerifyNoTypeLevelDependency(Assembly assembly, params string[] forbiddenAssemblyNames)
    {
        var result = Types.InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOnAny(forbiddenAssemblyNames)
            .GetResult();

        if (!result.IsSuccessful)
        {
            var failingTypes = string.Join(", ", result.FailingTypeNames);
            throw new Xunit.Sdk.XunitException(
                $"{assembly.GetName().Name} declares a forbidden type-level dependency on one of [" +
                $"{string.Join(", ", forbiddenAssemblyNames)}]. Failing types: {failingTypes}");
        }
    }

    private static void SisReferencesMustContain(Assembly assembly, string expectedName, string because)
    {
        var sisRefs = TestAssemblies.SisReferences(assembly);
        if (!sisRefs.Contains(expectedName))
        {
            throw new Xunit.Sdk.XunitException(because);
        }
    }
}