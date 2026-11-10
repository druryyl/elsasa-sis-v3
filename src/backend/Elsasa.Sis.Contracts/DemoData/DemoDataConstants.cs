namespace Elsasa.Sis.Contracts.DemoData;

/// <summary>
/// Deterministic GUID constants for flagship personas and staged applicants.
/// All GUIDs are fixed and immutable so downstream finance and academics
/// seeders can link transactions and results reliably (GAP-005, OQ-003).
/// </summary>
public static class DemoDataConstants
{
    /// <summary>
    /// Default password for all pre-configured demo user accounts.
    /// </summary>
    public const string DefaultDemoPassword = "Demo123!";

    // Flagship personas - Jonathan Pratama (High Honors Student)
    public static readonly Guid JonathanStudentId = new("00000000-0000-0000-0001-000000000001");
    public static readonly Guid JonathanApplicantId = new("00000000-0000-0000-0001-000000000002");
    public static readonly Guid HendraParentId = new("00000000-0000-0000-0001-000000000003");

    // Grace Santoso (Retake & Overdue Student)
    public static readonly Guid GraceStudentId = new("00000000-0000-0000-0002-000000000001");
    public static readonly Guid GraceApplicantId = new("00000000-0000-0000-0002-000000000002");
    public static readonly Guid GraceGuardianId = new("00000000-0000-0000-0002-000000000003");

    // Budi Wijaya (Live Grading Student)
    public static readonly Guid BudiStudentId = new("00000000-0000-0000-0003-000000000001");
    public static readonly Guid BudiGuardianId = new("00000000-0000-0000-0003-000000000003");

    // Ruth Halim (1-Click Enrollment Demo)
    public static readonly Guid RuthApplicantId = new("00000000-0000-0000-0004-000000000005");
    public static readonly Guid RuthStudentId = new("00000000-0000-0000-0005-000000000001");
    public static readonly Guid RuthGuardianId = new("00000000-0000-0000-0005-000000000002");

    // Other staged admission applicants
    public static readonly Guid DanielApplicantId = new("00000000-0000-0000-0004-000000000001"); // Submitted
    public static readonly Guid MariaApplicantId = new("00000000-0000-0000-0004-000000000002");  // Under Review
    public static readonly Guid SamuelApplicantId = new("00000000-0000-0000-0004-000000000003"); // Entrance Assessed
    public static readonly Guid EstherApplicantId = new("00000000-0000-0000-0004-000000000004"); // Accepted Awaiting Fee
    public static readonly Guid TimotiusApplicantId = new("00000000-0000-0000-0004-000000000006"); // Withdrawn

    // Deterministic IDs for Jonathan's Semester 2 unpaid invoice (Finance transactions)
    public static readonly Guid JonathanUnpaidInvoiceId = new("00000000-0000-0000-0005-000000000001");

    // Flagship teaching personas (GAP-008, TD-09, P5-S16)
    public static readonly Guid BudiHartonoTeacherId = new("00000000-0000-0000-0006-000000000001");
    public static readonly Guid SitiNurhalizaTeacherId = new("00000000-0000-0000-0006-000000000002");
    public static readonly Guid JohnMillerTeacherId = new("00000000-0000-0000-0006-000000000003");
    public static readonly Guid HendraSetiawanTeacherId = new("00000000-0000-0000-0006-000000000004");
}
