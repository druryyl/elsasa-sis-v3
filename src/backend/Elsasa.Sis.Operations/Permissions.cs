namespace Elsasa.Sis.Operations;

/// <summary>
/// Authoritative permission constants for the Operations domain (ADR-009).
/// </summary>
public static class Permissions
{
    public static class Operations
    {
        public static class Calendar
        {
            public const string View = "Permissions.Operations.Calendar.View";
            public const string Manage = "Permissions.Operations.Calendar.Manage";
            public const string Approve = "Permissions.Operations.Calendar.Approve";
            public const string Publish = "Permissions.Operations.Calendar.Publish";
        }

        public static class Closure
        {
            public const string Declare = "Permissions.Operations.Closure.Declare";
        }

        public static class Records
        {
            public const string Correct = "Permissions.Operations.Records.Correct";
        }
    }
}
