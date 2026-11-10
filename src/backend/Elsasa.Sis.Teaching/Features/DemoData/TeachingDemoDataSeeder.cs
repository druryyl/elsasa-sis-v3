namespace Elsasa.Sis.Teaching.Features.DemoData;

using Dapper;
using Elsasa.Sis.Contracts.DemoData;
using Elsasa.Sis.Shared.Database;

/// <summary>
/// Autonomous demo seeder for the Teaching domain (ADR-002, ADR-004, TD-09, P5-S16).
/// Seeds reference taxonomies (roles, capability types, qualification types) idempotently,
/// and creates 4 realistic Hagios School of Life teaching personas:
/// 1. Budi Hartono, M.Pd. (Senior Permanent Teacher, active standing, verified Serdik & S1, active authorization)
/// 2. Siti Nurhaliza, S.Pd. (Contract Teacher under Supervision, active standing, submitted credential, conditional authorization under co-teaching)
/// 3. John Miller, B.Ed. (Visiting Non-Employee Teacher, active standing, null employee_ref, passport ref, verified CELTA, active guest lecture authorization)
/// 4. Hendra Setiawan (Pending Onboarding Candidate, pending_approval standing, submitted coaching credential, zero authorizations).
/// </summary>
public sealed class TeachingDemoDataSeeder(
    ISqlConnectionFactory connectionFactory) : ITeachingDemoDataSeeder
{
    private const string _createdBy = "demo-data-seeder";
    private const string _campusMain = "Kampus Utama";
    private const string _kepalaSekolah = "Kepala Sekolah";

    // ─────────────────────────────────────────────────────────────────────────
    // Deterministic Authorization and Evidence GUIDs
    // ─────────────────────────────────────────────────────────────────────────
    private static readonly Guid _budiMathCapabilityId = new("00000000-0000-0000-0007-000000000011");
    private static readonly Guid _budiIpaCapabilityId = new("00000000-0000-0000-0007-000000000012");
    private static readonly Guid _budiSerdikEvidenceId = new("00000000-0000-0000-0007-000000000013");
    private static readonly Guid _budiDegreeEvidenceId = new("00000000-0000-0000-0007-000000000014");
    private static readonly Guid _budiAuthorizationId = new("00000000-0000-0000-0007-000000000015");

    private static readonly Guid _sitiEngCapabilityId = new("00000000-0000-0000-0007-000000000021");
    private static readonly Guid _sitiDegreeEvidenceId = new("00000000-0000-0000-0007-000000000022");
    private static readonly Guid _sitiAuthorizationId = new("00000000-0000-0000-0007-000000000023");
    private static readonly Guid _sitiSupervisionReqId = new("00000000-0000-0000-0007-000000000024");

    private static readonly Guid _johnImmersionCapabilityId = new("00000000-0000-0000-0007-000000000031");
    private static readonly Guid _johnCeltaEvidenceId = new("00000000-0000-0000-0007-000000000032");
    private static readonly Guid _johnAuthorizationId = new("00000000-0000-0000-0007-000000000033");

    private static readonly Guid _hendraCoachingEvidenceId = new("00000000-0000-0000-0007-000000000041");

    /// <inheritdoc/>
    public async Task<TeachingSeedResult> SeedTeachingAsync(
        OperationsSeedResult operationsContext,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(operationsContext);

        await using var connection = await connectionFactory.CreateConnectionAsync(cancellationToken);
        var baseDate = operationsContext.BaseDate.Date;
        var now = DateTimeOffset.UtcNow;

        // 1. Seed Reference Data (Teaching Roles, Capability Types, Qualification Types)
        var roleMap = await SeedTeachingRolesAsync(connection, now, cancellationToken);
        var capTypeMap = await SeedCapabilityTypesAsync(connection, now, cancellationToken);
        var qualTypeMap = await SeedQualificationTypesAsync(connection, now, cancellationToken);

        // 2. Seed Persona 1: Budi Hartono, M.Pd. (Senior Permanent Teacher)
        await SeedPersona1BudiHartonoAsync(connection, baseDate, now, roleMap["GURU_TETAP"], capTypeMap, qualTypeMap, cancellationToken);

        // 3. Seed Persona 2: Siti Nurhaliza, S.Pd. (Contract Teacher under Supervision)
        await SeedPersona2SitiNurhalizaAsync(connection, baseDate, now, roleMap["GURU_KONTRAK"], capTypeMap, qualTypeMap, cancellationToken);

        // 4. Seed Persona 3: John Miller, B.Ed. (Visiting Non-Employee Teacher)
        await SeedPersona3JohnMillerAsync(connection, baseDate, now, roleMap["GURU_TAMU"], capTypeMap, qualTypeMap, cancellationToken);

        // 5. Seed Persona 4: Hendra Setiawan (Pending Onboarding Candidate)
        await SeedPersona4HendraSetiawanAsync(connection, baseDate, now, roleMap["GURU_KONTRAK"], qualTypeMap, cancellationToken);

        // Aggregate counts
        var rolesCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition("SELECT COUNT(*) FROM teaching.teaching_roles;", cancellationToken: cancellationToken));
        var capTypesCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition("SELECT COUNT(*) FROM teaching.capability_types;", cancellationToken: cancellationToken));
        var qualTypesCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition("SELECT COUNT(*) FROM teaching.qualification_types;", cancellationToken: cancellationToken));
        var profilesCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition("SELECT COUNT(*) FROM teaching.teacher_profiles;", cancellationToken: cancellationToken));
        var capsCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition("SELECT COUNT(*) FROM teaching.teaching_capabilities;", cancellationToken: cancellationToken));
        var qualsCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition("SELECT COUNT(*) FROM teaching.qualification_evidence;", cancellationToken: cancellationToken));
        var authsCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition("SELECT COUNT(*) FROM teaching.teaching_authorizations;", cancellationToken: cancellationToken));
        var supCount = await connection.ExecuteScalarAsync<int>(new CommandDefinition("SELECT COUNT(*) FROM teaching.supervision_requirements;", cancellationToken: cancellationToken));

        return new TeachingSeedResult(
            RolesCreated: rolesCount,
            CapabilityTypesCreated: capTypesCount,
            QualificationTypesCreated: qualTypesCount,
            TeacherProfilesCreated: profilesCount,
            CapabilitiesCreated: capsCount,
            QualificationsCreated: qualsCount,
            AuthorizationsCreated: authsCount,
            SupervisionRequirementsCreated: supCount,
            Personas: new FlagshipTeacherPersonas(
                BudiHartonoTeacherId: DemoDataConstants.BudiHartonoTeacherId,
                SitiNurhalizaTeacherId: DemoDataConstants.SitiNurhalizaTeacherId,
                JohnMillerTeacherId: DemoDataConstants.JohnMillerTeacherId,
                HendraSetiawanTeacherId: DemoDataConstants.HendraSetiawanTeacherId));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Reference Data Seeders
    // ─────────────────────────────────────────────────────────────────────────

    private static async Task<Dictionary<string, Guid>> SeedTeachingRolesAsync(
        System.Data.Common.DbConnection connection,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var roles = new[]
        {
            ("GURU_TETAP", "Guru Tetap", "Senior Permanent Teacher with full pedagogical responsibilities", true),
            ("GURU_KONTRAK", "Guru Kontrak", "Contract Teacher under periodic review and supervision", true),
            ("GURU_TAMU", "Guru Tamu", "Visiting Non-Employee Lecturer or Expert Instructor", true),
            ("RELAWAN", "Relawan", "Volunteer Teaching Assistant or Community Educator", false),
            ("PENILAI_EKSTERNAL", "Penilai Eksternal", "External Assessor or Evaluation Specialist", true),
            ("FASILITATOR", "Fasilitator", "Co-curricular or Workshop Activity Facilitator", false)
        };

        const string sql = """
            IF NOT EXISTS (SELECT 1 FROM teaching.teaching_roles WHERE code = @Code)
            BEGIN
                INSERT INTO teaching.teaching_roles (
                    id, code, name, description, requires_qualification_check, is_active, created_at, created_by
                ) VALUES (
                    @Id, @Code, @Name, @Description, @RequiresCheck, 1, @CreatedAt, @CreatedBy
                );
            END;
            SELECT TOP (1) id FROM teaching.teaching_roles WHERE code = @Code;
            """;

        var result = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (var (code, name, description, reqCheck) in roles)
        {
            var id = await connection.ExecuteScalarAsync<Guid>(new CommandDefinition(
                sql,
                new
                {
                    Id = Guid.NewGuid(),
                    Code = code,
                    Name = name,
                    Description = description,
                    RequiresCheck = reqCheck,
                    CreatedAt = now,
                    CreatedBy = _createdBy
                },
                cancellationToken: cancellationToken));

            result[code] = id;
        }

        return result;
    }

    private static async Task<Dictionary<string, Guid>> SeedCapabilityTypesAsync(
        System.Data.Common.DbConnection connection,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var types = new[]
        {
            ("SUBJECT_INSTRUCTION", "Subject Area Instruction", "Direct teaching in academic curriculum subjects"),
            ("STAGE_LEVEL", "Grade / Educational Stage", "Pedagogical specialization by developmental phase"),
            ("DELIVERY_MODE", "Delivery Mode", "Delivery format methodology (face-to-face, blended, laboratory)"),
            ("LANGUAGE_INSTRUCTION", "Language of Instruction", "Language capability for curriculum delivery"),
            ("SPECIALIST_ACTIVITY", "Specialist Activity", "Specialized co-curricular, athletics, or arts coaching")
        };

        const string sql = """
            IF NOT EXISTS (SELECT 1 FROM teaching.capability_types WHERE code = @Code)
            BEGIN
                INSERT INTO teaching.capability_types (
                    id, code, name, description, is_active, created_at, created_by
                ) VALUES (
                    @Id, @Code, @Name, @Description, 1, @CreatedAt, @CreatedBy
                );
            END;
            SELECT TOP (1) id FROM teaching.capability_types WHERE code = @Code;
            """;

        var result = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (var (code, name, description) in types)
        {
            var id = await connection.ExecuteScalarAsync<Guid>(new CommandDefinition(
                sql,
                new
                {
                    Id = Guid.NewGuid(),
                    Code = code,
                    Name = name,
                    Description = description,
                    CreatedAt = now,
                    CreatedBy = _createdBy
                },
                cancellationToken: cancellationToken));

            result[code] = id;
        }

        return result;
    }

    private static async Task<Dictionary<string, Guid>> SeedQualificationTypesAsync(
        System.Data.Common.DbConnection connection,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        var types = new (string Code, string Name, string Description, bool RequiresRenewal, int? DefaultValidityMonths)[]
        {
            ("SERDIK", "Sertifikat Pendidik (Serdik)", "National Teacher Professional Certification issued by Kemdikbudristek", false, null),
            ("IJAZAH_S1", "Ijazah Sarjana (S1)", "Bachelor's Degree in Education or Academic Discipline", false, null),
            ("IJAZAH_S2", "Ijazah Magister (S2)", "Master's Degree in Education or Academic Field", false, null),
            ("CAMBRIDGE_CELTA", "Cambridge CELTA", "Certificate in Teaching English to Speakers of Other Languages", false, null),
            ("COACHING_CERT", "Sertifikat Pelatihan Pedagogik / Coaching", "Specialized pedagogical or sports coaching certification", true, 24),
            ("CHILD_PROTECTION", "Child Protection & Safeguarding", "Mandatory institutional child safeguarding credential", true, 12)
        };

        const string sql = """
            IF NOT EXISTS (SELECT 1 FROM teaching.qualification_types WHERE code = @Code)
            BEGIN
                INSERT INTO teaching.qualification_types (
                    id, code, name, description, requires_renewal, default_validity_months, is_active, created_at, created_by
                ) VALUES (
                    @Id, @Code, @Name, @Description, @RequiresRenewal, @DefaultValidityMonths, 1, @CreatedAt, @CreatedBy
                );
            END;
            SELECT TOP (1) id FROM teaching.qualification_types WHERE code = @Code;
            """;

        var result = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach (var (code, name, description, reqRenewal, valMonths) in types)
        {
            var id = await connection.ExecuteScalarAsync<Guid>(new CommandDefinition(
                sql,
                new
                {
                    Id = Guid.NewGuid(),
                    Code = code,
                    Name = name,
                    Description = description,
                    RequiresRenewal = reqRenewal,
                    DefaultValidityMonths = valMonths,
                    CreatedAt = now,
                    CreatedBy = _createdBy
                },
                cancellationToken: cancellationToken));

            result[code] = id;
        }

        return result;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Persona 1: Budi Hartono, M.Pd.
    // ─────────────────────────────────────────────────────────────────────────

    private static async Task SeedPersona1BudiHartonoAsync(
        System.Data.Common.DbConnection connection,
        DateTime baseDate,
        DateTimeOffset now,
        Guid roleId,
        IReadOnlyDictionary<string, Guid> capTypeMap,
        IReadOnlyDictionary<string, Guid> qualTypeMap,
        CancellationToken cancellationToken)
    {
        var teacherId = DemoDataConstants.BudiHartonoTeacherId;
        var effectiveFrom = baseDate.AddYears(-5);

        // 1. Teacher Profile
        const string insertProfileSql = """
            IF NOT EXISTS (SELECT 1 FROM teaching.teacher_profiles WHERE id = @Id OR teacher_number = @TeacherNumber)
            BEGIN
                INSERT INTO teaching.teacher_profiles (
                    id, teacher_number, full_name, display_name, employee_ref, national_id_or_external_ref,
                    email, phone, teaching_role_id, campus_affiliation, department, standing,
                    effective_from, effective_to, standing_reason, approved_by, approved_date,
                    notes, created_at, created_by
                ) VALUES (
                    @Id, @TeacherNumber, @FullName, @DisplayName, @EmployeeRef, @NationalId,
                    @Email, @Phone, @TeachingRoleId, @CampusAffiliation, @Department, 'active',
                    @EffectiveFrom, NULL, @StandingReason, @ApprovedBy, @ApprovedDate,
                    @Notes, @CreatedAt, @CreatedBy
                );
            END;
            """;

        await connection.ExecuteAsync(new CommandDefinition(
            insertProfileSql,
            new
            {
                Id = teacherId,
                TeacherNumber = "TCH-2026-0012",
                FullName = "Budi Hartono",
                DisplayName = "Budi Hartono, M.Pd.",
                EmployeeRef = "EMP-00101",
                NationalId = "3273010101750001",
                Email = "budi.hartono@hagios.sch.id",
                Phone = "+6281234567890",
                TeachingRoleId = roleId,
                CampusAffiliation = _campusMain,
                Department = "Matematika & IPA",
                EffectiveFrom = effectiveFrom,
                StandingReason = "Guru Tetap senior Hagios School of Life dengan sertifikasi pendidik penuh.",
                ApprovedBy = _kepalaSekolah,
                ApprovedDate = effectiveFrom,
                Notes = "Senior Lead Teacher in Mathematics and Natural Sciences.",
                CreatedAt = now,
                CreatedBy = _createdBy
            },
            cancellationToken: cancellationToken));

        // 2. Capabilities: Matematika SD & IPA SD
        const string insertCapSql = """
            IF NOT EXISTS (SELECT 1 FROM teaching.teaching_capabilities WHERE id = @Id)
            BEGIN
                INSERT INTO teaching.teaching_capabilities (
                    id, teacher_profile_id, capability_type_id, capability_code, capability_name,
                    applicable_level, delivery_mode, language_of_instruction, status, notes,
                    created_at, created_by
                ) VALUES (
                    @Id, @TeacherProfileId, @CapabilityTypeId, @CapabilityCode, @CapabilityName,
                    @ApplicableLevel, @DeliveryMode, @Language, 'active', @Notes,
                    @CreatedAt, @CreatedBy
                );
            END;
            """;

        await connection.ExecuteAsync(new CommandDefinition(
            insertCapSql,
            new
            {
                Id = _budiMathCapabilityId,
                TeacherProfileId = teacherId,
                CapabilityTypeId = capTypeMap["SUBJECT_INSTRUCTION"],
                CapabilityCode = "MATH_PRIMARY",
                CapabilityName = "Matematika SD",
                ApplicableLevel = "SD Kelas 4-6",
                DeliveryMode = "face_to_face",
                Language = "id",
                Notes = "Spesialisasi pengajaran matematika tingkat dasar lanjut.",
                CreatedAt = now,
                CreatedBy = _createdBy
            },
            cancellationToken: cancellationToken));

        await connection.ExecuteAsync(new CommandDefinition(
            insertCapSql,
            new
            {
                Id = _budiIpaCapabilityId,
                TeacherProfileId = teacherId,
                CapabilityTypeId = capTypeMap["SUBJECT_INSTRUCTION"],
                CapabilityCode = "IPA_PRIMARY",
                CapabilityName = "IPA SD",
                ApplicableLevel = "SD Kelas 4-6",
                DeliveryMode = "face_to_face",
                Language = "id",
                Notes = "Ilmu Pengetahuan Alam eksperimental dan pembelajaran kontekstual.",
                CreatedAt = now,
                CreatedBy = _createdBy
            },
            cancellationToken: cancellationToken));

        // 3. Qualification Evidence: Serdik & S1 (Verified)
        const string insertEvidenceSql = """
            IF NOT EXISTS (SELECT 1 FROM teaching.qualification_evidence WHERE id = @Id)
            BEGIN
                INSERT INTO teaching.qualification_evidence (
                    id, teacher_profile_id, qualification_type_id, title, issuing_authority,
                    credential_identifier, issue_date, expiry_date, document_reference,
                    verification_state, verified_by, verified_date, verification_notes,
                    created_at, created_by
                ) VALUES (
                    @Id, @TeacherProfileId, @QualificationTypeId, @Title, @IssuingAuthority,
                    @CredentialIdentifier, @IssueDate, @ExpiryDate, @DocumentRef,
                    @VerificationState, @VerifiedBy, @VerifiedDate, @VerificationNotes,
                    @CreatedAt, @CreatedBy
                );
            END;
            """;

        await connection.ExecuteAsync(new CommandDefinition(
            insertEvidenceSql,
            new
            {
                Id = _budiSerdikEvidenceId,
                TeacherProfileId = teacherId,
                QualificationTypeId = qualTypeMap["SERDIK"],
                Title = "Sertifikat Pendidik Matematika SD/SMP",
                IssuingAuthority = "Kementerian Pendidikan, Kebudayaan, Riset, dan Teknologi",
                CredentialIdentifier = "SERDIK-2020-098234",
                IssueDate = baseDate.AddYears(-4),
                ExpiryDate = (DateTime?)null,
                DocumentRef = "docs/credentials/budi_hartono_serdik.pdf",
                VerificationState = "verified",
                VerifiedBy = _kepalaSekolah,
                VerifiedDate = (DateTime?)baseDate.AddYears(-4),
                VerificationNotes = "Keabsahan sertifikat pendidik diverifikasi via pangkalan data Dikti/Kemdikbud.",
                CreatedAt = now,
                CreatedBy = _createdBy
            },
            cancellationToken: cancellationToken));

        await connection.ExecuteAsync(new CommandDefinition(
            insertEvidenceSql,
            new
            {
                Id = _budiDegreeEvidenceId,
                TeacherProfileId = teacherId,
                QualificationTypeId = qualTypeMap["IJAZAH_S1"],
                Title = "Sarjana Pendidikan Matematika",
                IssuingAuthority = "Universitas Pendidikan Indonesia (UPI)",
                CredentialIdentifier = "UPI-MAT-2005-0412",
                IssueDate = baseDate.AddYears(-15),
                ExpiryDate = (DateTime?)null,
                DocumentRef = "docs/credentials/budi_hartono_s1.pdf",
                VerificationState = "verified",
                VerifiedBy = _kepalaSekolah,
                VerifiedDate = (DateTime?)baseDate.AddYears(-5),
                VerificationNotes = "Ijazah S1 terverifikasi melalui PDDIKTI.",
                CreatedAt = now,
                CreatedBy = _createdBy
            },
            cancellationToken: cancellationToken));

        // 4. Teaching Authorization: Active for Matematika SD Kelas 4-6
        const string insertAuthSql = """
            IF NOT EXISTS (SELECT 1 FROM teaching.teaching_authorizations WHERE id = @Id OR authorization_number = @AuthNumber)
            BEGIN
                INSERT INTO teaching.teaching_authorizations (
                    id, authorization_number, teacher_profile_id, authorized_capability_id,
                    scope_description, campus_scope, program_scope, level_scope, delivery_mode_scope,
                    status, unverified_credential_warning, effective_start_date, effective_end_date,
                    decision_authority, approved_date, notes, created_at, created_by
                ) VALUES (
                    @Id, @AuthNumber, @TeacherProfileId, @AuthorizedCapabilityId,
                    @ScopeDescription, @CampusScope, @ProgramScope, @LevelScope, @DeliveryModeScope,
                    @Status, @UnverifiedWarning, @EffectiveStartDate, @EffectiveEndDate,
                    @DecisionAuthority, @ApprovedDate, @Notes, @CreatedAt, @CreatedBy
                );
            END;
            """;

        await connection.ExecuteAsync(new CommandDefinition(
            insertAuthSql,
            new
            {
                Id = _budiAuthorizationId,
                AuthNumber = "AUTH-2026-0045",
                TeacherProfileId = teacherId,
                AuthorizedCapabilityId = _budiMathCapabilityId,
                ScopeDescription = "Matematika SD Kelas 4–6 Tatap Muka",
                CampusScope = _campusMain,
                ProgramScope = "SD",
                LevelScope = "Kelas 4-6",
                DeliveryModeScope = "face_to_face",
                Status = "active",
                UnverifiedWarning = false,
                EffectiveStartDate = baseDate.AddMonths(-6),
                EffectiveEndDate = (DateTime?)baseDate.AddMonths(12),
                DecisionAuthority = _kepalaSekolah,
                ApprovedDate = baseDate.AddMonths(-6),
                Notes = "Otorisasi pengajaran penuh mata pelajaran Matematika jenjang SD.",
                CreatedAt = now,
                CreatedBy = _createdBy
            },
            cancellationToken: cancellationToken));

        // 5. Junction: Authorization Evidence
        const string insertAuthEvSql = """
            IF NOT EXISTS (SELECT 1 FROM teaching.authorization_evidence WHERE authorization_id = @AuthId AND qualification_evidence_id = @EvidenceId)
            BEGIN
                INSERT INTO teaching.authorization_evidence (
                    authorization_id, qualification_evidence_id, notes, created_at, created_by
                ) VALUES (
                    @AuthId, @EvidenceId, @Notes, @CreatedAt, @CreatedBy
                );
            END;
            """;

        await connection.ExecuteAsync(new CommandDefinition(
            insertAuthEvSql,
            new { AuthId = _budiAuthorizationId, EvidenceId = _budiSerdikEvidenceId, Notes = "Supporting Serdik credential", CreatedAt = now, CreatedBy = _createdBy },
            cancellationToken: cancellationToken));

        await connection.ExecuteAsync(new CommandDefinition(
            insertAuthEvSql,
            new { AuthId = _budiAuthorizationId, EvidenceId = _budiDegreeEvidenceId, Notes = "Supporting S1 degree credential", CreatedAt = now, CreatedBy = _createdBy },
            cancellationToken: cancellationToken));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Persona 2: Siti Nurhaliza, S.Pd.
    // ─────────────────────────────────────────────────────────────────────────

    private static async Task SeedPersona2SitiNurhalizaAsync(
        System.Data.Common.DbConnection connection,
        DateTime baseDate,
        DateTimeOffset now,
        Guid roleId,
        IReadOnlyDictionary<string, Guid> capTypeMap,
        IReadOnlyDictionary<string, Guid> qualTypeMap,
        CancellationToken cancellationToken)
    {
        var teacherId = DemoDataConstants.SitiNurhalizaTeacherId;
        var effectiveFrom = baseDate.AddMonths(-6);

        // 1. Teacher Profile
        const string insertProfileSql = """
            IF NOT EXISTS (SELECT 1 FROM teaching.teacher_profiles WHERE id = @Id OR teacher_number = @TeacherNumber)
            BEGIN
                INSERT INTO teaching.teacher_profiles (
                    id, teacher_number, full_name, display_name, employee_ref, national_id_or_external_ref,
                    email, phone, teaching_role_id, campus_affiliation, department, standing,
                    effective_from, effective_to, standing_reason, approved_by, approved_date,
                    notes, created_at, created_by
                ) VALUES (
                    @Id, @TeacherNumber, @FullName, @DisplayName, @EmployeeRef, @NationalId,
                    @Email, @Phone, @TeachingRoleId, @CampusAffiliation, @Department, 'active',
                    @EffectiveFrom, NULL, @StandingReason, @ApprovedBy, @ApprovedDate,
                    @Notes, @CreatedAt, @CreatedBy
                );
            END;
            """;

        await connection.ExecuteAsync(new CommandDefinition(
            insertProfileSql,
            new
            {
                Id = teacherId,
                TeacherNumber = "TCH-2026-0013",
                FullName = "Siti Nurhaliza",
                DisplayName = "Siti Nurhaliza, S.Pd.",
                EmployeeRef = "EMP-00102",
                NationalId = "3273010202920002",
                Email = "siti.nurhaliza@hagios.sch.id",
                Phone = "+6281298765432",
                TeachingRoleId = roleId,
                CampusAffiliation = _campusMain,
                Department = "Bahasa",
                EffectiveFrom = effectiveFrom,
                StandingReason = "Guru Kontrak Bahasa Inggris masa kontrak aktif.",
                ApprovedBy = _kepalaSekolah,
                ApprovedDate = effectiveFrom,
                Notes = "Contract English Teacher under mentoring program.",
                CreatedAt = now,
                CreatedBy = _createdBy
            },
            cancellationToken: cancellationToken));

        // 2. Capabilities: Bahasa Inggris SMP
        const string insertCapSql = """
            IF NOT EXISTS (SELECT 1 FROM teaching.teaching_capabilities WHERE id = @Id)
            BEGIN
                INSERT INTO teaching.teaching_capabilities (
                    id, teacher_profile_id, capability_type_id, capability_code, capability_name,
                    applicable_level, delivery_mode, language_of_instruction, status, notes,
                    created_at, created_by
                ) VALUES (
                    @Id, @TeacherProfileId, @CapabilityTypeId, @CapabilityCode, @CapabilityName,
                    @ApplicableLevel, @DeliveryMode, @Language, 'active', @Notes,
                    @CreatedAt, @CreatedBy
                );
            END;
            """;

        await connection.ExecuteAsync(new CommandDefinition(
            insertCapSql,
            new
            {
                Id = _sitiEngCapabilityId,
                TeacherProfileId = teacherId,
                CapabilityTypeId = capTypeMap["SUBJECT_INSTRUCTION"],
                CapabilityCode = "ENG_JUNIOR",
                CapabilityName = "Bahasa Inggris SMP",
                ApplicableLevel = "SMP Kelas 7-9",
                DeliveryMode = "face_to_face",
                Language = "en",
                Notes = "Pengajaran kurikulum Bahasa Inggris menengah pertama.",
                CreatedAt = now,
                CreatedBy = _createdBy
            },
            cancellationToken: cancellationToken));

        // 3. Qualification Evidence: Submitted S1 (awaiting verification)
        const string insertEvidenceSql = """
            IF NOT EXISTS (SELECT 1 FROM teaching.qualification_evidence WHERE id = @Id)
            BEGIN
                INSERT INTO teaching.qualification_evidence (
                    id, teacher_profile_id, qualification_type_id, title, issuing_authority,
                    credential_identifier, issue_date, expiry_date, document_reference,
                    verification_state, verified_by, verified_date, verification_notes,
                    created_at, created_by
                ) VALUES (
                    @Id, @TeacherProfileId, @QualificationTypeId, @Title, @IssuingAuthority,
                    @CredentialIdentifier, @IssueDate, @ExpiryDate, @DocumentRef,
                    @VerificationState, NULL, NULL, @VerificationNotes,
                    @CreatedAt, @CreatedBy
                );
            END;
            """;

        await connection.ExecuteAsync(new CommandDefinition(
            insertEvidenceSql,
            new
            {
                Id = _sitiDegreeEvidenceId,
                TeacherProfileId = teacherId,
                QualificationTypeId = qualTypeMap["IJAZAH_S1"],
                Title = "Sarjana Pendidikan Bahasa Inggris",
                IssuingAuthority = "Universitas Negeri Jakarta (UNJ)",
                CredentialIdentifier = "UNJ-ENG-2022-7719",
                IssueDate = baseDate.AddYears(-2),
                ExpiryDate = (DateTime?)null,
                DocumentRef = "docs/credentials/siti_nurhaliza_s1.pdf",
                VerificationState = "submitted",
                VerificationNotes = "Dokumen ijazah asli telah diunggah dan dalam antrean verifikasi.",
                CreatedAt = now,
                CreatedBy = _createdBy
            },
            cancellationToken: cancellationToken));

        // 4. Teaching Authorization: Authorized under supervision (with unverified credential warning = true)
        const string insertAuthSql = """
            IF NOT EXISTS (SELECT 1 FROM teaching.teaching_authorizations WHERE id = @Id OR authorization_number = @AuthNumber)
            BEGIN
                INSERT INTO teaching.teaching_authorizations (
                    id, authorization_number, teacher_profile_id, authorized_capability_id,
                    scope_description, campus_scope, program_scope, level_scope, delivery_mode_scope,
                    status, unverified_credential_warning, effective_start_date, effective_end_date,
                    conditions_or_restrictions, decision_authority, approved_date, notes,
                    created_at, created_by
                ) VALUES (
                    @Id, @AuthNumber, @TeacherProfileId, @AuthorizedCapabilityId,
                    @ScopeDescription, @CampusScope, @ProgramScope, @LevelScope, @DeliveryModeScope,
                    @Status, @UnverifiedWarning, @EffectiveStartDate, @EffectiveEndDate,
                    @Conditions, @DecisionAuthority, @ApprovedDate, @Notes,
                    @CreatedAt, @CreatedBy
                );
            END;
            """;

        await connection.ExecuteAsync(new CommandDefinition(
            insertAuthSql,
            new
            {
                Id = _sitiAuthorizationId,
                AuthNumber = "AUTH-2026-0046",
                TeacherProfileId = teacherId,
                AuthorizedCapabilityId = _sitiEngCapabilityId,
                ScopeDescription = "Bahasa Inggris SMP Kelas 7–9 Tatap Muka (Pendampingan Mentor)",
                CampusScope = _campusMain,
                ProgramScope = "SMP",
                LevelScope = "Kelas 7-9",
                DeliveryModeScope = "face_to_face",
                Status = "authorized_under_supervision",
                UnverifiedWarning = true,
                EffectiveStartDate = baseDate.AddMonths(-3),
                EffectiveEndDate = (DateTime?)baseDate.AddMonths(9),
                Conditions = "Wajib didampingi guru senior (Budi Hartono, M.Pd.) dalam kerangka co-teaching.",
                DecisionAuthority = _kepalaSekolah,
                ApprovedDate = baseDate.AddMonths(-3),
                Notes = "Otorisasi bersyarat di bawah supervisi mentor guru senior.",
                CreatedAt = now,
                CreatedBy = _createdBy
            },
            cancellationToken: cancellationToken));

        // 5. Junction: Authorization Evidence
        const string insertAuthEvSql = """
            IF NOT EXISTS (SELECT 1 FROM teaching.authorization_evidence WHERE authorization_id = @AuthId AND qualification_evidence_id = @EvidenceId)
            BEGIN
                INSERT INTO teaching.authorization_evidence (
                    authorization_id, qualification_evidence_id, notes, created_at, created_by
                ) VALUES (
                    @AuthId, @EvidenceId, @Notes, @CreatedAt, @CreatedBy
                );
            END;
            """;

        await connection.ExecuteAsync(new CommandDefinition(
            insertAuthEvSql,
            new { AuthId = _sitiAuthorizationId, EvidenceId = _sitiDegreeEvidenceId, Notes = "Submitted S1 credential awaiting verification", CreatedAt = now, CreatedBy = _createdBy },
            cancellationToken: cancellationToken));

        // 6. Supervision Requirement: Co-Teaching with Budi Hartono
        const string insertSupSql = """
            IF NOT EXISTS (SELECT 1 FROM teaching.supervision_requirements WHERE id = @Id)
            BEGIN
                INSERT INTO teaching.supervision_requirements (
                    id, authorization_id, supervising_teacher_id, supervision_type, description,
                    start_date, end_date, status, resolution_notes, created_at, created_by
                ) VALUES (
                    @Id, @AuthorizationId, @SupervisingTeacherId, @SupervisionType, @Description,
                    @StartDate, @EndDate, 'active', NULL, @CreatedAt, @CreatedBy
                );
            END;
            """;

        await connection.ExecuteAsync(new CommandDefinition(
            insertSupSql,
            new
            {
                Id = _sitiSupervisionReqId,
                AuthorizationId = _sitiAuthorizationId,
                SupervisingTeacherId = (Guid?)DemoDataConstants.BudiHartonoTeacherId,
                SupervisionType = "co_teaching",
                Description = "Co-teaching mentoring untuk guru baru dalam kurikulum Bahasa Inggris Hagios SMP.",
                StartDate = baseDate.AddMonths(-3),
                EndDate = (DateTime?)baseDate.AddMonths(3),
                CreatedAt = now,
                CreatedBy = _createdBy
            },
            cancellationToken: cancellationToken));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Persona 3: John Miller, B.Ed.
    // ─────────────────────────────────────────────────────────────────────────

    private static async Task SeedPersona3JohnMillerAsync(
        System.Data.Common.DbConnection connection,
        DateTime baseDate,
        DateTimeOffset now,
        Guid roleId,
        IReadOnlyDictionary<string, Guid> capTypeMap,
        IReadOnlyDictionary<string, Guid> qualTypeMap,
        CancellationToken cancellationToken)
    {
        var teacherId = DemoDataConstants.JohnMillerTeacherId;
        var effectiveFrom = baseDate.AddMonths(-2);

        // 1. Teacher Profile (null employee_ref, passport national_id_or_external_ref)
        const string insertProfileSql = """
            IF NOT EXISTS (SELECT 1 FROM teaching.teacher_profiles WHERE id = @Id OR teacher_number = @TeacherNumber)
            BEGIN
                INSERT INTO teaching.teacher_profiles (
                    id, teacher_number, full_name, display_name, employee_ref, national_id_or_external_ref,
                    email, phone, teaching_role_id, campus_affiliation, department, standing,
                    effective_from, effective_to, standing_reason, approved_by, approved_date,
                    notes, created_at, created_by
                ) VALUES (
                    @Id, @TeacherNumber, @FullName, @DisplayName, NULL, @NationalId,
                    @Email, @Phone, @TeachingRoleId, @CampusAffiliation, @Department, 'active',
                    @EffectiveFrom, @EffectiveTo, @StandingReason, @ApprovedBy, @ApprovedDate,
                    @Notes, @CreatedAt, @CreatedBy
                );
            END;
            """;

        await connection.ExecuteAsync(new CommandDefinition(
            insertProfileSql,
            new
            {
                Id = teacherId,
                TeacherNumber = "TCH-2026-0014",
                FullName = "John Miller",
                DisplayName = "John Miller, B.Ed.",
                NationalId = "P-USA-8849201",
                Email = "john.miller@external.hagios.sch.id",
                Phone = "+15551234567",
                TeachingRoleId = roleId,
                CampusAffiliation = _campusMain,
                Department = "Bahasa & Program Internasional",
                EffectiveFrom = effectiveFrom,
                EffectiveTo = (DateTime?)baseDate.AddMonths(10),
                StandingReason = "Dosen/Instruktur Tamu program imersi bahasa internasional.",
                ApprovedBy = _kepalaSekolah,
                ApprovedDate = effectiveFrom,
                Notes = "Visiting faculty expert for International English Immersion.",
                CreatedAt = now,
                CreatedBy = _createdBy
            },
            cancellationToken: cancellationToken));

        // 2. Capabilities: English Immersion
        const string insertCapSql = """
            IF NOT EXISTS (SELECT 1 FROM teaching.teaching_capabilities WHERE id = @Id)
            BEGIN
                INSERT INTO teaching.teaching_capabilities (
                    id, teacher_profile_id, capability_type_id, capability_code, capability_name,
                    applicable_level, delivery_mode, language_of_instruction, status, notes,
                    created_at, created_by
                ) VALUES (
                    @Id, @TeacherProfileId, @CapabilityTypeId, @CapabilityCode, @CapabilityName,
                    @ApplicableLevel, @DeliveryMode, @Language, 'active', @Notes,
                    @CreatedAt, @CreatedBy
                );
            END;
            """;

        await connection.ExecuteAsync(new CommandDefinition(
            insertCapSql,
            new
            {
                Id = _johnImmersionCapabilityId,
                TeacherProfileId = teacherId,
                CapabilityTypeId = capTypeMap["SUBJECT_INSTRUCTION"],
                CapabilityCode = "ENG_IMMERSION",
                CapabilityName = "English Immersion",
                ApplicableLevel = "SMP/SMA",
                DeliveryMode = "face_to_face",
                Language = "en",
                Notes = "Native English speaker immersion and debate training.",
                CreatedAt = now,
                CreatedBy = _createdBy
            },
            cancellationToken: cancellationToken));

        // 3. Qualification Evidence: Cambridge CELTA (Verified)
        const string insertEvidenceSql = """
            IF NOT EXISTS (SELECT 1 FROM teaching.qualification_evidence WHERE id = @Id)
            BEGIN
                INSERT INTO teaching.qualification_evidence (
                    id, teacher_profile_id, qualification_type_id, title, issuing_authority,
                    credential_identifier, issue_date, expiry_date, document_reference,
                    verification_state, verified_by, verified_date, verification_notes,
                    created_at, created_by
                ) VALUES (
                    @Id, @TeacherProfileId, @QualificationTypeId, @Title, @IssuingAuthority,
                    @CredentialIdentifier, @IssueDate, @ExpiryDate, @DocumentRef,
                    @VerificationState, @VerifiedBy, @VerifiedDate, @VerificationNotes,
                    @CreatedAt, @CreatedBy
                );
            END;
            """;

        await connection.ExecuteAsync(new CommandDefinition(
            insertEvidenceSql,
            new
            {
                Id = _johnCeltaEvidenceId,
                TeacherProfileId = teacherId,
                QualificationTypeId = qualTypeMap["CAMBRIDGE_CELTA"],
                Title = "Certificate in English Language Teaching to Adults (CELTA)",
                IssuingAuthority = "Cambridge Assessment English",
                CredentialIdentifier = "CELTA-LON-2018-9941",
                IssueDate = baseDate.AddYears(-6),
                ExpiryDate = (DateTime?)null,
                DocumentRef = "docs/credentials/john_miller_celta.pdf",
                VerificationState = "verified",
                VerifiedBy = _kepalaSekolah,
                VerifiedDate = (DateTime?)baseDate.AddMonths(-2),
                VerificationNotes = "Verified against Cambridge Assessment online verification portal.",
                CreatedAt = now,
                CreatedBy = _createdBy
            },
            cancellationToken: cancellationToken));

        // 4. Teaching Authorization: Guest Lecture / Immersion
        const string insertAuthSql = """
            IF NOT EXISTS (SELECT 1 FROM teaching.teaching_authorizations WHERE id = @Id OR authorization_number = @AuthNumber)
            BEGIN
                INSERT INTO teaching.teaching_authorizations (
                    id, authorization_number, teacher_profile_id, authorized_capability_id,
                    scope_description, campus_scope, program_scope, level_scope, delivery_mode_scope,
                    status, unverified_credential_warning, effective_start_date, effective_end_date,
                    decision_authority, approved_date, notes, created_at, created_by
                ) VALUES (
                    @Id, @AuthNumber, @TeacherProfileId, @AuthorizedCapabilityId,
                    @ScopeDescription, @CampusScope, @ProgramScope, @LevelScope, @DeliveryModeScope,
                    @Status, @UnverifiedWarning, @EffectiveStartDate, @EffectiveEndDate,
                    @DecisionAuthority, @ApprovedDate, @Notes, @CreatedAt, @CreatedBy
                );
            END;
            """;

        await connection.ExecuteAsync(new CommandDefinition(
            insertAuthSql,
            new
            {
                Id = _johnAuthorizationId,
                AuthNumber = "AUTH-2026-0047",
                TeacherProfileId = teacherId,
                AuthorizedCapabilityId = _johnImmersionCapabilityId,
                ScopeDescription = "Kuliah Tamu dan English Immersion Workshop",
                CampusScope = _campusMain,
                ProgramScope = "SMP/SMA",
                LevelScope = "Kelas 7-12",
                DeliveryModeScope = "face_to_face",
                Status = "active",
                UnverifiedWarning = false,
                EffectiveStartDate = baseDate.AddMonths(-2),
                EffectiveEndDate = (DateTime?)baseDate.AddMonths(10),
                DecisionAuthority = _kepalaSekolah,
                ApprovedDate = baseDate.AddMonths(-2),
                Notes = "Otorisasi pengajaran tamu imersi bahasa internasional.",
                CreatedAt = now,
                CreatedBy = _createdBy
            },
            cancellationToken: cancellationToken));

        // 5. Junction: Authorization Evidence
        const string insertAuthEvSql = """
            IF NOT EXISTS (SELECT 1 FROM teaching.authorization_evidence WHERE authorization_id = @AuthId AND qualification_evidence_id = @EvidenceId)
            BEGIN
                INSERT INTO teaching.authorization_evidence (
                    authorization_id, qualification_evidence_id, notes, created_at, created_by
                ) VALUES (
                    @AuthId, @EvidenceId, @Notes, @CreatedAt, @CreatedBy
                );
            END;
            """;

        await connection.ExecuteAsync(new CommandDefinition(
            insertAuthEvSql,
            new { AuthId = _johnAuthorizationId, EvidenceId = _johnCeltaEvidenceId, Notes = "Supporting Cambridge CELTA credential", CreatedAt = now, CreatedBy = _createdBy },
            cancellationToken: cancellationToken));
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Persona 4: Hendra Setiawan (Pending Candidate)
    // ─────────────────────────────────────────────────────────────────────────

    private static async Task SeedPersona4HendraSetiawanAsync(
        System.Data.Common.DbConnection connection,
        DateTime baseDate,
        DateTimeOffset now,
        Guid roleId,
        IReadOnlyDictionary<string, Guid> qualTypeMap,
        CancellationToken cancellationToken)
    {
        var teacherId = DemoDataConstants.HendraSetiawanTeacherId;

        // 1. Teacher Profile: pending_approval standing, null approved_by/approved_date
        const string insertProfileSql = """
            IF NOT EXISTS (SELECT 1 FROM teaching.teacher_profiles WHERE id = @Id OR teacher_number = @TeacherNumber)
            BEGIN
                INSERT INTO teaching.teacher_profiles (
                    id, teacher_number, full_name, display_name, employee_ref, national_id_or_external_ref,
                    email, phone, teaching_role_id, campus_affiliation, department, standing,
                    effective_from, effective_to, standing_reason, approved_by, approved_date,
                    notes, created_at, created_by
                ) VALUES (
                    @Id, @TeacherNumber, @FullName, @DisplayName, @EmployeeRef, @NationalId,
                    @Email, @Phone, @TeachingRoleId, @CampusAffiliation, @Department, 'pending_approval',
                    @EffectiveFrom, NULL, @StandingReason, NULL, NULL,
                    @Notes, @CreatedAt, @CreatedBy
                );
            END;
            """;

        await connection.ExecuteAsync(new CommandDefinition(
            insertProfileSql,
            new
            {
                Id = teacherId,
                TeacherNumber = "TCH-2026-0015",
                FullName = "Hendra Setiawan",
                DisplayName = "Hendra Setiawan",
                EmployeeRef = "EMP-00104",
                NationalId = "3273010303880004",
                Email = "hendra.setiawan@hagios.sch.id",
                Phone = "+6281355577788",
                TeachingRoleId = roleId,
                CampusAffiliation = _campusMain,
                Department = "Kesiswaan & Ekstrakurikuler",
                EffectiveFrom = baseDate,
                StandingReason = "Kandidat instruktur ekstrakurikuler dalam proses rekrutmen dan verifikasi kelengkapan berkas.",
                Notes = "Prospective coaching faculty candidate awaiting onboarding review.",
                CreatedAt = now,
                CreatedBy = _createdBy
            },
            cancellationToken: cancellationToken));

        // 2. Qualification Evidence: Submitted Coaching credential (awaiting verification)
        const string insertEvidenceSql = """
            IF NOT EXISTS (SELECT 1 FROM teaching.qualification_evidence WHERE id = @Id)
            BEGIN
                INSERT INTO teaching.qualification_evidence (
                    id, teacher_profile_id, qualification_type_id, title, issuing_authority,
                    credential_identifier, issue_date, expiry_date, document_reference,
                    verification_state, verified_by, verified_date, verification_notes,
                    created_at, created_by
                ) VALUES (
                    @Id, @TeacherProfileId, @QualificationTypeId, @Title, @IssuingAuthority,
                    @CredentialIdentifier, @IssueDate, @ExpiryDate, @DocumentRef,
                    @VerificationState, NULL, NULL, @VerificationNotes,
                    @CreatedAt, @CreatedBy
                );
            END;
            """;

        await connection.ExecuteAsync(new CommandDefinition(
            insertEvidenceSql,
            new
            {
                Id = _hendraCoachingEvidenceId,
                TeacherProfileId = teacherId,
                QualificationTypeId = qualTypeMap["COACHING_CERT"],
                Title = "Sertifikasi Pelatih Olahraga & Kepanduan Nasional",
                IssuingAuthority = "Lembaga Sertifikasi Profesi Olahraga Indonesia",
                CredentialIdentifier = "COACH-2024-5512",
                IssueDate = baseDate.AddYears(-1),
                ExpiryDate = (DateTime?)baseDate.AddYears(1),
                DocumentRef = "docs/credentials/hendra_setiawan_coach.pdf",
                VerificationState = "submitted",
                VerificationNotes = "Dokumen sertifikat baru diserahkan pelamar; menunggu verifikasi komite.",
                CreatedAt = now,
                CreatedBy = _createdBy
            },
            cancellationToken: cancellationToken));

        // Persona 4 has zero authorizations.
    }
}
