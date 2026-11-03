namespace Elsasa.Sis.Students;

/// <summary>
/// Authoritative permission constants for the Students domain (ADR-009).
/// </summary>
public static class Permissions
{
    public static class Students
    {
        public static class Admission
        {
            public const string View = "Permissions.Students.Admission.View";
            public const string Register = "Permissions.Students.Admission.Register";
            public const string Review = "Permissions.Students.Admission.Review";
            public const string VerifyDocument = "Permissions.Students.Admission.VerifyDocument";
            public const string Decide = "Permissions.Students.Admission.Decide";
        }
    }
}
