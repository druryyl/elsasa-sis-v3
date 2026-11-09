namespace Elsasa.Sis.Academics.Features.Transcripts;

using Elsasa.Sis.Contracts.Academics;

/// <summary>
/// Domain utility for computing academic period Grade Point Average (GPA) across completed courses (UC-ACD-13, P5-S13).
/// </summary>
public static class TranscriptGpaCalculator
{
    /// <summary>
    /// Calculates the weighted GPA for a collection of course entries based on credit weight and letter/numeric marks.
    /// </summary>
    /// <param name="courses">The courses completed or credited in an academic period.</param>
    /// <returns>Rounded 4.0-scale GPA, or null if no qualifying grade points exist.</returns>
    public static decimal? CalculatePeriodGpa(IReadOnlyList<TranscriptCourseEntryDto>? courses)
    {
        if (courses is null || courses.Count == 0)
        {
            return null;
        }

        decimal totalCredits = 0m;
        decimal totalGradePoints = 0m;

        foreach (var course in courses)
        {
            var gradePoint = ResolveGradePoint(course.GradeLetter, course.ScoreNumeric);
            if (gradePoint.HasValue && course.Credits > 0)
            {
                totalCredits += course.Credits;
                totalGradePoints += (course.Credits * gradePoint.Value);
            }
        }

        if (totalCredits <= 0m)
        {
            return null;
        }

        return Math.Round(totalGradePoints / totalCredits, 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Resolves standard 4.0 grade point value from letter grade or fallback numeric score.
    /// </summary>
    /// <param name="gradeLetter">The letter grade achieved (e.g. 'A', 'B+').</param>
    /// <param name="scoreNumeric">The numeric percentage score (0-100).</param>
    /// <returns>Grade point on 4.0 scale, or null if neither could be determined.</returns>
    public static decimal? ResolveGradePoint(string? gradeLetter, decimal? scoreNumeric)
    {
        if (!string.IsNullOrWhiteSpace(gradeLetter))
        {
            return gradeLetter.Trim().ToUpperInvariant() switch
            {
                "A+" or "A" => 4.0m,
                "A-" => 3.7m,
                "B+" => 3.3m,
                "B" => 3.0m,
                "B-" => 2.7m,
                "C+" => 2.3m,
                "C" => 2.0m,
                "C-" => 1.7m,
                "D" => 1.0m,
                "F" => 0.0m,
                _ => ResolveFromNumeric(scoreNumeric)
            };
        }

        return ResolveFromNumeric(scoreNumeric);
    }

    private static decimal? ResolveFromNumeric(decimal? score)
    {
        if (!score.HasValue)
        {
            return null;
        }

        var val = score.Value;
        return val switch
        {
            >= 85.0m => 4.0m,
            >= 75.0m => 3.0m,
            >= 65.0m => 2.0m,
            >= 50.0m => 1.0m,
            _ => 0.0m
        };
    }
}
