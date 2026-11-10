namespace Elsasa.Sis.Contracts.DemoData;

/// <summary>
/// Deterministic flagship teacher persona identifiers returned by the Teaching demo seeder.
/// </summary>
/// <param name="BudiHartonoTeacherId">Persona 1: Senior Permanent Teacher (active profile, verified Serdik, active authorization).</param>
/// <param name="SitiNurhalizaTeacherId">Persona 2: Contract Teacher under Supervision (active profile, verified S1, conditional authorization with mentor).</param>
/// <param name="JohnMillerTeacherId">Persona 3: Visiting Non-Employee Teacher (null employee_ref, passport identity, verified CELTA).</param>
/// <param name="HendraSetiawanTeacherId">Persona 4: Pending Candidate (pending_approval standing, submitted credential awaiting verification).</param>
public sealed record FlagshipTeacherPersonas(
    Guid BudiHartonoTeacherId,
    Guid SitiNurhalizaTeacherId,
    Guid JohnMillerTeacherId,
    Guid HendraSetiawanTeacherId);

/// <summary>
/// Result of seeding the Teaching domain reference data, personas, credentials, authorizations, and supervision.
/// </summary>
/// <param name="RolesCreated">The count of teaching roles seeded.</param>
/// <param name="CapabilityTypesCreated">The count of capability taxonomy types seeded.</param>
/// <param name="QualificationTypesCreated">The count of qualification types seeded.</param>
/// <param name="TeacherProfilesCreated">The count of teacher profiles seeded.</param>
/// <param name="CapabilitiesCreated">The count of teaching capabilities seeded.</param>
/// <param name="QualificationsCreated">The count of qualification evidence records seeded.</param>
/// <param name="AuthorizationsCreated">The count of teaching authorizations seeded.</param>
/// <param name="SupervisionRequirementsCreated">The count of supervision requirements seeded.</param>
/// <param name="Personas">The deterministic flagship teacher personas seeded.</param>
public sealed record TeachingSeedResult(
    int RolesCreated,
    int CapabilityTypesCreated,
    int QualificationTypesCreated,
    int TeacherProfilesCreated,
    int CapabilitiesCreated,
    int QualificationsCreated,
    int AuthorizationsCreated,
    int SupervisionRequirementsCreated,
    FlagshipTeacherPersonas Personas);
