namespace Elsasa.Sis.Academics.Common;

using Elsasa.Sis.Shared.Common;

/// <summary>
/// Domain errors specific to Academics domain boundaries.
/// </summary>
public static class DomainErrors
{
    public static class Academics
    {
        public static class Assessments
        {
            public static readonly DomainError NotFound =
                DomainError.NotFound(
                    "Academics.Assessments.NotFound",
                    "Assessment was not found.");

            public static readonly DomainError CannotCancelConductedAssessment =
                DomainError.Conflict(
                    "Academics.Assessments.CannotCancelConductedAssessment",
                    "Cannot cancel conducted assessment or assessment with recorded academic results.");
        }

        public static class Transcripts
        {
            public static readonly DomainError NotFound =
                DomainError.NotFound(
                    "Academics.Transcripts.NotFound",
                    "Transcript was not found.");

            public static readonly DomainError AlreadyIssued =
                DomainError.Conflict(
                    "Academics.Transcripts.AlreadyIssued",
                    "Transcript has already been issued and cannot be modified.");

            public static readonly DomainError StudentNotEnrolled =
                DomainError.Validation(
                    "Academics.Transcripts.StudentNotEnrolled",
                    "Student is not actively enrolled.");
        }

        public static class ReportCards
        {
            public static readonly DomainError AlreadyExists =
                DomainError.Conflict(
                    "Academics.ReportCards.AlreadyExists",
                    "A report card already exists for this student, academic period, and report type.");

            public static readonly DomainError NoActiveParticipations =
                DomainError.NotFound(
                    "Academics.ReportCards.NoActiveParticipations",
                    "No active academic participations were found for this student in the specified period.");

            public static readonly DomainError NotFound =
                DomainError.NotFound(
                    "Academics.ReportCards.NotFound",
                    "Report card was not found.");

            public static readonly DomainError NotDraft =
                DomainError.Conflict(
                    "Academics.ReportCards.NotDraft",
                    "Report card must be in draft status to be approved.");

            public static readonly DomainError NotApproved =
                DomainError.Conflict(
                    "Academics.ReportCards.NotApproved",
                    "Report card must be in approved status to be published.");

            public static readonly DomainError AlreadyApproved =
                DomainError.Conflict(
                    "Academics.ReportCards.AlreadyApproved",
                    "Report card has already been approved.");

            public static readonly DomainError AlreadyPublished =
                DomainError.Conflict(
                    "Academics.ReportCards.AlreadyPublished",
                    "Report card has already been published.");
        }
    }
}
