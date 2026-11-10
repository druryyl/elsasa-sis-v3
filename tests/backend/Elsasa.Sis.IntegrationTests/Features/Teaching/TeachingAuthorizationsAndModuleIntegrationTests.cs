namespace Elsasa.Sis.IntegrationTests.Features.Teaching;

using System.Net;
using System.Net.Http.Json;
using Dapper;
using Elsasa.Sis.Contracts.DemoData;
using Elsasa.Sis.Contracts.Teaching;
using Elsasa.Sis.IntegrationTests.Fixtures;
using Elsasa.Sis.Teaching;
using Elsasa.Sis.Teaching.Features.QualificationEvidence.SubmitQualificationEvidence;
using Elsasa.Sis.Teaching.Features.QualificationEvidence.VerifyQualificationEvidence;
using Elsasa.Sis.Teaching.Features.RecordCorrections.ApplyRecordCorrection;
using Elsasa.Sis.Teaching.Features.TeacherProfiles.ApproveTeacherProfile;
using Elsasa.Sis.Teaching.Features.TeacherProfiles.RegisterTeacherProfile;
using Elsasa.Sis.Teaching.Features.TeachingAuthorizations.AddSupervisionRequirement;
using Elsasa.Sis.Teaching.Features.TeachingAuthorizations.GrantTeachingAuthorization;
using Elsasa.Sis.Teaching.Features.TeachingAuthorizations.ResolveSupervisionRequirement;
using Elsasa.Sis.Teaching.Features.TeachingAuthorizations.UpdateTeachingAuthorizationStatus;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

/// <summary>
/// Integration tests verifying teaching authorization granting, permissive warnings, supervision enforcement,
/// auditable record corrections, cross-domain ITeachingModule inquiry contract, and demo seeding/purging cycle (P6-S20, TD-05..TD-09).
/// </summary>
public sealed class TeachingAuthorizationsAndModuleIntegrationTests(IntegrationTestWebAppFactory factory)
    : BaseIntegrationTest(factory)
{
    private const string _teachersUri = "/api/v1/teaching/teachers";
    private const string _qualificationsUri = "/api/v1/teaching/qualifications";
    private const string _authorizationsUri = "/api/v1/teaching/authorizations";
    private const string _supervisionUri = "/api/v1/teaching/supervision";
    private const string _correctionsUri = "/api/v1/teaching/corrections";

    private sealed record AuthDbRecord(
        Guid Id,
        string Status,
        bool UnverifiedCredentialWarning,
        string ScopeDescription,
        string? SuspensionOrRevocationReason);

    private sealed record SupDbRecord(
        Guid Id,
        string Status,
        string SupervisionType,
        Guid? SupervisingTeacherId,
        string? ResolutionNotes);

    private async Task<(Guid RoleId, Guid CapTypeId, Guid QualTypeId)> EnsureReferenceDataAsync()
    {
        await using var connection = await OpenConnectionAsync();
        var roleId = await TeachingIntegrationTestHelpers.EnsureTeachingRoleAsync(connection);
        var capTypeId = await TeachingIntegrationTestHelpers.EnsureCapabilityTypeAsync(connection);
        var qualTypeId = await TeachingIntegrationTestHelpers.EnsureQualificationTypeAsync(connection);
        return (roleId, capTypeId, qualTypeId);
    }

    private async Task<Guid> CreateActiveTeacherAsync(
        Guid roleId,
        string prefix = "TCH",
        string campus = "Kampus Utama",
        string department = "Mathematics")
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var nationalId = $"3177{Random.Shared.Next(10000000, 99999999)}";

        var regRequest = new RegisterTeacherProfileRequest(
            TeacherNumber: $"{prefix}-{suffix}",
            FullName: $"Teacher {prefix} {suffix}",
            DisplayName: $"Teacher {prefix}",
            EmployeeRef: $"EMP-{suffix}",
            NationalIdOrExternalRef: nationalId,
            Email: $"teacher.{suffix}@elsasa.edu",
            Phone: "+628123456789",
            TeachingRoleId: roleId,
            CampusAffiliation: campus,
            Department: department,
            EffectiveFrom: new DateOnly(2026, 8, 1));

        var regResponse = await Client.PostAsJsonAsync(_teachersUri, regRequest);
        regResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await regResponse.Content.ReadFromJsonAsync<RegisterTeacherProfileResponse>();

        var approveResponse = await Client.PostAsync($"{_teachersUri}/{created!.Id}/approve", null);
        approveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        return created.Id;
    }

    private async Task<Guid> SubmitCredentialAsync(
        Guid teacherId,
        Guid qualificationTypeId,
        string title = "Bachelor of Science in Education")
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var submitRequest = new SubmitQualificationEvidenceRequest(
            QualificationTypeId: qualificationTypeId,
            Title: title,
            IssuingAuthority: "State University of Jakarta",
            CredentialIdentifier: $"CRED-{suffix}",
            IssueDate: new DateOnly(2021, 7, 1),
            ExpiryDate: null,
            DocumentReference: $"https://documents.elsasa.edu/{suffix}.pdf");

        var submitResponse = await Client.PostAsJsonAsync($"{_teachersUri}/{teacherId}/qualifications", submitRequest);
        submitResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var result = await submitResponse.Content.ReadFromJsonAsync<SubmitQualificationEvidenceResponse>();
        return result!.Id;
    }

    private async Task VerifyCredentialAsync(Guid evidenceId)
    {
        var verifyRequest = new VerifyQualificationEvidenceRequest(
            VerificationNotes: "Credential verified against authoritative registry");
        var verifyResponse = await Client.PostAsJsonAsync($"{_qualificationsUri}/{evidenceId}/verify", verifyRequest);
        verifyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [SkippableFact]
    public async Task AuthorizationGrant_WithPermissiveWarning_Should_SetWarningFlag_When_LinkingUnverifiedCredential()
    {
        // Arrange: Active teacher and submitted (unverified) credential
        SkipIfDatabaseUnavailable();
        var (roleId, _, qualTypeId) = await EnsureReferenceDataAsync();
        var teacherId = await CreateActiveTeacherAsync(roleId, "AUTH-WARN");
        var unverifiedEvidenceId = await SubmitCredentialAsync(teacherId, qualTypeId, "Unverified Math Diploma");

        // Act: Grant authorization linking the unverified credential (TD-05 Permissive Gate)
        var grantRequest = new GrantTeachingAuthorizationRequest(
            ScopeDescription: "Advanced Calculus and Algebra Grades 10-12",
            CampusScope: "Kampus Utama",
            EffectiveStartDate: new DateOnly(2026, 8, 1),
            Status: "active",
            LinkedEvidenceIds: [unverifiedEvidenceId]);

        var response = await Client.PostAsJsonAsync($"{_teachersUri}/{teacherId}/authorizations", grantRequest);

        // Assert: 201 Created with warning flag set to true
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<GrantTeachingAuthorizationResponse>();
        created.Should().NotBeNull();
        created!.Id.Should().NotBeEmpty();
        created.UnverifiedCredentialWarning.Should().BeTrue();
        created.Status.Should().Be("active");
        created.ScopeDescription.Should().Be(grantRequest.ScopeDescription);

        // Verify database persistence
        const string checkSql = """
            SELECT id AS Id,
                   status AS Status,
                   unverified_credential_warning AS UnverifiedCredentialWarning,
                   scope_description AS ScopeDescription,
                   suspension_or_revocation_reason AS SuspensionOrRevocationReason
            FROM teaching.teaching_authorizations
            WHERE id = @Id;
            """;
        var dbRecord = await QuerySingleOrDefaultAsync<AuthDbRecord>(checkSql, new { created.Id });
        dbRecord.Should().NotBeNull();
        dbRecord!.UnverifiedCredentialWarning.Should().BeTrue();
        dbRecord.Status.Should().Be("active");

        // Verify post-commit domain event publication
        Factory.EventCollector.TeachingAuthorizationGrantedEvents
            .Should().Contain(e => e.AuthorizationId == created.Id && e.HasUnverifiedWarning == true);
    }

    [SkippableFact]
    public async Task WarningResolution_Should_ClearWarningFlag_When_AllLinkedCredentialsBecomeVerified()
    {
        // Arrange: Grant provisional authorization linking unverified credential
        SkipIfDatabaseUnavailable();
        var (roleId, _, qualTypeId) = await EnsureReferenceDataAsync();
        var teacherId = await CreateActiveTeacherAsync(roleId, "WARN-RES");
        var unverifiedEvidenceId = await SubmitCredentialAsync(teacherId, qualTypeId, "Provisional Teaching Certificate");

        var grantRequest = new GrantTeachingAuthorizationRequest(
            ScopeDescription: "Primary English and Literature",
            CampusScope: "Kampus Utama",
            EffectiveStartDate: new DateOnly(2026, 8, 1),
            Status: "active",
            LinkedEvidenceIds: [unverifiedEvidenceId]);

        var grantResponse = await Client.PostAsJsonAsync($"{_teachersUri}/{teacherId}/authorizations", grantRequest);
        grantResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var auth = await grantResponse.Content.ReadFromJsonAsync<GrantTeachingAuthorizationResponse>();
        auth!.UnverifiedCredentialWarning.Should().BeTrue();

        // Act: Verify the credential via verifier workflow (P3-S10)
        await VerifyCredentialAsync(unverifiedEvidenceId);

        // Assert: Authorization warning flag is automatically cleared to 0 in database
        const string checkSql = """
            SELECT unverified_credential_warning
            FROM teaching.teaching_authorizations
            WHERE id = @Id;
            """;
        var warningFlag = await ExecuteScalarAsync<bool>(checkSql, new { auth.Id });
        warningFlag.Should().BeFalse("warning flag must be cleared when all linked credentials become verified");

        // Verify domain event publication
        Factory.EventCollector.QualificationEvidenceVerifiedEvents
            .Should().Contain(e => e.EvidenceId == unverifiedEvidenceId);
    }

    [SkippableFact]
    public async Task ConditionalSupervision_Should_CreateRequirement_And_ResolutionPromotesAuthorizationToActive()
    {
        // Arrange: Mentee and mentor active teacher profiles
        SkipIfDatabaseUnavailable();
        var (roleId, _, _) = await EnsureReferenceDataAsync();
        var menteeId = await CreateActiveTeacherAsync(roleId, "MENTEE");
        var mentorId = await CreateActiveTeacherAsync(roleId, "MENTOR");

        // Act 1: Grant authorization with status 'authorized_under_supervision' and mandatory supervision payload (TD-05, P4-S11)
        var grantRequest = new GrantTeachingAuthorizationRequest(
            ScopeDescription: "Biology and Environmental Sciences Grades 9-10",
            CampusScope: "Kampus Utama",
            EffectiveStartDate: new DateOnly(2026, 8, 1),
            Status: "authorized_under_supervision",
            Supervision: new GrantSupervisionPayload(
                SupervisionType: "co_teaching",
                Description: "Co-teaching and lesson planning review with designated senior mentor",
                StartDate: new DateOnly(2026, 8, 1),
                EndDate: new DateOnly(2026, 12, 1),
                SupervisingTeacherId: mentorId));

        var grantResponse = await Client.PostAsJsonAsync($"{_teachersUri}/{menteeId}/authorizations", grantRequest);
        grantResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var createdAuth = await grantResponse.Content.ReadFromJsonAsync<GrantTeachingAuthorizationResponse>();
        createdAuth.Should().NotBeNull();
        createdAuth!.Status.Should().Be("authorized_under_supervision");
        createdAuth.SupervisionRequirementId.Should().NotBeNull();

        // Verify supervision requirement record in DB
        const string checkSupSql = """
            SELECT id AS Id,
                   status AS Status,
                   supervision_type AS SupervisionType,
                   supervising_teacher_id AS SupervisingTeacherId,
                   resolution_notes AS ResolutionNotes
            FROM teaching.supervision_requirements
            WHERE id = @Id;
            """;
        var supRecord = await QuerySingleOrDefaultAsync<SupDbRecord>(checkSupSql, new { Id = createdAuth.SupervisionRequirementId });
        supRecord.Should().NotBeNull();
        supRecord!.Status.Should().Be("active");
        supRecord.SupervisionType.Should().Be("co_teaching");
        supRecord.SupervisingTeacherId.Should().Be(mentorId);

        // Act 2: Resolve supervision requirement upon condition completion (P4-S13)
        var resolveRequest = new ResolveSupervisionRequirementRequest(
            ResolutionStatus: "completed",
            ResolutionNotes: "Candidate completed 40 hours of co-teaching with exemplary evaluation from senior mentor.",
            UpdateAuthorizationToActive: true);

        var resolveResponse = await Client.PostAsJsonAsync($"{_supervisionUri}/{createdAuth.SupervisionRequirementId}/resolve", resolveRequest);
        resolveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var resolveResult = await resolveResponse.Content.ReadFromJsonAsync<ResolveSupervisionRequirementResponse>();
        resolveResult.Should().NotBeNull();
        resolveResult!.Status.Should().Be("completed");
        resolveResult.AuthorizationUpdatedToActive.Should().BeTrue();
        resolveResult.AuthorizationStatus.Should().Be("active");

        // Verify database: authorization status promoted to 'active'
        const string checkAuthSql = """
            SELECT status
            FROM teaching.teaching_authorizations
            WHERE id = @Id;
            """;
        var updatedAuthStatus = await ExecuteScalarAsync<string>(checkAuthSql, new { createdAuth.Id });
        updatedAuthStatus.Should().Be("active");

        // Verify domain event publication
        Factory.EventCollector.TeachingAuthorizationStatusChangedEvents
            .Should().Contain(e => e.AuthorizationId == createdAuth.Id && e.NewStatus == "active");
    }

    [SkippableFact]
    public async Task AuthorizationStatusLifecycle_Should_EnforceMandatoryReason_And_UpdateStatus()
    {
        // Arrange: Active authorization
        SkipIfDatabaseUnavailable();
        var (roleId, _, _) = await EnsureReferenceDataAsync();
        var teacherId = await CreateActiveTeacherAsync(roleId, "AUTH-LIFE");

        var grantRequest = new GrantTeachingAuthorizationRequest(
            ScopeDescription: "Physics Grade 11-12",
            CampusScope: "Kampus Utama",
            EffectiveStartDate: new DateOnly(2026, 8, 1),
            Status: "active");

        var grantResponse = await Client.PostAsJsonAsync($"{_teachersUri}/{teacherId}/authorizations", grantRequest);
        var auth = await grantResponse.Content.ReadFromJsonAsync<GrantTeachingAuthorizationResponse>();

        // 1. Act & Assert: Transition to 'suspended' without reason fails (422 UnprocessableEntity)
        var invalidReq = new UpdateTeachingAuthorizationStatusRequest(
            Status: "suspended",
            StatusReason: "   ");
        var invalidResponse = await Client.PostAsJsonAsync($"{_authorizationsUri}/{auth!.Id}/status", invalidReq);
        invalidResponse.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        // 2. Act: Valid transition with mandatory justification (P4-S12)
        var validReq = new UpdateTeachingAuthorizationStatusRequest(
            Status: "suspended",
            StatusReason: "Pending internal investigation on syllabus adherence.");
        var validResponse = await Client.PostAsJsonAsync($"{_authorizationsUri}/{auth.Id}/status", validReq);

        validResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await validResponse.Content.ReadFromJsonAsync<UpdateTeachingAuthorizationStatusResponse>();
        updated.Should().NotBeNull();
        updated!.Status.Should().Be("suspended");
        updated.StatusReason.Should().Be(validReq.StatusReason);

        // Verify database persistence
        const string checkSql = """
            SELECT status AS Status, suspension_or_revocation_reason AS SuspensionOrRevocationReason
            FROM teaching.teaching_authorizations
            WHERE id = @Id;
            """;
        var dbRecord = await QuerySingleOrDefaultAsync<AuthDbRecord>(checkSql, new { auth.Id });
        dbRecord.Should().NotBeNull();
        dbRecord!.Status.Should().Be("suspended");
        dbRecord.SuspensionOrRevocationReason.Should().Be(validReq.StatusReason);

        // Verify domain event publication
        Factory.EventCollector.TeachingAuthorizationStatusChangedEvents
            .Should().Contain(e => e.AuthorizationId == auth.Id && e.NewStatus == "suspended");
    }

    [SkippableFact]
    public async Task AuditableRecordCorrections_Should_BranchByRole_And_RequireApprovalForCoordinator()
    {
        // Arrange: Teacher profile and active authorization
        SkipIfDatabaseUnavailable();
        var (roleId, _, _) = await EnsureReferenceDataAsync();
        var teacherId = await CreateActiveTeacherAsync(roleId, "CORR-TEST");

        var grantRequest = new GrantTeachingAuthorizationRequest(
            ScopeDescription: "Original Scope Description: General Science",
            CampusScope: "Kampus Utama",
            EffectiveStartDate: new DateOnly(2026, 8, 1),
            Status: "active");

        var grantResponse = await Client.PostAsJsonAsync($"{_teachersUri}/{teacherId}/authorizations", grantRequest);
        var auth = await grantResponse.Content.ReadFromJsonAsync<GrantTeachingAuthorizationResponse>();

        // ─────────────────────────────────────────────────────────────────────
        // 1. Non-Authority Flow (TeachingCoordinator): Creates 'requested' correction, target unchanged (TD-06)
        // ─────────────────────────────────────────────────────────────────────
        Factory.CurrentUser.Roles = ["TeachingCoordinator"];
        Factory.CurrentUser.Permissions = [Permissions.Teaching.Faculty.Correct];

        var coordRequest = new ApplyRecordCorrectionRequest(
            TargetTableName: "teaching_authorizations",
            TargetRecordId: auth!.Id,
            FieldName: "scope_description",
            CorrectedValue: "Realigned Scope: General Science & Chemistry Laboratory",
            ReasonForCorrection: "Syllabus revision aligned with new semester curriculum",
            SupportingEvidence: "Curriculum Committee Memo #2026-11",
            DownstreamImpactReviewed: true,
            TeacherProfileId: teacherId);

        var coordResponse = await Client.PostAsJsonAsync(_correctionsUri, coordRequest);
        coordResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var requestedCorrection = await coordResponse.Content.ReadFromJsonAsync<TeachingRecordCorrectionDto>();
        requestedCorrection.Should().NotBeNull();
        requestedCorrection!.Status.Should().Be("requested");
        requestedCorrection.ApprovedBy.Should().BeNull();
        requestedCorrection.ApprovedDate.Should().BeNull();

        // Verify database: target column remains UNCHANGED
        var currentScope = await ExecuteScalarAsync<string>(
            "SELECT scope_description FROM teaching.teaching_authorizations WHERE id = @Id;",
            new { auth.Id });
        currentScope.Should().Be("Original Scope Description: General Science", "coordinator cannot overwrite target records directly");

        // ─────────────────────────────────────────────────────────────────────
        // 2. Teaching Authority Approval Flow: Approves requested correction and applies to target
        // ─────────────────────────────────────────────────────────────────────
        Factory.CurrentUser.Reset(); // Restores Admin / Teaching Authority role

        var approveResponse = await Client.PostAsync($"{_correctionsUri}/{requestedCorrection.Id}/approve", null);
        approveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var approvedResult = await approveResponse.Content.ReadFromJsonAsync<TeachingRecordCorrectionDto>();
        approvedResult.Should().NotBeNull();
        approvedResult!.Status.Should().Be("approved");
        approvedResult.ApprovedBy.Should().NotBeNullOrWhiteSpace();

        // Verify database: target column IS now updated
        var updatedScope = await ExecuteScalarAsync<string>(
            "SELECT scope_description FROM teaching.teaching_authorizations WHERE id = @Id;",
            new { auth.Id });
        updatedScope.Should().Be("Realigned Scope: General Science & Chemistry Laboratory");

        // Verify post-commit domain event
        Factory.EventCollector.TeachingRecordCorrectedEvents
            .Should().Contain(e => e.CorrectionId == requestedCorrection.Id && e.TargetTable == "teaching_authorizations");

        // ─────────────────────────────────────────────────────────────────────
        // 3. Teaching Authority Direct Apply Flow: Directly applies correction in single atomic transaction
        // ─────────────────────────────────────────────────────────────────────
        var directRequest = new ApplyRecordCorrectionRequest(
            TargetTableName: "teacher_profiles",
            TargetRecordId: teacherId,
            FieldName: "department",
            CorrectedValue: "Physical and Chemical Sciences",
            ReasonForCorrection: "Departmental restructuring mandate",
            SupportingEvidence: "Rector Decree #2026-09",
            DownstreamImpactReviewed: true,
            TeacherProfileId: teacherId);

        var directResponse = await Client.PostAsJsonAsync(_correctionsUri, directRequest);
        directResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var directResult = await directResponse.Content.ReadFromJsonAsync<TeachingRecordCorrectionDto>();
        directResult.Should().NotBeNull();
        directResult!.Status.Should().Be("applied");
        directResult.ApprovedBy.Should().NotBeNullOrWhiteSpace();

        // Verify database: target teacher profile column is immediately updated
        var updatedDept = await ExecuteScalarAsync<string>(
            "SELECT department FROM teaching.teacher_profiles WHERE id = @Id;",
            new { Id = teacherId });
        updatedDept.Should().Be("Physical and Chemical Sciences");

        // Verify post-commit domain event
        Factory.EventCollector.TeachingRecordCorrectedEvents
            .Should().Contain(e => e.CorrectionId == directResult.Id && e.TargetTable == "teacher_profiles");
    }

    [SkippableFact]
    public async Task TeachingModule_CrossDomainInquiry_Should_AnswerProfile_Authorization_AndActiveFacultyInquiries()
    {
        // Arrange: Seed active teacher with verified credential and active authorization
        SkipIfDatabaseUnavailable();
        var (roleId, capTypeId, qualTypeId) = await EnsureReferenceDataAsync();
        var teacherId = await CreateActiveTeacherAsync(roleId, "MOD-TEST", "Kampus Utama", "Natural Sciences");
        var evidenceId = await SubmitCredentialAsync(teacherId, qualTypeId, "Certified Physics Educator");
        await VerifyCredentialAsync(evidenceId);

        // Add pedagogical capability
        await using (var conn = await OpenConnectionAsync())
        {
            await conn.ExecuteAsync("""
                INSERT INTO teaching.teaching_capabilities (
                    id, teacher_profile_id, capability_type_id, capability_code, capability_name,
                    proficiency_level, status, created_at, created_by
                ) VALUES (
                    @Id, @TeacherId, @CapTypeId, 'CAP-PHYSICS-01', 'High School Physics',
                    'expert', 'active', SYSUTCDATETIME(), 'TEST_SETUP'
                );
                """, new { Id = Guid.NewGuid(), TeacherId = teacherId, CapTypeId = capTypeId });
        }

        var grantRequest = new GrantTeachingAuthorizationRequest(
            ScopeDescription: "High School Physics Laboratory and Mechanics",
            CampusScope: "Kampus Utama",
            ProgramScope: "Senior High Academic Track",
            EffectiveStartDate: new DateOnly(2026, 8, 1),
            EffectiveEndDate: new DateOnly(2027, 7, 31),
            Status: "active",
            LinkedEvidenceIds: [evidenceId]);

        var grantResponse = await Client.PostAsJsonAsync($"{_teachersUri}/{teacherId}/authorizations", grantRequest);
        grantResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        // Act: Resolve cross-domain ITeachingModule service (ADR-001, ADR-010, TD-08)
        using var scope = Factory.Services.CreateScope();
        var module = scope.ServiceProvider.GetRequiredService<ITeachingModule>();

        // 1. Test GetTeacherProfileAsync
        var existingProfileResult = await module.GetTeacherProfileAsync(teacherId);
        existingProfileResult.IsSuccess.Should().BeTrue();
        existingProfileResult.Value.Should().NotBeNull();
        existingProfileResult.Value!.Id.Should().Be(teacherId);
        existingProfileResult.Value.Standing.Should().Be("active");
        existingProfileResult.Value.Authorizations.Should().Contain(a => a.ScopeDescription == grantRequest.ScopeDescription);
        existingProfileResult.Value.Qualifications.Should().Contain(q => q.Id == evidenceId && q.VerificationState == "verified");

        var nonExistentProfileResult = await module.GetTeacherProfileAsync(Guid.NewGuid());
        nonExistentProfileResult.IsSuccess.Should().BeTrue();
        nonExistentProfileResult.Value.Should().BeNull();

        // 2. Test IsTeacherAuthorizedAsync
        var authorizedDate = new DateOnly(2026, 10, 15);

        // Matching subject and campus within date range -> true
        var authCheck1 = await module.IsTeacherAuthorizedAsync(teacherId, "High School Physics", "Kampus Utama", authorizedDate);
        authCheck1.IsSuccess.Should().BeTrue();
        authCheck1.Value.Should().BeTrue();

        // Matching partial subject in scope description -> true
        var authCheck2 = await module.IsTeacherAuthorizedAsync(teacherId, "Mechanics", "Kampus Utama", authorizedDate);
        authCheck2.IsSuccess.Should().BeTrue();
        authCheck2.Value.Should().BeTrue();

        // Non-matching subject -> false
        var unauthCheckSubj = await module.IsTeacherAuthorizedAsync(teacherId, "Sociology", "Kampus Utama", authorizedDate);
        unauthCheckSubj.IsSuccess.Should().BeTrue();
        unauthCheckSubj.Value.Should().BeFalse();

        // Non-matching campus -> false
        var unauthCheckCampus = await module.IsTeacherAuthorizedAsync(teacherId, "High School Physics", "Kampus Timur", authorizedDate);
        unauthCheckCampus.IsSuccess.Should().BeTrue();
        unauthCheckCampus.Value.Should().BeFalse();

        // Outside effective date range (prior to start) -> false
        var unauthCheckDate = await module.IsTeacherAuthorizedAsync(teacherId, "High School Physics", "Kampus Utama", new DateOnly(2025, 1, 1));
        unauthCheckDate.IsSuccess.Should().BeTrue();
        unauthCheckDate.Value.Should().BeFalse();

        // 3. Test GetActiveTeachersAsync
        var activeTeachersAll = await module.GetActiveTeachersAsync();
        activeTeachersAll.IsSuccess.Should().BeTrue();
        activeTeachersAll.Value.Should().Contain(t => t.Id == teacherId && t.Standing == "active");

        var activeTeachersCampus = await module.GetActiveTeachersAsync("Kampus Utama");
        activeTeachersCampus.IsSuccess.Should().BeTrue();
        activeTeachersCampus.Value.Should().Contain(t => t.Id == teacherId);

        var activeTeachersOtherCampus = await module.GetActiveTeachersAsync("NonExistentCampus");
        activeTeachersOtherCampus.IsSuccess.Should().BeTrue();
        activeTeachersOtherCampus.Value.Should().NotContain(t => t.Id == teacherId);
    }

    [SkippableFact]
    public async Task DemoDataSeederAndPurgerCycle_Should_SeedAllPersonas_And_PurgeCleanlyInReverseOrder()
    {
        // Arrange
        SkipIfDatabaseUnavailable();

        using var scope = Factory.Services.CreateScope();
        var seeder = scope.ServiceProvider.GetRequiredService<ITeachingDemoDataSeeder>();
        var purger = scope.ServiceProvider.GetRequiredService<ITeachingDemoDataPurger>();

        // 1. Initial Purge to guarantee pristine state
        await purger.PurgeDomainDataAsync();

        // 2. Act: Seed Teaching Domain with realistic Hagios personas (TD-09, P5-S16)
        var operationsContext = new OperationsSeedResult(
            AcademicYearId: Guid.NewGuid(),
            AcademicYearCode: "AY-2026-2027",
            HistoricalPeriodId: Guid.NewGuid(),
            ActivePeriodId: Guid.NewGuid(),
            FuturePeriodId: Guid.NewGuid(),
            SchoolCalendarId: Guid.NewGuid(),
            BaseDate: new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc));

        var seedResult = await seeder.SeedTeachingAsync(operationsContext);

        // 3. Assert Seeding Results
        seedResult.Should().NotBeNull();
        seedResult.RolesCreated.Should().BeGreaterThan(0);
        seedResult.CapabilityTypesCreated.Should().BeGreaterThan(0);
        seedResult.QualificationTypesCreated.Should().BeGreaterThan(0);
        seedResult.TeacherProfilesCreated.Should().Be(4);
        seedResult.AuthorizationsCreated.Should().BeGreaterThan(0);

        // Verify deterministic persona identifiers match DemoDataConstants
        seedResult.Personas.BudiHartonoTeacherId.Should().Be(DemoDataConstants.BudiHartonoTeacherId);
        seedResult.Personas.SitiNurhalizaTeacherId.Should().Be(DemoDataConstants.SitiNurhalizaTeacherId);
        seedResult.Personas.JohnMillerTeacherId.Should().Be(DemoDataConstants.JohnMillerTeacherId);
        seedResult.Personas.HendraSetiawanTeacherId.Should().Be(DemoDataConstants.HendraSetiawanTeacherId);

        // Verify database persona characteristics
        const string queryTeacherSql = """
            SELECT id AS Id, teacher_number AS TeacherNumber, standing AS Standing, employee_ref AS EmployeeRef
            FROM teaching.teacher_profiles
            WHERE id = @Id;
            """;

        // Persona 1: Budi Hartono (Senior Permanent Teacher, active standing)
        var budi = await QuerySingleOrDefaultAsync<TeacherProfileSummary>(queryTeacherSql, new { Id = DemoDataConstants.BudiHartonoTeacherId });
        budi.Should().NotBeNull();
        budi!.Standing.Should().Be("active");
        budi.EmployeeRef.Should().NotBeNull();

        // Persona 2: Siti Nurhaliza (Contract Teacher, under supervision)
        var siti = await QuerySingleOrDefaultAsync<TeacherProfileSummary>(queryTeacherSql, new { Id = DemoDataConstants.SitiNurhalizaTeacherId });
        siti.Should().NotBeNull();
        siti!.Standing.Should().Be("active");

        var sitiSupervisionCount = await ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM teaching.supervision_requirements WHERE supervising_teacher_id = @BudiId;",
            new { BudiId = DemoDataConstants.BudiHartonoTeacherId });
        sitiSupervisionCount.Should().BeGreaterThan(0, "Siti should have Budi assigned as supervising mentor");

        // Persona 3: John Miller (Visiting Non-Employee Teacher, null employee_ref)
        var john = await QuerySingleOrDefaultAsync<TeacherProfileSummary>(queryTeacherSql, new { Id = DemoDataConstants.JohnMillerTeacherId });
        john.Should().NotBeNull();
        john!.Standing.Should().Be("active");
        john.EmployeeRef.Should().BeNull("visiting non-employee teacher must have null employee_ref");

        // Persona 4: Hendra Setiawan (Pending Onboarding Candidate, pending_approval standing)
        var hendra = await QuerySingleOrDefaultAsync<TeacherProfileSummary>(queryTeacherSql, new { Id = DemoDataConstants.HendraSetiawanTeacherId });
        hendra.Should().NotBeNull();
        hendra!.Standing.Should().Be("pending_approval");

        // 4. Act: Purge Domain Data in strict reverse-dependency order (P5-S16)
        await purger.PurgeDomainDataAsync();

        // 5. Assert: All 10 domain tables in teaching.* are completely empty
        var trcCount = await ExecuteScalarAsync<int>("SELECT COUNT(1) FROM teaching.teaching_record_corrections;");
        var supCount = await ExecuteScalarAsync<int>("SELECT COUNT(1) FROM teaching.supervision_requirements;");
        var authEvCount = await ExecuteScalarAsync<int>("SELECT COUNT(1) FROM teaching.authorization_evidence;");
        var authCount = await ExecuteScalarAsync<int>("SELECT COUNT(1) FROM teaching.teaching_authorizations;");
        var qualEvCount = await ExecuteScalarAsync<int>("SELECT COUNT(1) FROM teaching.qualification_evidence;");
        var capCount = await ExecuteScalarAsync<int>("SELECT COUNT(1) FROM teaching.teaching_capabilities;");
        var profCount = await ExecuteScalarAsync<int>("SELECT COUNT(1) FROM teaching.teacher_profiles;");
        var qualTypeCount = await ExecuteScalarAsync<int>("SELECT COUNT(1) FROM teaching.qualification_types;");
        var capTypeCount = await ExecuteScalarAsync<int>("SELECT COUNT(1) FROM teaching.capability_types;");
        var roleCount = await ExecuteScalarAsync<int>("SELECT COUNT(1) FROM teaching.teaching_roles;");

        trcCount.Should().Be(0);
        supCount.Should().Be(0);
        authEvCount.Should().Be(0);
        authCount.Should().Be(0);
        qualEvCount.Should().Be(0);
        capCount.Should().Be(0);
        profCount.Should().Be(0);
        qualTypeCount.Should().Be(0);
        capTypeCount.Should().Be(0);
        roleCount.Should().Be(0);
    }

    private sealed record TeacherProfileSummary(Guid Id, string TeacherNumber, string Standing, string? EmployeeRef);
}
