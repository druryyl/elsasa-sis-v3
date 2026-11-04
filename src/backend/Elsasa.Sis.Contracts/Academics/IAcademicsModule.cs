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
}
