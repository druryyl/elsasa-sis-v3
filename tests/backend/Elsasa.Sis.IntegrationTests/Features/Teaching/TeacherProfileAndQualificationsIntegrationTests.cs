namespace Elsasa.Sis.IntegrationTests.Features.Teaching;

using System.Net;
using System.Net.Http.Json;
using Elsasa.Sis.IntegrationTests.Fixtures;
using Elsasa.Sis.Teaching.Features.QualificationEvidence.RejectQualificationEvidence;
using Elsasa.Sis.Teaching.Features.QualificationEvidence.SubmitQualificationEvidence;
using Elsasa.Sis.Teaching.Features.QualificationEvidence.VerifyQualificationEvidence;
using Elsasa.Sis.Teaching.Features.TeacherProfiles.ApproveTeacherProfile;
using Elsasa.Sis.Teaching.Features.TeacherProfiles.ChangeTeacherStanding;
using Elsasa.Sis.Teaching.Features.TeacherProfiles.RegisterTeacherProfile;
using Elsasa.Sis.Teaching.Features.TeachingCapabilities.AddTeachingCapability;
using FluentAssertions;
using Xunit;

/// <summary>
/// Integration tests verifying teacher profile candidate intake, authority approval,
/// standing transitions, capabilities management, and qualification verification/rejection (P6-S19, TD-01..TD-05).
/// </summary>
public sealed class TeacherProfileAndQualificationsIntegrationTests(IntegrationTestWebAppFactory factory)
    : BaseIntegrationTest(factory)
{
    private const string _teachersUri = "/api/v1/teaching/teachers";
    private const string _qualificationsUri = "/api/v1/teaching/qualifications";

    private sealed record ProfileAuditRecord(string Standing, string? ApprovedBy, DateTime? ApprovedDate);
    private sealed record ProfileRefRecord(string? EmployeeRef, string? NationalIdOrExternalRef);
    private sealed record EvidenceRecord(string VerificationState, string? VerifiedBy, DateTime? VerifiedDate, string? IssuingAuthority, string? VerificationNotes, string? RejectionReason);

    private async Task<(Guid RoleId, Guid CapTypeId, Guid QualTypeId)> EnsureReferenceDataAsync()
    {
        await using var connection = await OpenConnectionAsync();
        var roleId = await TeachingIntegrationTestHelpers.EnsureTeachingRoleAsync(connection);
        var capTypeId = await TeachingIntegrationTestHelpers.EnsureCapabilityTypeAsync(connection);
        var qualTypeId = await TeachingIntegrationTestHelpers.EnsureQualificationTypeAsync(connection);
        return (roleId, capTypeId, qualTypeId);
    }

    [SkippableFact]
    public async Task CandidateIntake_Should_CreateProfileWithPendingApprovalStanding_And_PublishEvent()
    {
        // Arrange
        SkipIfDatabaseUnavailable();
        var (roleId, _, _) = await EnsureReferenceDataAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var nationalId = $"3171{Random.Shared.Next(10000000, 99999999)}0001";

        var request = new RegisterTeacherProfileRequest(
            TeacherNumber: $"TCH-TEST-{suffix}",
            FullName: $"Dr. Jane Doe {suffix}",
            DisplayName: "Jane Doe",
            EmployeeRef: $"EMP-{suffix}",
            NationalIdOrExternalRef: nationalId,
            Email: $"jane.doe.{suffix}@elsasa.edu",
            Phone: "+6281234567890",
            TeachingRoleId: roleId,
            CampusAffiliation: "Kampus Utama",
            Department: "Sciences",
            EffectiveFrom: new DateOnly(2026, 8, 1),
            Notes: "Candidate application submitted via HR portal");

        // Act
        var response = await Client.PostAsJsonAsync(_teachersUri, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<RegisterTeacherProfileResponse>();
        created.Should().NotBeNull();
        created!.Id.Should().NotBeEmpty();
        created.TeacherNumber.Should().Be(request.TeacherNumber);
        created.Standing.Should().Be("pending_approval");

        // Verify database persistence
        const string querySql = """
            SELECT standing AS Standing, approved_by AS ApprovedBy, approved_date AS ApprovedDate
            FROM teaching.teacher_profiles
            WHERE id = @Id;
            """;
        var dbRecord = await QuerySingleOrDefaultAsync<ProfileAuditRecord>(querySql, new { created.Id });
        dbRecord.Should().NotBeNull();
        dbRecord!.Standing.Should().Be("pending_approval");
        dbRecord.ApprovedBy.Should().BeNull();
        dbRecord.ApprovedDate.Should().BeNull();

        // Verify domain event publication
        Factory.EventCollector.TeacherProfileCreatedEvents
            .Should().Contain(e => e.TeacherProfileId == created.Id && e.TeacherNumber == created.TeacherNumber);
    }

    [SkippableFact]
    public async Task NonEmployeeRegistration_Should_Succeed_WithNullEmployeeRef_And_UniqueNationalId()
    {
        // Arrange
        SkipIfDatabaseUnavailable();
        var (roleId, _, _) = await EnsureReferenceDataAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];
        var passportNumber = $"W{Random.Shared.Next(10000000, 99999999)}";

        var request = new RegisterTeacherProfileRequest(
            TeacherNumber: $"TCH-EXT-{suffix}",
            FullName: $"Visiting Prof. John Smith {suffix}",
            DisplayName: "Prof. John",
            EmployeeRef: null, // Non-employee / external adjunct
            NationalIdOrExternalRef: passportNumber,
            Email: $"john.smith.{suffix}@external.org",
            Phone: "+14155552671",
            TeachingRoleId: roleId,
            CampusAffiliation: "Kampus Barat",
            Department: "Humanities",
            EffectiveFrom: new DateOnly(2026, 8, 1));

        // Act
        var response = await Client.PostAsJsonAsync(_teachersUri, request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<RegisterTeacherProfileResponse>();
        created.Should().NotBeNull();

        // Verify database has NULL employee_ref and persisted national_id
        const string checkSql = """
            SELECT employee_ref AS EmployeeRef, national_id_or_external_ref AS NationalIdOrExternalRef
            FROM teaching.teacher_profiles
            WHERE id = @Id;
            """;
        var dbRecord = await QuerySingleOrDefaultAsync<ProfileRefRecord>(checkSql, new { created!.Id });
        dbRecord.Should().NotBeNull();
        dbRecord!.EmployeeRef.Should().BeNull();
        dbRecord.NationalIdOrExternalRef.Should().Be(passportNumber);

        // Attempt duplicate registration with the same passport number -> 409 Conflict
        var duplicateRequest = new RegisterTeacherProfileRequest(
            TeacherNumber: $"TCH-EXT-DUP-{suffix}",
            FullName: $"Duplicate Applicant {suffix}",
            DisplayName: null,
            EmployeeRef: null,
            NationalIdOrExternalRef: passportNumber,
            Email: $"duplicate.{suffix}@external.org",
            Phone: null,
            TeachingRoleId: roleId,
            CampusAffiliation: "Kampus Barat",
            Department: "Humanities",
            EffectiveFrom: new DateOnly(2026, 8, 1));

        var duplicateResponse = await Client.PostAsJsonAsync(_teachersUri, duplicateRequest);
        duplicateResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [SkippableFact]
    public async Task AuthorityApproval_Should_TransitionStandingToActive_And_RecordApproverAndDate()
    {
        // Arrange
        SkipIfDatabaseUnavailable();
        var (roleId, _, _) = await EnsureReferenceDataAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var regRequest = new RegisterTeacherProfileRequest(
            TeacherNumber: $"TCH-APP-{suffix}",
            FullName: $"Candidate Mary {suffix}",
            DisplayName: "Mary",
            EmployeeRef: $"EMP-APP-{suffix}",
            NationalIdOrExternalRef: $"3172{Random.Shared.Next(10000000, 99999999)}0002",
            Email: $"mary.{suffix}@elsasa.edu",
            Phone: "+6281987654321",
            TeachingRoleId: roleId,
            CampusAffiliation: "Kampus Utama",
            Department: "Mathematics",
            EffectiveFrom: new DateOnly(2026, 8, 1));

        var regResponse = await Client.PostAsJsonAsync(_teachersUri, regRequest);
        regResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var candidate = await regResponse.Content.ReadFromJsonAsync<RegisterTeacherProfileResponse>();

        // Act: Formal authority approval
        var approveResponse = await Client.PostAsync($"{_teachersUri}/{candidate!.Id}/approve", null);

        // Assert
        approveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var approveResult = await approveResponse.Content.ReadFromJsonAsync<ApproveTeacherProfileResponse>();
        approveResult.Should().NotBeNull();
        approveResult!.Id.Should().Be(candidate.Id);
        approveResult.Standing.Should().Be("active");
        approveResult.ApprovedBy.Should().NotBeNullOrWhiteSpace();
        approveResult.ApprovedDate.Should().Be(DateOnly.FromDateTime(DateTime.UtcNow));

        // Verify database persistence
        const string checkSql = """
            SELECT standing AS Standing, approved_by AS ApprovedBy, approved_date AS ApprovedDate
            FROM teaching.teacher_profiles
            WHERE id = @Id;
            """;
        var dbRecord = await QuerySingleOrDefaultAsync<ProfileAuditRecord>(checkSql, new { candidate.Id });
        dbRecord.Should().NotBeNull();
        dbRecord!.Standing.Should().Be("active");
        dbRecord.ApprovedBy.Should().Be(Factory.CurrentUser.Username);
        DateOnly.FromDateTime(dbRecord.ApprovedDate!.Value).Should().Be(DateOnly.FromDateTime(DateTime.UtcNow));

        // Verify post-commit domain event
        Factory.EventCollector.TeacherProfileApprovedEvents
            .Should().Contain(e => e.TeacherProfileId == candidate.Id && e.ApprovedBy == dbRecord.ApprovedBy);
    }

    [SkippableFact]
    public async Task StandingModification_Should_TransitionToRestrictedOrSuspended_WithMandatoryReason()
    {
        // Arrange
        SkipIfDatabaseUnavailable();
        var (roleId, _, _) = await EnsureReferenceDataAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var regRequest = new RegisterTeacherProfileRequest(
            TeacherNumber: $"TCH-STD-{suffix}",
            FullName: $"Teacher Robert {suffix}",
            DisplayName: "Robert",
            EmployeeRef: $"EMP-STD-{suffix}",
            NationalIdOrExternalRef: $"3173{Random.Shared.Next(10000000, 99999999)}0003",
            Email: $"robert.{suffix}@elsasa.edu",
            Phone: "+6281122334455",
            TeachingRoleId: roleId,
            CampusAffiliation: "Kampus Utama",
            Department: "Arts",
            EffectiveFrom: new DateOnly(2026, 8, 1));

        var regResponse = await Client.PostAsJsonAsync(_teachersUri, regRequest);
        var created = await regResponse.Content.ReadFromJsonAsync<RegisterTeacherProfileResponse>();
        await Client.PostAsync($"{_teachersUri}/{created!.Id}/approve", null);

        // 1. Act & Assert: Transition without mandatory reason should fail (422 UnprocessableEntity)
        var invalidRequest = new ChangeTeacherStandingRequest(
            Standing: "restricted",
            StandingReason: "   "); // Empty reason
        var invalidResponse = await Client.PostAsJsonAsync($"{_teachersUri}/{created.Id}/standing", invalidRequest);
        invalidResponse.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        // 2. Act: Valid transition to 'restricted'
        var restrictedRequest = new ChangeTeacherStandingRequest(
            Standing: "restricted",
            StandingReason: "Pending annual health certificate clearance.");
        var restrictedResponse = await Client.PostAsJsonAsync($"{_teachersUri}/{created.Id}/standing", restrictedRequest);

        restrictedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var restrictedResult = await restrictedResponse.Content.ReadFromJsonAsync<ChangeTeacherStandingResponse>();
        restrictedResult.Should().NotBeNull();
        restrictedResult!.Standing.Should().Be("restricted");
        restrictedResult.StandingReason.Should().Be(restrictedRequest.StandingReason);

        // Verify database persistence for restricted
        var dbStanding1 = await ExecuteScalarAsync<string>(
            "SELECT standing FROM teaching.teacher_profiles WHERE id = @Id;",
            new { created.Id });
        dbStanding1.Should().Be("restricted");

        // 3. Act: Valid transition to 'suspended'
        var suspendedRequest = new ChangeTeacherStandingRequest(
            Standing: "suspended",
            StandingReason: "Administrative investigation ongoing under HR-REG-2026.");
        var suspendedResponse = await Client.PostAsJsonAsync($"{_teachersUri}/{created.Id}/standing", suspendedRequest);

        suspendedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var suspendedResult = await suspendedResponse.Content.ReadFromJsonAsync<ChangeTeacherStandingResponse>();
        suspendedResult.Should().NotBeNull();
        suspendedResult!.Standing.Should().Be("suspended");
        suspendedResult.StandingReason.Should().Be(suspendedRequest.StandingReason);

        // Verify database persistence for suspended
        var dbStanding2 = await ExecuteScalarAsync<string>(
            "SELECT standing FROM teaching.teacher_profiles WHERE id = @Id;",
            new { created.Id });
        dbStanding2.Should().Be("suspended");

        // Verify domain events captured
        Factory.EventCollector.TeacherStandingChangedEvents
            .Should().Contain(e => e.TeacherProfileId == created.Id && e.NewStanding == "restricted");
        Factory.EventCollector.TeacherStandingChangedEvents
            .Should().Contain(e => e.TeacherProfileId == created.Id && e.NewStanding == "suspended");
    }

    [SkippableFact]
    public async Task CapabilitiesManagement_Should_AddAndRemoveCapability_WithoutAffectingStandingOrAuthorizations()
    {
        // Arrange
        SkipIfDatabaseUnavailable();
        var (roleId, capTypeId, _) = await EnsureReferenceDataAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var regRequest = new RegisterTeacherProfileRequest(
            TeacherNumber: $"TCH-CAP-{suffix}",
            FullName: $"Teacher Alice {suffix}",
            DisplayName: "Alice",
            EmployeeRef: $"EMP-CAP-{suffix}",
            NationalIdOrExternalRef: $"3174{Random.Shared.Next(10000000, 99999999)}0004",
            Email: $"alice.{suffix}@elsasa.edu",
            Phone: "+6281778899001",
            TeachingRoleId: roleId,
            CampusAffiliation: "Kampus Utama",
            Department: "Languages",
            EffectiveFrom: new DateOnly(2026, 8, 1));

        var regResponse = await Client.PostAsJsonAsync(_teachersUri, regRequest);
        var created = await regResponse.Content.ReadFromJsonAsync<RegisterTeacherProfileResponse>();
        await Client.PostAsync($"{_teachersUri}/{created!.Id}/approve", null);

        // 1. Act: Add capability
        var addCapRequest = new AddTeachingCapabilityRequest(
            CapabilityTypeId: capTypeId,
            CapabilityCode: $"EN_PRIMARY_{suffix}",
            CapabilityName: "English for Primary School",
            ApplicableLevel: "Primary",
            DeliveryMode: "in_person",
            LanguageOfInstruction: "en",
            Notes: "Native-level fluency");

        var addResponse = await Client.PostAsJsonAsync($"{_teachersUri}/{created.Id}/capabilities", addCapRequest);

        // Assert
        addResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var capResult = await addResponse.Content.ReadFromJsonAsync<AddTeachingCapabilityResponse>();
        capResult.Should().NotBeNull();
        capResult!.Id.Should().NotBeEmpty();

        // Verify capability in DB is active
        var capDbStatus = await ExecuteScalarAsync<string>(
            "SELECT status FROM teaching.teaching_capabilities WHERE id = @Id;",
            new { capResult.Id });
        capDbStatus.Should().Be("active");

        // Assert profile standing is unaffected (remains 'active')
        var currentStanding = await ExecuteScalarAsync<string>(
            "SELECT standing FROM teaching.teacher_profiles WHERE id = @Id;",
            new { created.Id });
        currentStanding.Should().Be("active");

        // Assert authorizations table has zero rows for this teacher
        var authCount = await ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM teaching.teaching_authorizations WHERE teacher_profile_id = @Id;",
            new { created.Id });
        authCount.Should().Be(0);

        // 2. Act: Remove capability
        var removeResponse = await Client.DeleteAsync($"{_teachersUri}/{created.Id}/capabilities/{capResult.Id}");
        removeResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Verify capability is soft-retired (status = 'inactive')
        var capStatus = await ExecuteScalarAsync<string>(
            "SELECT status FROM teaching.teaching_capabilities WHERE id = @Id;",
            new { capResult.Id });
        capStatus.Should().Be("inactive");

        // Assert standing remains 'active'
        var finalStanding = await ExecuteScalarAsync<string>(
            "SELECT standing FROM teaching.teacher_profiles WHERE id = @Id;",
            new { created.Id });
        finalStanding.Should().Be("active");
    }

    [SkippableFact]
    public async Task QualificationEvidenceSubmission_Should_EnterInSubmittedState()
    {
        // Arrange
        SkipIfDatabaseUnavailable();
        var (roleId, _, qualTypeId) = await EnsureReferenceDataAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var regRequest = new RegisterTeacherProfileRequest(
            TeacherNumber: $"TCH-QSUB-{suffix}",
            FullName: $"Teacher Marcus {suffix}",
            DisplayName: "Marcus",
            EmployeeRef: $"EMP-QSUB-{suffix}",
            NationalIdOrExternalRef: $"3175{Random.Shared.Next(10000000, 99999999)}0005",
            Email: $"marcus.{suffix}@elsasa.edu",
            Phone: "+6281556677889",
            TeachingRoleId: roleId,
            CampusAffiliation: "Kampus Utama",
            Department: "Physical Education",
            EffectiveFrom: new DateOnly(2026, 8, 1));

        var regResponse = await Client.PostAsJsonAsync(_teachersUri, regRequest);
        var created = await regResponse.Content.ReadFromJsonAsync<RegisterTeacherProfileResponse>();

        // Act: Submit qualification evidence
        var submitRequest = new SubmitQualificationEvidenceRequest(
            QualificationTypeId: qualTypeId,
            Title: "Bachelor of Physical Education",
            IssuingAuthority: "Universitas Negeri Jakarta",
            CredentialIdentifier: $"DEG-{suffix}",
            IssueDate: new DateOnly(2020, 6, 25),
            ExpiryDate: null,
            DocumentReference: $"https://documents.elsasa.edu/credentials/{suffix}.pdf",
            RestrictionsOrConditions: null);

        var submitResponse = await Client.PostAsJsonAsync($"{_teachersUri}/{created!.Id}/qualifications", submitRequest);

        // Assert
        submitResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var submitResult = await submitResponse.Content.ReadFromJsonAsync<SubmitQualificationEvidenceResponse>();
        submitResult.Should().NotBeNull();
        submitResult!.Id.Should().NotBeEmpty();
        submitResult.TeacherProfileId.Should().Be(created.Id);
        submitResult.VerificationState.Should().Be("submitted");

        // Verify database persistence
        const string checkSql = """
            SELECT verification_state AS VerificationState,
                   verified_by AS VerifiedBy,
                   verified_date AS VerifiedDate,
                   issuing_authority AS IssuingAuthority,
                   verification_notes AS VerificationNotes,
                   rejection_reason AS RejectionReason
            FROM teaching.qualification_evidence
            WHERE id = @Id;
            """;
        var dbRecord = await QuerySingleOrDefaultAsync<EvidenceRecord>(checkSql, new { submitResult.Id });
        dbRecord.Should().NotBeNull();
        dbRecord!.VerificationState.Should().Be("submitted");
        dbRecord.VerifiedBy.Should().BeNull();
        dbRecord.VerifiedDate.Should().BeNull();
        dbRecord.IssuingAuthority.Should().Be(submitRequest.IssuingAuthority);
    }

    [SkippableFact]
    public async Task CredentialVerification_Should_MarkVerified_And_RecordVerifierAndDate()
    {
        // Arrange
        SkipIfDatabaseUnavailable();
        var (roleId, _, qualTypeId) = await EnsureReferenceDataAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var regRequest = new RegisterTeacherProfileRequest(
            TeacherNumber: $"TCH-QVER-{suffix}",
            FullName: $"Teacher Clara {suffix}",
            DisplayName: "Clara",
            EmployeeRef: $"EMP-QVER-{suffix}",
            NationalIdOrExternalRef: $"3176{Random.Shared.Next(10000000, 99999999)}0006",
            Email: $"clara.{suffix}@elsasa.edu",
            Phone: "+6281334455667",
            TeachingRoleId: roleId,
            CampusAffiliation: "Kampus Utama",
            Department: "Early Childhood",
            EffectiveFrom: new DateOnly(2026, 8, 1));

        var regResponse = await Client.PostAsJsonAsync(_teachersUri, regRequest);
        var created = await regResponse.Content.ReadFromJsonAsync<RegisterTeacherProfileResponse>();

        var submitRequest = new SubmitQualificationEvidenceRequest(
            QualificationTypeId: qualTypeId,
            Title: "Montessori Early Childhood Diploma",
            IssuingAuthority: "Association Montessori Internationale",
            CredentialIdentifier: $"AMI-{suffix}",
            IssueDate: new DateOnly(2021, 5, 15),
            ExpiryDate: null,
            DocumentReference: $"https://documents.elsasa.edu/credentials/ami-{suffix}.pdf");

        var submitResponse = await Client.PostAsJsonAsync($"{_teachersUri}/{created!.Id}/qualifications", submitRequest);
        var evidence = await submitResponse.Content.ReadFromJsonAsync<SubmitQualificationEvidenceResponse>();

        // Act: Verifier marks evidence as verified
        var verifyRequest = new VerifyQualificationEvidenceRequest(
            VerificationNotes: "Verified directly via AMI credential registry API.");

        var verifyResponse = await Client.PostAsJsonAsync($"{_qualificationsUri}/{evidence!.Id}/verify", verifyRequest);

        // Assert
        verifyResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var verifyResult = await verifyResponse.Content.ReadFromJsonAsync<VerifyQualificationEvidenceResponse>();
        verifyResult.Should().NotBeNull();
        verifyResult!.Id.Should().Be(evidence.Id);
        verifyResult.VerificationState.Should().Be("verified");
        verifyResult.VerifiedBy.Should().Be(Factory.CurrentUser.Username);
        verifyResult.VerifiedDate.Should().Be(DateOnly.FromDateTime(DateTime.UtcNow));
        verifyResult.VerificationNotes.Should().Be(verifyRequest.VerificationNotes);

        // Verify database persistence
        const string checkSql = """
            SELECT verification_state AS VerificationState,
                   verified_by AS VerifiedBy,
                   verified_date AS VerifiedDate,
                   issuing_authority AS IssuingAuthority,
                   verification_notes AS VerificationNotes,
                   rejection_reason AS RejectionReason
            FROM teaching.qualification_evidence
            WHERE id = @Id;
            """;
        var dbRecord = await QuerySingleOrDefaultAsync<EvidenceRecord>(checkSql, new { evidence.Id });
        dbRecord.Should().NotBeNull();
        dbRecord!.VerificationState.Should().Be("verified");
        dbRecord.VerifiedBy.Should().Be(Factory.CurrentUser.Username);
        DateOnly.FromDateTime(dbRecord.VerifiedDate!.Value).Should().Be(DateOnly.FromDateTime(DateTime.UtcNow));
        dbRecord.VerificationNotes.Should().Be(verifyRequest.VerificationNotes);

        // Verify domain event
        Factory.EventCollector.QualificationEvidenceVerifiedEvents
            .Should().Contain(e => e.EvidenceId == evidence.Id && e.VerifiedBy == dbRecord.VerifiedBy);
    }

    [SkippableFact]
    public async Task CredentialRejection_Should_RecordMandatoryRejectionReason_And_MarkRejected()
    {
        // Arrange
        SkipIfDatabaseUnavailable();
        var (roleId, _, qualTypeId) = await EnsureReferenceDataAsync();
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var regRequest = new RegisterTeacherProfileRequest(
            TeacherNumber: $"TCH-QREJ-{suffix}",
            FullName: $"Teacher David {suffix}",
            DisplayName: "David",
            EmployeeRef: $"EMP-QREJ-{suffix}",
            NationalIdOrExternalRef: $"3177{Random.Shared.Next(10000000, 99999999)}0007",
            Email: $"david.{suffix}@elsasa.edu",
            Phone: "+6281445566778",
            TeachingRoleId: roleId,
            CampusAffiliation: "Kampus Utama",
            Department: "Music",
            EffectiveFrom: new DateOnly(2026, 8, 1));

        var regResponse = await Client.PostAsJsonAsync(_teachersUri, regRequest);
        var created = await regResponse.Content.ReadFromJsonAsync<RegisterTeacherProfileResponse>();

        var submitRequest = new SubmitQualificationEvidenceRequest(
            QualificationTypeId: qualTypeId,
            Title: "ABRSM Music Teaching Certificate",
            IssuingAuthority: "Associated Board of Royal Schools of Music",
            CredentialIdentifier: $"ABRSM-{suffix}",
            IssueDate: new DateOnly(2019, 3, 10),
            ExpiryDate: null,
            DocumentReference: $"https://documents.elsasa.edu/credentials/abrsm-{suffix}.pdf");

        var submitResponse = await Client.PostAsJsonAsync($"{_teachersUri}/{created!.Id}/qualifications", submitRequest);
        var evidence = await submitResponse.Content.ReadFromJsonAsync<SubmitQualificationEvidenceResponse>();

        // 1. Act: Rejection without mandatory reason should fail (422 UnprocessableEntity)
        var invalidRejectRequest = new RejectQualificationEvidenceRequest(RejectionReason: "   ");
        var invalidResponse = await Client.PostAsJsonAsync($"{_qualificationsUri}/{evidence!.Id}/reject", invalidRejectRequest);
        invalidResponse.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        // 2. Act: Valid rejection with mandatory reason
        var validRejectRequest = new RejectQualificationEvidenceRequest(
            RejectionReason: "Uploaded certificate document is unreadable/corrupted. Please resubmit clear scan.");

        var rejectResponse = await Client.PostAsJsonAsync($"{_qualificationsUri}/{evidence.Id}/reject", validRejectRequest);

        // Assert
        rejectResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var rejectResult = await rejectResponse.Content.ReadFromJsonAsync<RejectQualificationEvidenceResponse>();
        rejectResult.Should().NotBeNull();
        rejectResult!.Id.Should().Be(evidence.Id);
        rejectResult.VerificationState.Should().Be("rejected");
        rejectResult.VerifiedBy.Should().Be(Factory.CurrentUser.Username);
        rejectResult.VerifiedDate.Should().Be(DateOnly.FromDateTime(DateTime.UtcNow));
        rejectResult.RejectionReason.Should().Be(validRejectRequest.RejectionReason);

        // Verify database persistence
        const string checkSql = """
            SELECT verification_state AS VerificationState,
                   verified_by AS VerifiedBy,
                   verified_date AS VerifiedDate,
                   issuing_authority AS IssuingAuthority,
                   verification_notes AS VerificationNotes,
                   rejection_reason AS RejectionReason
            FROM teaching.qualification_evidence
            WHERE id = @Id;
            """;
        var dbRecord = await QuerySingleOrDefaultAsync<EvidenceRecord>(checkSql, new { evidence.Id });
        dbRecord.Should().NotBeNull();
        dbRecord!.VerificationState.Should().Be("rejected");
        dbRecord.VerifiedBy.Should().Be(Factory.CurrentUser.Username);
        DateOnly.FromDateTime(dbRecord.VerifiedDate!.Value).Should().Be(DateOnly.FromDateTime(DateTime.UtcNow));
        dbRecord.RejectionReason.Should().Be(validRejectRequest.RejectionReason);
    }
}
