namespace Elsasa.Sis.Teaching;

/// <summary>
/// Authoritative permission constants for the Teaching domain (ADR-009, P1-S04, P1-S03).
/// </summary>
public static class Permissions
{
    public static class Faculty
    {
        public const string View = "Permissions.Teaching.Faculty.View";
        public const string Manage = "Permissions.Teaching.Faculty.Manage";
        public const string Verify = "Permissions.Teaching.Faculty.Verify";
        public const string Authorize = "Permissions.Teaching.Faculty.Authorize";
        public const string Correct = "Permissions.Teaching.Faculty.Correct";
    }

    public static class Assignments
    {
        public const string View = "Permissions.Teaching.Assignments.View";
        public const string Manage = "Permissions.Teaching.Assignments.Manage";
        public const string Approve = "Permissions.Teaching.Assignments.Approve";
    }

    public static class Substitutions
    {
        public const string Arrange = "Permissions.Teaching.Substitutions.Arrange";
        public const string ApproveException = "Permissions.Teaching.Substitutions.ApproveException";
    }

    public static class Workload
    {
        public const string View = "Permissions.Teaching.Workload.View";
        public const string Recalculate = "Permissions.Teaching.Workload.Recalculate";
        public const string RequestException = "Permissions.Teaching.Workload.RequestException";
        public const string ApproveException = "Permissions.Teaching.Workload.ApproveException";
    }

    public static class Availability
    {
        public const string Declare = "Permissions.Teaching.Availability.Declare";
    }

    /// <summary>
    /// Explicit domain prefix hierarchy matching cross-domain permission conventions.
    /// </summary>
    public static class Teaching
    {
        public static class Faculty
        {
            public const string View = Permissions.Faculty.View;
            public const string Manage = Permissions.Faculty.Manage;
            public const string Verify = Permissions.Faculty.Verify;
            public const string Authorize = Permissions.Faculty.Authorize;
            public const string Correct = Permissions.Faculty.Correct;
        }

        public static class Assignments
        {
            public const string View = Permissions.Assignments.View;
            public const string Manage = Permissions.Assignments.Manage;
            public const string Approve = Permissions.Assignments.Approve;
        }

        public static class Substitutions
        {
            public const string Arrange = Permissions.Substitutions.Arrange;
            public const string ApproveException = Permissions.Substitutions.ApproveException;
        }

        public static class Workload
        {
            public const string View = Permissions.Workload.View;
            public const string Recalculate = Permissions.Workload.Recalculate;
            public const string RequestException = Permissions.Workload.RequestException;
            public const string ApproveException = Permissions.Workload.ApproveException;
        }

        public static class Availability
        {
            public const string Declare = Permissions.Availability.Declare;
        }
    }
}
