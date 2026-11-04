namespace Elsasa.Sis.Students.Features.Records.History.GetStudentAdministrativeHistory;

/// <summary>
/// Response payload containing the chronological administrative history for a student (GAP-006, TD-06).
/// </summary>
public sealed record GetStudentAdministrativeHistoryResponse(
    Guid StudentId,
    IReadOnlyList<AdministrativeHistoryEntryDto> Entries);
