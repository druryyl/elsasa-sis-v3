namespace Elsasa.Sis.Finance.Features.Policies.GetPolicyCategories;

/// <summary>
/// Read-only DTO representing an active financial policy category from the master reference table.
/// </summary>
/// <param name="Code">Unique canonical category code (natural key).</param>
/// <param name="Name">Human-readable category display name.</param>
/// <param name="Description">Optional description explaining the policy category scope.</param>
/// <param name="DisplayOrder">Deterministic sort order for UI filter and dropdown presentation.</param>
/// <param name="IsActive">Indicates whether the category is currently active for selection.</param>
public sealed record PolicyCategoryDto(
    string Code,
    string Name,
    string? Description,
    int DisplayOrder,
    bool IsActive);
