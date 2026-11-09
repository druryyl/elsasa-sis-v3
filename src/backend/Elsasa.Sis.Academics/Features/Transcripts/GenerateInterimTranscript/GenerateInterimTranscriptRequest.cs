namespace Elsasa.Sis.Academics.Features.Transcripts.GenerateInterimTranscript;

/// <summary>
/// Request contract for compiling an interim cumulative academic transcript (UC-ACD-13, P5-S13).
/// </summary>
/// <param name="StudentId">The unique identifier of the target student.</param>
/// <param name="TranscriptType">The transcript type ('official' or 'unofficial').</param>
/// <param name="PeriodStart">The starting boundary date of the academic period history to include.</param>
/// <param name="PeriodEnd">The ending boundary date of the academic period history to include.</param>
/// <param name="Purpose">Optional declared issuance purpose (e.g., "Transfer Application", "Internal Advising").</param>
/// <param name="Notes">Optional administrative notes.</param>
public sealed record GenerateInterimTranscriptRequest(
    Guid StudentId,
    string TranscriptType,
    DateOnly PeriodStart,
    DateOnly PeriodEnd,
    string? Purpose = null,
    string? Notes = null);
