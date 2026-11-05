namespace Elsasa.Sis.Academics.Features.Assignments.CreateAssignment;

/// <summary>
/// Request payload for creating a coursework assignment under a learning activity (TD-05, UC-ACD-04, P5-S10).
/// </summary>
public sealed record CreateAssignmentRequest(
    string Title,
    string? Description,
    DateOnly ReleaseDate,
    DateOnly? DueDate,
    string? Notes);
