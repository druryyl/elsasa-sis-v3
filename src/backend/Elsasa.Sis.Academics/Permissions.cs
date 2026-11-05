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
    }
}
