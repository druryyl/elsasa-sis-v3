namespace Elsasa.Sis.IntegrationTests.UnitTests.Academics;

using Elsasa.Sis.Academics.Features.LearningUnits;
using FluentAssertions;
using Xunit;

/// <summary>
/// Unit tests for 2-Level Hierarchy Invariant and Self-Parenting rules (P4-S11, TD-04, GAP-004, OQ-002).
/// Verifies that curriculum units are strictly capped at 2 hierarchy levels (root = 0, child = 1)
/// and self-referential cycles are prohibited.
/// </summary>
[Trait("Category", "UnitTest")]
public sealed class TwoLevelHierarchyUnitTests
{
    [Fact]
    public void CalculateDepth_Should_ReturnDepth0_When_UnitHasNoParent_RootUnit()
    {
        // Arrange: root unit has no parent
        Guid? parentUnitId = null;
        Guid? grandParentUnitId = null;

        // Act
        var result = LearningUnitHierarchyRules.CalculateDepth(parentUnitId, grandParentUnitId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(LearningUnitHierarchyRules.RootDepth);
        result.Value.Should().Be(0);
    }

    [Fact]
    public void CalculateDepth_Should_ReturnDepth1_When_ParentIsRootUnit_DirectChild()
    {
        // Arrange: parent exists, but parent itself has no parent (it is a root unit)
        var rootParentId = Guid.NewGuid();
        Guid? grandParentUnitId = null;

        // Act
        var result = LearningUnitHierarchyRules.CalculateDepth(rootParentId, grandParentUnitId);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(LearningUnitHierarchyRules.ChildDepth);
        result.Value.Should().Be(1);
    }

    [Fact]
    public void CalculateDepth_Should_Fail_When_ParentAlreadyHasParent_ChildOfChildExceedsCap()
    {
        // Arrange: parent exists AND parent itself already has a parent (it is a child unit)
        var childParentId = Guid.NewGuid();
        var grandParentId = Guid.NewGuid();

        // Act
        var result = LearningUnitHierarchyRules.CalculateDepth(childParentId, grandParentId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be("Academic.HierarchyDepthExceeded");
        result.Error.Message.Should().Contain("Learning units are constrained to a maximum of 2 hierarchy levels");
        result.Error.Message.Should().Contain("A child unit cannot have sub-children");
    }

    [Fact]
    public void ValidateSelfParenting_Should_Fail_When_UnitReferencesItselfAsParent()
    {
        // Arrange: unitId and parentUnitId are identical
        var unitId = Guid.NewGuid();
        var parentUnitId = unitId;

        // Act
        var result = LearningUnitHierarchyRules.ValidateSelfParenting(unitId, parentUnitId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be("Academic.SelfParentingNotAllowed");
        result.Error.Message.Should().Contain("A learning unit cannot be its own parent");
    }

    [Fact]
    public void ValidateSelfParenting_Should_Succeed_When_ParentIsDifferentUnit()
    {
        // Arrange: unitId and parentUnitId are different
        var unitId = Guid.NewGuid();
        var parentUnitId = Guid.NewGuid();

        // Act
        var result = LearningUnitHierarchyRules.ValidateSelfParenting(unitId, parentUnitId);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ValidateSelfParenting_Should_Succeed_When_ParentIsNull_RootUnit()
    {
        // Arrange: unitId has no parent
        var unitId = Guid.NewGuid();
        Guid? parentUnitId = null;

        // Act
        var result = LearningUnitHierarchyRules.ValidateSelfParenting(unitId, parentUnitId);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(10)]
    public void ValidateNoExistingChildren_Should_Fail_When_UnitWithChildrenTriesToBecomeChild(int existingChildren)
    {
        // Arrange: existing unit has children and tries to set a parentUnitId
        var newParentId = Guid.NewGuid();

        // Act
        var result = LearningUnitHierarchyRules.ValidateNoExistingChildrenWhenBecomingChild(existingChildren, newParentId);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().NotBeNull();
        result.Error!.Code.Should().Be("Academic.HierarchyDepthExceeded");
        result.Error.Message.Should().Contain("A unit with child units cannot be assigned a parent");
    }

    [Fact]
    public void ValidateNoExistingChildren_Should_Succeed_When_UnitHasNoChildren()
    {
        // Arrange: existing unit has 0 children and sets a parentUnitId
        var newParentId = Guid.NewGuid();

        // Act
        var result = LearningUnitHierarchyRules.ValidateNoExistingChildrenWhenBecomingChild(0, newParentId);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void ValidateNoExistingChildren_Should_Succeed_When_UnitRemainsRootUnit_EvenWithChildren()
    {
        // Arrange: unit has children, but is staying a root unit (newParentUnitId is null)
        Guid? newParentId = null;

        // Act
        var result = LearningUnitHierarchyRules.ValidateNoExistingChildrenWhenBecomingChild(5, newParentId);

        // Assert
        result.IsSuccess.Should().BeTrue();
    }
}
