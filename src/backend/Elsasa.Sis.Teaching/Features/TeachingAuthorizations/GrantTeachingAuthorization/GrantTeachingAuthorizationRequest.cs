namespace Elsasa.Sis.Teaching.Features.TeachingAuthorizations.GrantTeachingAuthorization;

/// <summary>
/// Command request payload for granting a formal teaching authorization (TD-05, P4-S11).
/// </summary>
public sealed record GrantTeachingAuthorizationRequest(
    string? ScopeDescription = null,
    string? AuthorizationScope = null,
    string? CampusScope = null,
    string? CampusBoundary = null,
    string? ProgramScope = null,
    string? EducationalProgramScope = null,
    string? LevelScope = null,
    string? GradeLevelScope = null,
    string? DeliveryModeScope = null,
    DateOnly? EffectiveStartDate = null,
    DateOnly? EffectiveFrom = null,
    DateOnly? EffectiveEndDate = null,
    DateOnly? EffectiveTo = null,
    string? Status = null,
    string? AuthorizationNumber = null,
    Guid? AuthorizedCapabilityId = null,
    string? ConditionsOrRestrictions = null,
    string? Notes = null,
    IReadOnlyList<Guid>? LinkedEvidenceIds = null,
    GrantSupervisionPayload? Supervision = null,
    string? SupervisionType = null,
    string? SupervisionDescription = null,
    DateOnly? SupervisionStartDate = null,
    DateOnly? SupervisionEndDate = null,
    Guid? SupervisingTeacherId = null,
    Guid? MentorTeacherId = null,
    DateOnly? TargetResolutionDate = null)
{
    /// <summary>
    /// Gets the resolved scope description from either ScopeDescription or AuthorizationScope.
    /// </summary>
    public string ResolvedScopeDescription =>
        !string.IsNullOrWhiteSpace(ScopeDescription) ? ScopeDescription.Trim()
        : (!string.IsNullOrWhiteSpace(AuthorizationScope) ? AuthorizationScope.Trim() : string.Empty);

    /// <summary>
    /// Gets the resolved campus scope boundary, defaulting to 'All'.
    /// </summary>
    public string ResolvedCampusScope =>
        !string.IsNullOrWhiteSpace(CampusScope) ? CampusScope.Trim()
        : (!string.IsNullOrWhiteSpace(CampusBoundary) ? CampusBoundary.Trim() : "All");

    /// <summary>
    /// Gets the resolved educational program scope.
    /// </summary>
    public string? ResolvedProgramScope =>
        !string.IsNullOrWhiteSpace(ProgramScope) ? ProgramScope.Trim()
        : (!string.IsNullOrWhiteSpace(EducationalProgramScope) ? EducationalProgramScope.Trim() : null);

    /// <summary>
    /// Gets the resolved grade level scope.
    /// </summary>
    public string? ResolvedLevelScope =>
        !string.IsNullOrWhiteSpace(LevelScope) ? LevelScope.Trim()
        : (!string.IsNullOrWhiteSpace(GradeLevelScope) ? GradeLevelScope.Trim() : null);

    /// <summary>
    /// Gets the resolved effective start date.
    /// </summary>
    public DateOnly ResolvedEffectiveStartDate =>
        EffectiveStartDate ?? EffectiveFrom ?? DateOnly.FromDateTime(DateTime.UtcNow);

    /// <summary>
    /// Gets the resolved effective end date.
    /// </summary>
    public DateOnly? ResolvedEffectiveEndDate =>
        EffectiveEndDate ?? EffectiveTo;

    /// <summary>
    /// Gets the resolved authorization status, defaulting to 'active'.
    /// </summary>
    public string ResolvedStatus =>
        !string.IsNullOrWhiteSpace(Status) ? Status.Trim().ToLowerInvariant() : "active";

    /// <summary>
    /// Gets the resolved supervision payload if provided directly or via flat fields.
    /// </summary>
    public GrantSupervisionPayload? ResolvedSupervision
    {
        get
        {
            if (Supervision is not null)
            {
                return Supervision;
            }

            var type = SupervisionType;
            var desc = SupervisionDescription;
            var start = SupervisionStartDate;
            var end = SupervisionEndDate ?? TargetResolutionDate;
            var mentor = SupervisingTeacherId ?? MentorTeacherId;

            if (!string.IsNullOrWhiteSpace(type) || !string.IsNullOrWhiteSpace(desc) || start.HasValue || mentor.HasValue)
            {
                return new GrantSupervisionPayload(
                    type?.Trim() ?? string.Empty,
                    desc?.Trim() ?? string.Empty,
                    start ?? ResolvedEffectiveStartDate,
                    end,
                    mentor);
            }

            return null;
        }
    }
}
