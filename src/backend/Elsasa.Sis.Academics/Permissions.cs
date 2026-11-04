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
    }
}
