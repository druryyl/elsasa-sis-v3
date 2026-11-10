namespace Elsasa.Sis.Teaching;

/// <summary>
/// Authoritative permission constants for the Teaching domain (ADR-009, P1-S04).
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
    }
}
