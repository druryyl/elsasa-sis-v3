namespace Elsasa.Sis.Students.Features.Records.Placements.ChangeAdministrativePlacement;

/// <summary>
/// Response payload returned after successfully reassigning a student's administrative placement (ADR-004, UC-STD-06).
/// </summary>
public sealed record ChangeAdministrativePlacementResponse(
    Guid PlacementId,
    Guid StudentId,
    bool IsCurrent);
