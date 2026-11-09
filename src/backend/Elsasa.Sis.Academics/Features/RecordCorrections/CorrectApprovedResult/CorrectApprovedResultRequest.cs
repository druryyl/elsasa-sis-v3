namespace Elsasa.Sis.Academics.Features.RecordCorrections.CorrectApprovedResult;

using System.Text.Json.Serialization;

/// <summary>
/// Request payload for auditable in-place correction of an approved academic result (UC-ACD-19, TD-06, P6-S14).
/// Requires a mandatory justification reason (minimum 10 characters) and at least one altered value field.
/// </summary>
/// <param name="ScoreNumeric">Corrected numeric score, if applicable.</param>
/// <param name="ScoreMax">Corrected maximum possible score, if applicable.</param>
/// <param name="GradeLetter">Corrected letter grade (e.g. 'A', 'B+'), if applicable.</param>
/// <param name="GradeLabel">Corrected competency or descriptive label, if applicable.</param>
/// <param name="NarrativeFeedback">Corrected qualitative narrative feedback or remarks.</param>
/// <param name="Reason">Mandatory justification explaining the historical grade correction (minimum 10 characters).</param>
/// <param name="Evidence">Optional URL, document reference, or supporting justification text.</param>
/// <param name="IsMaterial">Indicates whether the alteration is material (e.g. crossing passing threshold).</param>
/// <param name="NewScoreNumeric">Alternative parameter for corrected numeric score.</param>
/// <param name="NewScoreMax">Alternative parameter for corrected maximum possible score.</param>
/// <param name="NewGradeLetter">Alternative parameter for corrected letter grade.</param>
/// <param name="NewGradeLabel">Alternative parameter for corrected grade label.</param>
/// <param name="NewNarrativeFeedback">Alternative parameter for corrected narrative feedback.</param>
public sealed record CorrectApprovedResultRequest(
    [property: JsonPropertyName("score_numeric")] decimal? ScoreNumeric = null,
    [property: JsonPropertyName("score_max")] decimal? ScoreMax = null,
    [property: JsonPropertyName("grade_letter")] string? GradeLetter = null,
    [property: JsonPropertyName("grade_label")] string? GradeLabel = null,
    [property: JsonPropertyName("narrative_feedback")] string? NarrativeFeedback = null,
    [property: JsonPropertyName("reason")] string Reason = "",
    [property: JsonPropertyName("evidence")] string? Evidence = null,
    [property: JsonPropertyName("is_material")] bool IsMaterial = false,
    [property: JsonPropertyName("new_score_numeric")] decimal? NewScoreNumeric = null,
    [property: JsonPropertyName("new_score_max")] decimal? NewScoreMax = null,
    [property: JsonPropertyName("new_grade_letter")] string? NewGradeLetter = null,
    [property: JsonPropertyName("new_grade_label")] string? NewGradeLabel = null,
    [property: JsonPropertyName("new_narrative_feedback")] string? NewNarrativeFeedback = null)
{
    /// <summary>
    /// Effective corrected numeric score taking into account either parameter name.
    /// </summary>
    [JsonIgnore]
    public decimal? EffectiveScoreNumeric => NewScoreNumeric ?? ScoreNumeric;

    /// <summary>
    /// Effective corrected score max taking into account either parameter name.
    /// </summary>
    [JsonIgnore]
    public decimal? EffectiveScoreMax => NewScoreMax ?? ScoreMax;

    /// <summary>
    /// Effective corrected grade letter taking into account either parameter name.
    /// </summary>
    [JsonIgnore]
    public string? EffectiveGradeLetter => NewGradeLetter ?? GradeLetter;

    /// <summary>
    /// Effective corrected grade label taking into account either parameter name.
    /// </summary>
    [JsonIgnore]
    public string? EffectiveGradeLabel => NewGradeLabel ?? GradeLabel;

    /// <summary>
    /// Effective corrected narrative feedback taking into account either parameter name.
    /// </summary>
    [JsonIgnore]
    public string? EffectiveNarrativeFeedback => NewNarrativeFeedback ?? NarrativeFeedback;

    /// <summary>
    /// Indicates whether at least one correction value field was supplied in the request.
    /// </summary>
    [JsonIgnore]
    public bool HasAnyCorrection =>
        EffectiveScoreNumeric.HasValue ||
        EffectiveScoreMax.HasValue ||
        EffectiveGradeLetter != null ||
        EffectiveGradeLabel != null ||
        EffectiveNarrativeFeedback != null;
}
