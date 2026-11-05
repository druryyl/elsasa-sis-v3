namespace Elsasa.Sis.Academics.Features.LearningUnits;

using Elsasa.Sis.Shared.Common;

/// <summary>
/// Domain rules governing the 2-level hierarchy invariant for learning units (GAP-004, OQ-002, TD-04).
/// </summary>
public static class LearningUnitHierarchyRules
{
    public const int RootDepth = 0;
    public const int ChildDepth = 1;
    public const int MaxAllowedDepth = 1;

    /// <summary>
    /// Calculates the hierarchy depth for a learning unit based on its parent hierarchy.
    /// Root unit (parent is null) has depth 0.
    /// Child unit (parent is root, meaning parent's parent is null) has depth 1.
    /// If parent already has a parent (depth would be &gt;= 2), returns failure with Academic.HierarchyDepthExceeded.
    /// </summary>
    public static Result<int> CalculateDepth(Guid? parentUnitId, Guid? grandParentUnitId)
    {
        if (!parentUnitId.HasValue)
        {
            return Result.Success(RootDepth);
        }

        if (grandParentUnitId.HasValue)
        {
            return Result.Failure<int>(DomainError.Validation(
                "Academic.HierarchyDepthExceeded",
                "Learning units are constrained to a maximum of 2 hierarchy levels. A child unit cannot have sub-children."));
        }

        return Result.Success(ChildDepth);
    }

    /// <summary>
    /// Validates that a learning unit does not reference itself as parent.
    /// </summary>
    public static Result ValidateSelfParenting(Guid unitId, Guid? parentUnitId)
    {
        if (parentUnitId.HasValue && parentUnitId.Value == unitId)
        {
            return Result.Failure(DomainError.Validation(
                "Academic.SelfParentingNotAllowed",
                "A learning unit cannot be its own parent."));
        }

        return Result.Success();
    }

    /// <summary>
    /// Validates that an existing unit being assigned a parent does not already have children of its own.
    /// </summary>
    public static Result ValidateNoExistingChildrenWhenBecomingChild(int existingChildCount, Guid? newParentUnitId)
    {
        if (newParentUnitId.HasValue && existingChildCount > 0)
        {
            return Result.Failure(DomainError.Validation(
                "Academic.HierarchyDepthExceeded",
                "Learning units are constrained to a maximum of 2 hierarchy levels. A unit with child units cannot be assigned a parent."));
        }

        return Result.Success();
    }
}
