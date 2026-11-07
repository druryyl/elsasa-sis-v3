namespace Elsasa.Sis.IntegrationTests.UnitTests.Academics;

using Elsasa.Sis.Academics.Features.StudentLearningPlans.AddAcademicPlanAdjustment;
using Elsasa.Sis.Academics.Features.StudentLearningPlans.AddSlpLearningUnit;
using Elsasa.Sis.Academics.Features.StudentLearningPlans.ApproveStudentLearningPlan;
using Elsasa.Sis.Academics.Features.StudentLearningPlans.ArchiveStudentLearningPlan;
using Elsasa.Sis.Academics.Features.StudentLearningPlans.CreateStudentLearningPlan;
using Elsasa.Sis.Academics.Features.StudentLearningPlans.GetStudentLearningPlanById;
using Elsasa.Sis.Academics.Features.StudentLearningPlans.GetStudentLearningPlans;
using Elsasa.Sis.Academics.Features.StudentLearningPlans.SubmitStudentLearningPlan;
using Elsasa.Sis.Academics.Features.StudentLearningPlans.UpdateStudentLearningPlan;
using Elsasa.Sis.Contracts.Academics;
using Elsasa.Sis.Contracts.Academics.Events;
using Elsasa.Sis.Contracts.Common;
using Elsasa.Sis.Shared.Common;
using FluentAssertions;
using Xunit;

/// <summary>
/// Unit tests for Student Learning Plan DTO projection logic, event contracts, and response mapping (P5-S13).
/// </summary>
[Trait("Category", "UnitTest")]
public sealed class StudentLearningPlanDtoMappingTests
{
    #region StudentLearningPlanDetailDto Mapping Tests

    [Fact]
    public void StudentLearningPlanDetailDto_Should_MapAllPropertiesCorrectly_When_FullyPopulated()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var unitId = Guid.NewGuid();
        var referencedUnitId = Guid.NewGuid();
        var adjustmentId = Guid.NewGuid();
        var approvalId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow.AddDays(-10);
        var updatedAt = DateTimeOffset.UtcNow.AddDays(-1);
        var effectiveDate = new DateOnly(2029, 9, 1);
        var endDate = new DateOnly(2030, 6, 30);
        var approvedDate = new DateOnly(2029, 9, 2);

        var units = new List<SlpLearningUnitDto>
        {
            new(unitId, referencedUnitId, "MAT-03-S1", "Remedial Math Grade 3", 1, true, "Grade 3 pacing adaptation")
        };

        var adjustments = new List<AcademicPlanAdjustmentDto>
        {
            new(
                adjustmentId,
                "accommodation",
                "50% extra exam time in quiet room",
                "Verified psycho-educational evaluation recommendations",
                effectiveDate,
                endDate,
                "ratna.vp",
                approvedDate,
                "Reviewed annually")
        };

        var approvals = new List<SlpApprovalDto>
        {
            new(
                approvalId,
                1,
                "ratna.vp",
                approvedDate,
                "initial_approval",
                "Initial approval granted for individualized pathway.")
        };

        // Act
        var detail = new StudentLearningPlanDetailDto(
            Id: planId,
            StudentId: studentId,
            StudentName: "Budi Santoso",
            StudentNis: "0071234567",
            AcademicPeriodId: periodId,
            AcademicPeriodLabel: "Semester 1 TP 2029/2030",
            Status: "active",
            Version: 1,
            Notes: "Primary individualized learning plan.",
            CreatedAt: createdAt,
            CreatedBy: "coordinator.jane",
            UpdatedAt: updatedAt,
            UpdatedBy: "authority.ratna",
            ConstituentUnits: units,
            Adjustments: adjustments,
            Approvals: approvals);

        // Assert
        detail.Id.Should().Be(planId);
        detail.StudentId.Should().Be(studentId);
        detail.StudentName.Should().Be("Budi Santoso");
        detail.StudentNis.Should().Be("0071234567");
        detail.AcademicPeriodId.Should().Be(periodId);
        detail.AcademicPeriodLabel.Should().Be("Semester 1 TP 2029/2030");
        detail.Status.Should().Be("active");
        detail.Version.Should().Be(1);
        detail.Notes.Should().Be("Primary individualized learning plan.");
        detail.CreatedAt.Should().Be(createdAt);
        detail.CreatedBy.Should().Be("coordinator.jane");
        detail.UpdatedAt.Should().Be(updatedAt);
        detail.UpdatedBy.Should().Be("authority.ratna");

        detail.ConstituentUnits.Should().HaveCount(1);
        var u = detail.ConstituentUnits[0];
        u.Id.Should().Be(unitId);
        u.LearningUnitId.Should().Be(referencedUnitId);
        u.UnitCode.Should().Be("MAT-03-S1");
        u.Title.Should().Be("Remedial Math Grade 3");
        u.SequenceOrder.Should().Be(1);
        u.IsRequired.Should().BeTrue();
        u.Notes.Should().Be("Grade 3 pacing adaptation");

        detail.Adjustments.Should().HaveCount(1);
        var a = detail.Adjustments[0];
        a.Id.Should().Be(adjustmentId);
        a.AdjustmentType.Should().Be("accommodation");
        a.Description.Should().Be("50% extra exam time in quiet room");
        a.Rationale.Should().Be("Verified psycho-educational evaluation recommendations");
        a.EffectiveDate.Should().Be(effectiveDate);
        a.EndDate.Should().Be(endDate);
        a.ApprovedBy.Should().Be("ratna.vp");
        a.ApprovedDate.Should().Be(approvedDate);
        a.Notes.Should().Be("Reviewed annually");

        detail.Approvals.Should().HaveCount(1);
        var ap = detail.Approvals[0];
        ap.Id.Should().Be(approvalId);
        ap.PlanVersion.Should().Be(1);
        ap.ApprovedBy.Should().Be("ratna.vp");
        ap.ApprovedDate.Should().Be(approvedDate);
        ap.ApprovalType.Should().Be("initial_approval");
        ap.Notes.Should().Be("Initial approval granted for individualized pathway.");
    }

    [Fact]
    public void StudentLearningPlanDetailDto_Should_SupportNullableFields_When_DraftPlanCreated()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;

        // Act
        var detail = new StudentLearningPlanDetailDto(
            Id: planId,
            StudentId: studentId,
            StudentName: null,
            StudentNis: null,
            AcademicPeriodId: periodId,
            AcademicPeriodLabel: "Semester 1 TP 2029/2030",
            Status: "draft",
            Version: 1,
            Notes: null,
            CreatedAt: createdAt,
            CreatedBy: "coordinator.jane",
            UpdatedAt: null,
            UpdatedBy: null,
            ConstituentUnits: [],
            Adjustments: [],
            Approvals: []);

        // Assert
        detail.StudentName.Should().BeNull();
        detail.StudentNis.Should().BeNull();
        detail.Notes.Should().BeNull();
        detail.UpdatedAt.Should().BeNull();
        detail.UpdatedBy.Should().BeNull();
        detail.ConstituentUnits.Should().BeEmpty();
        detail.Adjustments.Should().BeEmpty();
        detail.Approvals.Should().BeEmpty();
    }

    #endregion

    #region ActiveStudentLearningPlanDto Mapping Tests

    [Fact]
    public void ActiveStudentLearningPlanDto_Should_MapAllPropertiesCorrectly()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var unitId = Guid.NewGuid();
        var refUnitId = Guid.NewGuid();
        var adjustmentId = Guid.NewGuid();

        var units = new List<SlpLearningUnitDto>
        {
            new(unitId, refUnitId, "BIO-04-S1", "Biology Adaptive", null, false, null)
        };

        var adjustments = new List<AcademicPlanAdjustmentDto>
        {
            new(
                adjustmentId,
                "substitution",
                "Substituted Latin with Computer Science",
                "Approved curriculum pathway substitution",
                new DateOnly(2029, 9, 1),
                null,
                "ratna.vp",
                new DateOnly(2029, 9, 1),
                null)
        };

        // Act
        var activePlan = new ActiveStudentLearningPlanDto(
            PlanId: planId,
            StudentId: studentId,
            AcademicPeriodId: periodId,
            AcademicPeriodLabel: "Semester 1 TP 2029/2030",
            Version: 2,
            Status: "active",
            Notes: "Active revised learning plan",
            ConstituentUnits: units,
            Adjustments: adjustments);

        // Assert
        activePlan.PlanId.Should().Be(planId);
        activePlan.StudentId.Should().Be(studentId);
        activePlan.AcademicPeriodId.Should().Be(periodId);
        activePlan.AcademicPeriodLabel.Should().Be("Semester 1 TP 2029/2030");
        activePlan.Version.Should().Be(2);
        activePlan.Status.Should().Be("active");
        activePlan.Notes.Should().Be("Active revised learning plan");
        activePlan.ConstituentUnits.Should().HaveCount(1);
        activePlan.ConstituentUnits[0].UnitCode.Should().Be("BIO-04-S1");
        activePlan.ConstituentUnits[0].IsRequired.Should().BeFalse();
        activePlan.ConstituentUnits[0].SequenceOrder.Should().BeNull();
        activePlan.Adjustments.Should().HaveCount(1);
        activePlan.Adjustments[0].AdjustmentType.Should().Be("substitution");
        activePlan.Adjustments[0].EndDate.Should().BeNull();
    }

    #endregion

    #region StudentLearningPlanDto Contract Mapping Tests

    [Fact]
    public void StudentLearningPlanDto_ShortConstructor_Should_InitializeEmptyCollections()
    {
        // Arrange
        var id = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        // Act
        var dto = new StudentLearningPlanDto(
            id,
            studentId,
            periodId,
            "Semester 1 TP 2029/2030",
            "draft",
            1,
            "Notes",
            now,
            "coordinator.jane",
            null,
            null);

        // Assert
        dto.Id.Should().Be(id);
        dto.StudentId.Should().Be(studentId);
        dto.AcademicPeriodId.Should().Be(periodId);
        dto.AcademicPeriodLabel.Should().Be("Semester 1 TP 2029/2030");
        dto.Status.Should().Be("draft");
        dto.Version.Should().Be(1);
        dto.Notes.Should().Be("Notes");
        dto.CreatedAt.Should().Be(now);
        dto.CreatedBy.Should().Be("coordinator.jane");
        dto.UpdatedAt.Should().BeNull();
        dto.UpdatedBy.Should().BeNull();
        dto.ConstituentUnits.Should().BeEmpty();
        dto.Adjustments.Should().BeEmpty();
        dto.Approvals.Should().BeEmpty();
    }

    [Fact]
    public void StudentLearningPlanDto_FullConstructor_Should_RetainCollections()
    {
        // Arrange
        var id = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        var units = new List<SlpLearningUnitDto>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), "ENG-01", "English", 1, true, null)
        };
        var adjustments = new List<AcademicPlanAdjustmentDto>
        {
            new(Guid.NewGuid(), "exemption", "Exemption", "Rationale", new DateOnly(2029, 9, 1), null, null, null, null)
        };
        var approvals = new List<SlpApprovalDto>
        {
            new(Guid.NewGuid(), 1, "authority", new DateOnly(2029, 9, 1), "initial_approval", null)
        };

        // Act
        var dto = new StudentLearningPlanDto(
            id,
            studentId,
            periodId,
            "Semester 1 TP 2029/2030",
            "active",
            1,
            "Approved plan",
            now,
            "coordinator",
            now,
            "authority",
            units,
            adjustments,
            approvals);

        // Assert
        dto.ConstituentUnits.Should().HaveCount(1);
        dto.Adjustments.Should().HaveCount(1);
        dto.Approvals.Should().HaveCount(1);
        dto.ConstituentUnits[0].UnitCode.Should().Be("ENG-01");
        dto.Adjustments[0].AdjustmentType.Should().Be("exemption");
        dto.Approvals[0].ApprovalType.Should().Be("initial_approval");
    }

    #endregion

    #region StudentLearningPlanListItemDto Mapping Tests

    [Fact]
    public void StudentLearningPlanListItemDto_Should_MapSummaryColumnsAccurately()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow.AddDays(-5);
        var updatedAt = DateTimeOffset.UtcNow.AddHours(-2);

        // Act
        var item = new StudentLearningPlanListItemDto(
            Id: planId,
            StudentId: studentId,
            StudentName: "Timotius",
            StudentNis: "0079876543",
            AcademicPeriodId: periodId,
            AcademicPeriodLabel: "Semester 1 TP 2029/2030",
            Status: "draft",
            Version: 1,
            UnitsCount: 4,
            AdjustmentsCount: 2,
            CreatedAt: createdAt,
            UpdatedAt: updatedAt);

        // Assert
        item.Id.Should().Be(planId);
        item.StudentId.Should().Be(studentId);
        item.StudentName.Should().Be("Timotius");
        item.StudentNis.Should().Be("0079876543");
        item.AcademicPeriodId.Should().Be(periodId);
        item.AcademicPeriodLabel.Should().Be("Semester 1 TP 2029/2030");
        item.Status.Should().Be("draft");
        item.Version.Should().Be(1);
        item.UnitsCount.Should().Be(4);
        item.AdjustmentsCount.Should().Be(2);
        item.CreatedAt.Should().Be(createdAt);
        item.UpdatedAt.Should().Be(updatedAt);
    }

    [Fact]
    public void StudentLearningPlanListItemDto_Should_AllowNullStudentDetailsAndNullUpdatedAt()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var createdAt = DateTimeOffset.UtcNow;

        // Act
        var item = new StudentLearningPlanListItemDto(
            Id: planId,
            StudentId: studentId,
            StudentName: null,
            StudentNis: null,
            AcademicPeriodId: periodId,
            AcademicPeriodLabel: "Period Label",
            Status: "draft",
            Version: 1,
            UnitsCount: 0,
            AdjustmentsCount: 0,
            CreatedAt: createdAt,
            UpdatedAt: null);

        // Assert
        item.StudentName.Should().BeNull();
        item.StudentNis.Should().BeNull();
        item.UpdatedAt.Should().BeNull();
        item.UnitsCount.Should().Be(0);
        item.AdjustmentsCount.Should().Be(0);
    }

    #endregion

    #region SlpLearningUnitDto & AcademicPlanAdjustmentDto Mapping Tests

    [Fact]
    public void SlpLearningUnitDto_Should_MapAllConfiguredFields()
    {
        // Arrange
        var entryId = Guid.NewGuid();
        var luId = Guid.NewGuid();

        // Act
        var unit = new SlpLearningUnitDto(
            Id: entryId,
            LearningUnitId: luId,
            UnitCode: "MATH-03",
            Title: "Remedial Mathematics",
            SequenceOrder: 2,
            IsRequired: true,
            Notes: "Lower level adaptation");

        // Assert
        unit.Id.Should().Be(entryId);
        unit.LearningUnitId.Should().Be(luId);
        unit.UnitCode.Should().Be("MATH-03");
        unit.Title.Should().Be("Remedial Mathematics");
        unit.SequenceOrder.Should().Be(2);
        unit.IsRequired.Should().BeTrue();
        unit.Notes.Should().Be("Lower level adaptation");
    }

    [Fact]
    public void AcademicPlanAdjustmentDto_Should_MapAllConfiguredFields()
    {
        // Arrange
        var adjId = Guid.NewGuid();
        var effective = new DateOnly(2029, 9, 1);
        var end = new DateOnly(2030, 6, 30);
        var approved = new DateOnly(2029, 9, 2);

        // Act
        var adjustment = new AcademicPlanAdjustmentDto(
            Id: adjId,
            AdjustmentType: "accommodation",
            Description: "Audio reader software during exams",
            Rationale: "Reading comprehension disability accommodations",
            EffectiveDate: effective,
            EndDate: end,
            ApprovedBy: "ratna.vp",
            ApprovedDate: approved,
            Notes: "Must be provisioned in testing center");

        // Assert
        adjustment.Id.Should().Be(adjId);
        adjustment.AdjustmentType.Should().Be("accommodation");
        adjustment.Description.Should().Be("Audio reader software during exams");
        adjustment.Rationale.Should().Be("Reading comprehension disability accommodations");
        adjustment.EffectiveDate.Should().Be(effective);
        adjustment.EndDate.Should().Be(end);
        adjustment.ApprovedBy.Should().Be("ratna.vp");
        adjustment.ApprovedDate.Should().Be(approved);
        adjustment.Notes.Should().Be("Must be provisioned in testing center");
    }

    [Fact]
    public void SlpApprovalDto_Should_MapAllAuditTrailFields()
    {
        // Arrange
        var approvalId = Guid.NewGuid();
        var approvedDate = new DateOnly(2029, 9, 15);

        // Act
        var approval = new SlpApprovalDto(
            Id: approvalId,
            PlanVersion: 2,
            ApprovedBy: "ratna.vp",
            ApprovedDate: approvedDate,
            ApprovalType: "revision_approval",
            Notes: "Amended plan version 2 approved.");

        // Assert
        approval.Id.Should().Be(approvalId);
        approval.PlanVersion.Should().Be(2);
        approval.ApprovedBy.Should().Be("ratna.vp");
        approval.ApprovedDate.Should().Be(approvedDate);
        approval.ApprovalType.Should().Be("revision_approval");
        approval.Notes.Should().Be("Amended plan version 2 approved.");
    }

    #endregion

    #region Command Response DTO Mapping Tests

    [Fact]
    public void CommandResponses_Should_RetainAssignedPayloads()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var unitId = Guid.NewGuid();
        var luId = Guid.NewGuid();
        var adjId = Guid.NewGuid();
        var approvalId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;
        var effDate = new DateOnly(2029, 9, 1);
        var appDate = new DateOnly(2029, 9, 2);

        // Act
        var createResponse = new CreateStudentLearningPlanResponse(
            planId, studentId, periodId, "Semester 1 TP 2029/2030", "draft", 1, "Notes", now, "coordinator");

        var updateResponse = new UpdateStudentLearningPlanResponse(
            planId, "draft", 1, "Updated notes", now, "coordinator");

        var archiveResponse = new ArchiveStudentLearningPlanResponse(
            planId, "archived", 1, now, "authority");

        var addUnitResponse = new AddSlpLearningUnitResponse(
            unitId, planId, luId, 1, true, "Unit notes", now, "coordinator");

        var addAdjResponse = new AddAcademicPlanAdjustmentResponse(
            adjId, planId, "accommodation", "Desc", "Rationale 10 chars", effDate, null, string.Empty, new DateOnly(1900, 1, 1), "Adj notes", now, "coordinator");

        var submitResponse = new SubmitStudentLearningPlanResponse(
            planId, "draft", 1, now, "coordinator", "Submission notes");

        var approveResponse = new ApproveStudentLearningPlanResponse(
            planId, approvalId, 1, "active", "initial_approval", "authority", appDate, "Approval notes");

        // Assert
        createResponse.Id.Should().Be(planId);
        createResponse.StudentId.Should().Be(studentId);
        createResponse.AcademicPeriodId.Should().Be(periodId);
        createResponse.AcademicPeriodLabel.Should().Be("Semester 1 TP 2029/2030");
        createResponse.Status.Should().Be("draft");
        createResponse.Version.Should().Be(1);
        createResponse.Notes.Should().Be("Notes");
        createResponse.CreatedAt.Should().Be(now);
        createResponse.CreatedBy.Should().Be("coordinator");

        updateResponse.Id.Should().Be(planId);
        updateResponse.Status.Should().Be("draft");
        updateResponse.Version.Should().Be(1);
        updateResponse.Notes.Should().Be("Updated notes");
        updateResponse.UpdatedAt.Should().Be(now);
        updateResponse.UpdatedBy.Should().Be("coordinator");

        archiveResponse.Id.Should().Be(planId);
        archiveResponse.Status.Should().Be("archived");
        archiveResponse.Version.Should().Be(1);
        archiveResponse.UpdatedAt.Should().Be(now);
        archiveResponse.UpdatedBy.Should().Be("authority");

        addUnitResponse.Id.Should().Be(unitId);
        addUnitResponse.StudentLearningPlanId.Should().Be(planId);
        addUnitResponse.LearningUnitId.Should().Be(luId);
        addUnitResponse.SequenceOrder.Should().Be(1);
        addUnitResponse.IsRequired.Should().BeTrue();
        addUnitResponse.Notes.Should().Be("Unit notes");
        addUnitResponse.CreatedAt.Should().Be(now);
        addUnitResponse.CreatedBy.Should().Be("coordinator");

        addAdjResponse.Id.Should().Be(adjId);
        addAdjResponse.StudentLearningPlanId.Should().Be(planId);
        addAdjResponse.AdjustmentType.Should().Be("accommodation");
        addAdjResponse.Description.Should().Be("Desc");
        addAdjResponse.Rationale.Should().Be("Rationale 10 chars");
        addAdjResponse.EffectiveDate.Should().Be(effDate);
        addAdjResponse.EndDate.Should().BeNull();
        addAdjResponse.ApprovedBy.Should().BeEmpty();
        addAdjResponse.ApprovedDate.Should().Be(new DateOnly(1900, 1, 1));
        addAdjResponse.Notes.Should().Be("Adj notes");
        addAdjResponse.CreatedAt.Should().Be(now);
        addAdjResponse.CreatedBy.Should().Be("coordinator");

        submitResponse.Id.Should().Be(planId);
        submitResponse.Status.Should().Be("draft");
        submitResponse.Version.Should().Be(1);
        submitResponse.SubmittedAt.Should().Be(now);
        submitResponse.SubmittedBy.Should().Be("coordinator");
        submitResponse.Notes.Should().Be("Submission notes");

        approveResponse.PlanId.Should().Be(planId);
        approveResponse.ApprovalId.Should().Be(approvalId);
        approveResponse.Version.Should().Be(1);
        approveResponse.Status.Should().Be("active");
        approveResponse.ApprovalType.Should().Be("initial_approval");
        approveResponse.ApprovedBy.Should().Be("authority");
        approveResponse.ApprovedDate.Should().Be(appDate);
        approveResponse.Notes.Should().Be("Approval notes");
    }

    #endregion

    #region Domain Event Contract Tests

    [Fact]
    public void StudentLearningPlanCreatedEvent_Should_ImplementIDomainEvent_And_CarryExpectedPayload()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        // Act
        var ev = new StudentLearningPlanCreatedEvent(
            PlanId: planId,
            StudentId: studentId,
            AcademicPeriodId: periodId,
            Version: 1,
            CreatedBy: "coordinator.jane",
            OccurredOn: now);

        // Assert
        ev.Should().BeAssignableTo<IDomainEvent>();
        ev.PlanId.Should().Be(planId);
        ev.StudentId.Should().Be(studentId);
        ev.AcademicPeriodId.Should().Be(periodId);
        ev.Version.Should().Be(1);
        ev.CreatedBy.Should().Be("coordinator.jane");
        ev.OccurredOn.Should().Be(now);
    }

    [Fact]
    public void StudentLearningPlanApprovedEvent_Should_ImplementIDomainEvent_And_CarryExpectedPayload()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        // Act
        var ev = new StudentLearningPlanApprovedEvent(
            PlanId: planId,
            StudentId: studentId,
            AcademicPeriodId: periodId,
            Version: 2,
            ApprovalType: "revision_approval",
            ApprovedBy: "ratna.vp",
            OccurredOn: now);

        // Assert
        ev.Should().BeAssignableTo<IDomainEvent>();
        ev.PlanId.Should().Be(planId);
        ev.StudentId.Should().Be(studentId);
        ev.AcademicPeriodId.Should().Be(periodId);
        ev.Version.Should().Be(2);
        ev.ApprovalType.Should().Be("revision_approval");
        ev.ApprovedBy.Should().Be("ratna.vp");
        ev.OccurredOn.Should().Be(now);
    }

    [Fact]
    public void StudentLearningPlanRevisedEvent_Should_ImplementIDomainEvent_And_CarryExpectedPayload()
    {
        // Arrange
        var prevPlanId = Guid.NewGuid();
        var newPlanId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        // Act
        var ev = new StudentLearningPlanRevisedEvent(
            PriorPlanId: prevPlanId,
            NewPlanId: newPlanId,
            StudentId: studentId,
            AcademicPeriodId: periodId,
            NewVersion: 2,
            RevisedBy: "coordinator.jane",
            OccurredOn: now);

        // Assert
        ev.Should().BeAssignableTo<IDomainEvent>();
        ev.PriorPlanId.Should().Be(prevPlanId);
        ev.NewPlanId.Should().Be(newPlanId);
        ev.StudentId.Should().Be(studentId);
        ev.AcademicPeriodId.Should().Be(periodId);
        ev.NewVersion.Should().Be(2);
        ev.RevisedBy.Should().Be("coordinator.jane");
        ev.OccurredOn.Should().Be(now);
    }

    [Fact]
    public void StudentLearningPlanParticipationsSyncedEvent_Should_ImplementIDomainEvent_And_CarryExpectedPayload()
    {
        // Arrange
        var planId = Guid.NewGuid();
        var studentId = Guid.NewGuid();
        var now = DateTimeOffset.UtcNow;

        // Act
        var ev = new StudentLearningPlanParticipationsSyncedEvent(
            PlanId: planId,
            StudentId: studentId,
            SyncedCount: 3,
            SyncedBy: "coordinator.jane",
            OccurredOn: now);

        // Assert
        ev.Should().BeAssignableTo<IDomainEvent>();
        ev.PlanId.Should().Be(planId);
        ev.StudentId.Should().Be(studentId);
        ev.SyncedCount.Should().Be(3);
        ev.SyncedBy.Should().Be("coordinator.jane");
        ev.OccurredOn.Should().Be(now);
    }

    #endregion
}
