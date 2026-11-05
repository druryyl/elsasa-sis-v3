namespace Elsasa.Sis.Academics.Features.Attendance.RecordSessionAttendance;

/// <summary>
/// Summary response of attendance counts recorded for a learning activity session (P4-S08, GAP-005, OQ-003, TD-04, UC-ACD-03).
/// </summary>
/// <param name="LearningActivityId">Unique identifier of the learning activity.</param>
/// <param name="TotalRecorded">Total attendance records for this activity.</param>
/// <param name="TotalPresent">Count of students marked present.</param>
/// <param name="TotalAbsent">Count of students marked absent.</param>
/// <param name="TotalLate">Count of students marked late.</param>
/// <param name="TotalExcused">Count of students marked excused absence.</param>
/// <param name="TotalPartial">Count of students marked partial attendance.</param>
/// <param name="RecordedDate">Date the roll call was recorded.</param>
/// <param name="RecordedBy">Username or actor who recorded the attendance.</param>
public sealed record RecordSessionAttendanceResponse(
    Guid LearningActivityId,
    int TotalRecorded,
    int TotalPresent,
    int TotalAbsent,
    int TotalLate,
    int TotalExcused,
    int TotalPartial,
    DateOnly RecordedDate,
    string RecordedBy);
