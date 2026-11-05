namespace Elsasa.Sis.Academics;

/// <summary>
/// Authoritative permission constants for the Academics domain (ADR-009, TD-07).
/// </summary>
public static class Permissions
{
    public static class Academics
    {
        public static class Curriculum
        {
            public const string View = "academics.curriculum.view";
            public const string Manage = "academics.curriculum.manage";
            public const string Submit = "academics.curriculum.submit";
            public const string Approve = "academics.curriculum.approve";
            public const string Archive = "academics.curriculum.archive";
        }

        public static class Participations
        {
            public const string View = "academics.participations.view";
            public const string Manage = "academics.participations.manage";
            public const string RecognizePrior = "academics.participations.recognize_prior";
            public const string Conclude = "academics.participations.conclude";
        }

        public static class Activities
        {
            public const string View = "academics.activities.view";
            public const string Manage = "academics.activities.manage";
            public const string Conduct = "academics.activities.conduct";
        }

        public static class Attendance
        {
            public const string View = "academics.attendance.view";
            public const string Record = "academics.attendance.record";
            public const string Correct = "academics.attendance.correct";
        }

        public static class Assignments
        {
            public const string View = "academics.assignments.view";
            public const string Manage = "academics.assignments.manage";
            public const string Release = "academics.assignments.release";
            public const string Close = "academics.assignments.close";
        }

        public static class Submissions
        {
            public const string Submit = "academics.submissions.submit";
            public const string Exception = "academics.submissions.exception";
            public const string View = "academics.submissions.view";
        }

        public static class Assessments
        {
            public const string View = "academics.assessments.view";
            public const string Manage = "academics.assessments.manage";
            public const string Conduct = "academics.assessments.conduct";
        }

        public static class Evidence
        {
            public const string Record = "academics.evidence.record";
            public const string View = "academics.evidence.view";
        }

        public static class Results
        {
            public const string Evaluate = "academics.results.evaluate";
            public const string Approve = "academics.results.approve";
            public const string Publish = "academics.results.publish";
            public const string View = "academics.results.view";
        }

        public static class ReportCards
        {
            public const string Compile = "academics.reportcards.compile";
            public const string Approve = "academics.reportcards.approve";
            public const string Publish = "academics.reportcards.publish";
            public const string View = "academics.reportcards.view";
        }

        public static class Transcripts
        {
            public const string Generate = "academics.transcripts.generate";
            public const string Issue = "academics.transcripts.issue";
            public const string View = "academics.transcripts.view";
        }

        public static class Corrections
        {
            public const string Correct = "academics.corrections.correct";
            public const string ViewHistory = "academics.corrections.viewhistory";
        }
    }
}
