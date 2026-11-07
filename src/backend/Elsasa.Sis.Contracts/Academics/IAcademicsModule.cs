namespace Elsasa.Sis.Contracts.Academics;

using Elsasa.Sis.Shared.Common;

/// <summary>
/// Synchronous cross-domain query contract for the Academics domain (ADR-001, ADR-003, TD-09).
/// Allows compile-time isolated synchronous inquiries from Operations, Students, and other domains.
/// </summary>
public interface IAcademicsModule
{
    /// <summary>
    /// Retrieves the active learning structure for a given operational/academic period.
    /// </summary>
    /// <param name="academicPeriodId">The unique identifier of the academic period.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the active learning structure DTO, or <see langword="null"/> if none is active.</returns>
    Task<Result<ActiveLearningStructureDto?>> GetActiveLearningStructureForPeriodAsync(Guid academicPeriodId, CancellationToken ct = default);

    /// <summary>
    /// Retrieves all learning units belonging to a specific learning structure.
    /// </summary>
    /// <param name="learningStructureId">The unique identifier of the learning structure.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the read-only list of learning units.</returns>
    Task<Result<IReadOnlyList<LearningUnitDto>>> GetLearningUnitsByStructureAsync(Guid learningStructureId, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a specific learning unit by its unique identifier.
    /// </summary>
    /// <param name="learningUnitId">The unique identifier of the learning unit.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the learning unit DTO, or <see langword="null"/> if not found.</returns>
    Task<Result<LearningUnitDto?>> GetLearningUnitByIdAsync(Guid learningUnitId, CancellationToken ct = default);

    /// <summary>
    /// Retrieves all academic requirements belonging to a specific learning structure.
    /// </summary>
    /// <param name="learningStructureId">The unique identifier of the learning structure.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the read-only list of academic requirements.</returns>
    Task<Result<IReadOnlyList<AcademicRequirementDto>>> GetAcademicRequirementsByStructureAsync(Guid learningStructureId, CancellationToken ct = default);

    /// <summary>
    /// Checks whether a student is actively participating in a specific learning unit for an academic period.
    /// </summary>
    /// <param name="studentId">The unique identifier of the student.</param>
    /// <param name="learningUnitId">The unique identifier of the learning unit.</param>
    /// <param name="academicPeriodId">The unique identifier of the academic period.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing <see langword="true"/> if actively participating; otherwise <see langword="false"/>.</returns>
    Task<Result<bool>> IsStudentParticipatingAsync(Guid studentId, Guid learningUnitId, Guid academicPeriodId, CancellationToken ct = default);

    /// <summary>
    /// Retrieves attendance summary aggregates for a student in an academic period.
    /// </summary>
    /// <param name="studentId">The unique identifier of the student.</param>
    /// <param name="academicPeriodId">The unique identifier of the academic period.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the student attendance summary DTO.</returns>
    Task<Result<StudentAttendanceSummaryDto>> GetStudentAttendanceSummaryAsync(Guid studentId, Guid academicPeriodId, CancellationToken ct = default);

    /// <summary>
    /// Retrieves all completed learning units for a student across their academic history.
    /// </summary>
    /// <param name="studentId">The unique identifier of the student.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the read-only list of completed participation DTOs.</returns>
    Task<Result<IReadOnlyList<CompletedParticipationDto>>> GetStudentCompletedUnitsAsync(Guid studentId, CancellationToken ct = default);

    /// <summary>
    /// Retrieves operative approved results for a student within a specific academic period.
    /// </summary>
    /// <param name="studentId">The unique identifier of the student.</param>
    /// <param name="academicPeriodId">The unique identifier of the academic period.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the read-only list of approved results.</returns>
    Task<Result<IReadOnlyList<StudentApprovedResultDto>>> GetStudentApprovedResultsAsync(
        Guid studentId,
        Guid academicPeriodId,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves the entrance or diagnostic assessment score for an applicant.
    /// </summary>
    /// <param name="applicantId">The unique identifier of the applicant.</param>
    /// <param name="assessmentId">The unique identifier of the assessment.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the applicant assessment score DTO, or <see langword="null"/> if not found.</returns>
    Task<Result<ApplicantAssessmentScoreDto?>> GetApplicantAssessmentScoreAsync(
        Guid applicantId,
        Guid assessmentId,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves the frozen content snapshot DTO for an approved report card.
    /// </summary>
    /// <param name="reportCardId">The unique identifier of the report card.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the report card snapshot DTO, or <see langword="null"/> if not found or not approved.</returns>
    Task<Result<ReportCardSnapshotDto?>> GetStudentReportCardSnapshotAsync(
        Guid reportCardId,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves the formal academic standing determination for a student in an academic period.
    /// </summary>
    /// <param name="studentId">The unique identifier of the student.</param>
    /// <param name="academicPeriodId">The unique identifier of the academic period.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the student academic standing DTO, or <see langword="null"/> if not determined.</returns>
    Task<Result<StudentAcademicStandingDto?>> GetStudentAcademicStandingAsync(
        Guid studentId,
        Guid academicPeriodId,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves the authoritative academic progression recommendation for a student in an academic period.
    /// </summary>
    /// <param name="studentId">The unique identifier of the student.</param>
    /// <param name="academicPeriodId">The unique identifier of the academic period.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the promotion recommendation DTO, or <see langword="null"/> if not issued.</returns>
    Task<Result<PromotionRecommendationDto?>> GetPromotionRecommendationAsync(
        Guid studentId,
        Guid academicPeriodId,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves all academic achievements, distinctions, and honors conferred on a student, optionally filtered by academic period.
    /// </summary>
    /// <param name="studentId">The unique identifier of the student.</param>
    /// <param name="academicPeriodId">Optional academic period filter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the read-only list of academic achievements.</returns>
    Task<Result<IReadOnlyList<AcademicAchievementDto>>> GetStudentAcademicAchievementsAsync(
        Guid studentId,
        Guid? academicPeriodId = null,
        CancellationToken ct = default);

    /// <summary>
    /// Retrieves aggregated learning progress counts for a student within an academic period.
    /// </summary>
    /// <param name="studentId">The unique identifier of the student.</param>
    /// <param name="academicPeriodId">The unique identifier of the academic period.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A result containing the read-only list of learning progress summary DTOs.</returns>
    Task<Result<IReadOnlyList<LearningProgressSummaryDto>>> GetStudentLearningProgressSummaryAsync(
        Guid studentId,
        Guid academicPeriodId,
        CancellationToken ct = default);
}
